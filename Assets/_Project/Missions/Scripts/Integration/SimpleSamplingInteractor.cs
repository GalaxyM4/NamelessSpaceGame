// SimpleSamplingInteractor.cs — PROVISIONAL drill (the rover has no drilling system yet).
// When the tip (INT_ROV_Drill_Tip, or the rover / camera) is close to a sampleable rock, "Hold [R] to drill"
// appears. Holding R fills the bar in 2 s × hardness; releasing early cancels.
// When done it stores the sample in a tube (10 tubes) and calls RockSampleTarget.TryCompleteSample().
// It does not touch RoverController: it is a separate component. It talks through events (the UI listens).
using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game.Missions
{
    public class SimpleSamplingInteractor : MonoBehaviour
    {
        [Tooltip("Drill tip. Empty = looks for INT_ROV_Drill_Tip in the children; if missing, this object.")]
        public Transform tip;
        [Tooltip("Horizontal reach from the drill tip (or rover center) to the edge of the rock (m). Same as the analysis window.")]
        public float reach = 4f;
        [Tooltip("Drilling seconds per hardness point.")]
        public float secondsPerHardness = 2f;
        [Tooltip("Rover sample tubes.")]
        public int tubeCapacity = MissionDefinition.RoverTubeCapacity;
#if ENABLE_INPUT_SYSTEM
        public Key drillKey = Key.R;
#endif

        public RockSampleTarget Target { get; private set; }
        public RockSampleTarget Drilling { get; private set; }
        public float Progress { get; private set; }           // 0..1 of the drilling in progress
        public int TubesUsed { get; private set; }
        public int TubesLeft => tubeCapacity - TubesUsed;
        public string KeyLabel =>
#if ENABLE_INPUT_SYSTEM
            drillKey.ToString().ToUpper();
#else
            "R";
#endif

        /// <summary>The rock in reach changed (null = none).</summary>
        public event Action<RockSampleTarget> TargetChanged;
        /// <summary>Drilling progress (0..1). Also raised with 0 on cancel.</summary>
        public event Action<float> ProgressChanged;
        /// <summary>Short message for the player (cancelled, no tubes, sample stored…).</summary>
        public event Action<string> Message;

        readonly Collider[] _hits = new Collider[64];

        void Awake()
        {
            if (tip == null)
                foreach (var t in GetComponentsInChildren<Transform>(true))
                    if (t.name == "INT_ROV_Drill_Tip") { tip = t; break; }
            if (tip == null) tip = transform;
        }

        MissionManager _manager;

        void Start()
        {
            // tubes are emptied when each mission starts
            _manager = FindAnyObjectByType<MissionManager>();
            if (_manager != null) _manager.OnMissionStarted += OnMissionStarted;
        }

        void OnDestroy()
        {
            if (_manager != null) _manager.OnMissionStarted -= OnMissionStarted;
        }

        void OnMissionStarted(MissionDefinition _) => ResetTubes();

        void OnDisable() => Cancel(false);

        /// <summary>Empties the tubes (when starting or retrying a mission).</summary>
        public void ResetTubes() => TubesUsed = 0;

        bool DrillHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb[drillKey].isPressed;
#else
            return Input.GetKey(KeyCode.R);
#endif
        }

        void Update()
        {
            var target = FindTarget();
            if (target != Target) { Target = target; TargetChanged?.Invoke(Target); }

            bool held = DrillHeld();

            // drilling in progress
            if (Drilling != null)
            {
                if (!held || Drilling != Target) { Cancel(true); return; }
                float duration = Mathf.Max(0.1f, secondsPerHardness * Drilling.rockType.hardness);
                Progress = Mathf.Min(1f, Progress + Time.deltaTime / duration);
                ProgressChanged?.Invoke(Progress);
                if (Progress >= 1f) Complete();
                return;
            }

            // start: holding R is enough (even if it was pressed before reaching the rock);
            // "already sampled" / "no tubes" messages only on press, so they don't repeat every frame
            if (held && Target != null)
            {
                bool pressed = WasPressedThisFrame();
                if (Target.IsSampled) { if (pressed) Say(MissionTexts.AlreadySampled); return; }
                if (TubesLeft <= 0) { if (pressed) Say(MissionTexts.NoTubesLeft); return; }
                if (Target.BeginSampling())
                {
                    Drilling = Target;
                    Progress = 0f;
                    ProgressChanged?.Invoke(0f);
                }
            }

#if UNITY_EDITOR || DEBUG
            // F9: completes the nearest sampleable rock (debug)
            if (DebugKeyDown()) DebugCompleteNearest();
#endif
        }

        bool WasPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb[drillKey].wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.R);
#endif
        }

        void Complete()
        {
            var rock = Drilling;
            Drilling = null;
            Progress = 0f;
            bool ok = rock.TryCompleteSample();       // first it is marked as sampled…
            if (ok) TubesUsed++;
            ProgressChanged?.Invoke(0f);              // …then notified, so the HUD already shows "Rock already sampled"
            if (ok) Say(string.Format(MissionTexts.SampleStored, TubesUsed, tubeCapacity));
            if (ok && _manager != null) _manager.ReportTubesLeft(TubesLeft);
        }

        void Cancel(bool notify)
        {
            if (Drilling == null) return;
            Drilling.CancelSampling();
            Drilling = null;
            Progress = 0f;
            ProgressChanged?.Invoke(0f);
            if (notify) Say(MissionTexts.DrillCancelled);
        }

        void Say(string text)
        {
            Message?.Invoke(text);
        }

        /// <summary>
        /// The closest rock to the tip within reach (distance to its surface), preferring the one in front of the
        /// rover (the same one the analysis window picks). Searches colliders/triggers and the terrain rock field
        /// (FieldRockIndex), whose rocks have no collider.
        /// </summary>
        RockSampleTarget FindTarget()
        {
            var p = tip.position;
            var fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            RockSampleTarget best = null;
            float bestScore = float.MaxValue;
            FieldRockIndex.Entry bestEntry = default;
            bool bestIsEntry = false;

            int n = Physics.OverlapSphereNonAlloc(p, reach + 1.5f, _hits, ~0, QueryTriggerInteraction.Collide);   // vertical margin
            for (int i = 0; i < n; i++)
            {
                var t = _hits[i].GetComponentInParent<RockSampleTarget>();
                if (t == null || t.rockType == null) continue;
                var h0 = t.DrillPoint - p; h0.y = 0;
                if (h0.magnitude > reach) continue;
                float s = Score(p, fwd, t.DrillPoint, 0f);
                if (s < bestScore) { bestScore = s; best = t; bestIsEntry = false; }
            }

            var entries = FieldRockIndex.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                var h = e.center - p; h.y = 0;              // horizontal, like the analysis window
                float d = h.magnitude - e.radius;
                if (d > reach) continue;
                float s = Score(p, fwd, e.center, e.radius);
                if (s < bestScore) { bestScore = s; bestEntry = e; bestIsEntry = true; }
            }

            return bestIsEntry ? FieldRockIndex.Acquire(bestEntry) : best;
        }

        float Score(Vector3 p, Vector3 fwd, Vector3 rock, float radius)
        {
            var to = rock - p;
            to.y = 0;
            float d = to.magnitude - radius;
            return d + (fwd.sqrMagnitude > 0f && Vector3.Dot(to.normalized, fwd) < 0.2f ? reach : 0f);
        }

#if UNITY_EDITOR || DEBUG
        bool DebugKeyDown()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb.f9Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F9);
#endif
        }

        void DebugCompleteNearest()
        {
            RockSampleTarget best = null;
            float bestD = float.MaxValue;
            foreach (var t in RockSampleTarget.All)
            {
                if (!t.CanSample) continue;
                float d = (t.DrillPoint - tip.position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = t; }
            }
            if (best == null) return;
            Cancel(false);
            if (best.TryCompleteSample())
            {
                TubesUsed++;
                Say("[F9] " + string.Format(MissionTexts.SampleStored, TubesUsed, tubeCapacity));
                if (_manager != null) _manager.ReportTubesLeft(TubesLeft);
            }
        }
#endif

    }
}
