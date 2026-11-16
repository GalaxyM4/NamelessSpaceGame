// MissionTutorial.cs — short controls tutorial shown before every mission briefing.
// Each step completes when the player does it:
//   1) drive with W A S D   2) brake with SPACE   3) rotate the camera with Q / E
//   4) raise / lower the camera with SHIFT / CTRL
//   5) a random nearby rock is marked: drive to it and press F to analyze it
//   6) hold R to drill a sample from it
// Then it shows the mission briefing. TAB skips it. The UI (MissionUIController) listens to its events.
using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game.Missions
{
    public class MissionTutorial : MonoBehaviour
    {
        public MissionManager manager;
        public Transform rover;
        [Tooltip("Distance range (m) for picking the random rock to analyze and drill.")]
        public Vector2 rockDistance = new Vector2(8f, 22f);
        [Tooltip("If the rover ends up farther than this from the marked rock (m), a new nearby rock is marked.")]
        public float maxRockDistance = 40f;
        [Tooltip("Seconds the final message stays on screen before the briefing.")]
        public float doneSeconds = 2.5f;

        public const int StepCount = 6;

        readonly System.Collections.Generic.List<RockSampleTarget> _drilled = new System.Collections.Generic.List<RockSampleTarget>();

        /// <summary>Step changed: index (1..StepCount, or StepCount+1 = done), instruction, extra line.</summary>
        public event Action<int, string, string> StepChanged;
        /// <summary>The tutorial ended (completed or skipped).</summary>
        public event Action Finished;

        public bool IsRunning { get; private set; }

        int _step;
        float _accum, _doneTimer;
        Vector3 _lastPos;
        FieldRockIndex.Entry _rock;
        bool _hasRock, _analyzed;
        GameObject _marker;

        void Start()
        {
            if (rover == null) { var rc = FindAnyObjectByType<RoverController>(); if (rc) rover = rc.transform; }
            IsRunning = true;
            RocasMarte.RoverRockScanner.Inspected += OnInspected;
            SampleEvents.OnSampleCollected += OnSample;
            GoTo(1);
        }

        void OnDestroy()
        {
            RocasMarte.RoverRockScanner.Inspected -= OnInspected;
            SampleEvents.OnSampleCollected -= OnSample;
            if (_marker) Destroy(_marker);
        }

        void GoTo(int step)
        {
            _step = step;
            _accum = 0f;
            if (rover) _lastPos = rover.position;
            string sub = null;
            if (step == 5) { PickRock(); sub = RockDistanceText(); }
            StepChanged?.Invoke(step, Instruction(step), sub);
        }

        static string Instruction(int step)
        {
            switch (step)
            {
                case 1: return MissionTexts.TutorialMove;
                case 2: return MissionTexts.TutorialBrake;
                case 3: return MissionTexts.TutorialCameraYaw;
                case 4: return MissionTexts.TutorialCameraPitch;
                case 5: return MissionTexts.TutorialAnalyze;
                case 6: return MissionTexts.TutorialDrill;
                default: return MissionTexts.TutorialDone;
            }
        }

        void Update()
        {
            if (!IsRunning) return;
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.tabKey.wasPressedThisFrame) { End(); return; }

            switch (_step)
            {
                case 1:   // drive a few meters
                    if (rover)
                    {
                        var d = rover.position - _lastPos; d.y = 0f;
                        _accum += d.magnitude;
                        _lastPos = rover.position;
                    }
                    if (_accum >= 3f) GoTo(2);
                    break;
                case 2:
                    if (kb.spaceKey.wasPressedThisFrame) GoTo(3);
                    break;
                case 3:
                    if (kb.qKey.isPressed || kb.eKey.isPressed) _accum += Time.deltaTime;
                    if (_accum >= 0.5f) GoTo(4);
                    break;
                case 4:
                    if (kb.leftShiftKey.isPressed || kb.leftCtrlKey.isPressed) _accum += Time.deltaTime;
                    if (_accum >= 0.5f) GoTo(5);
                    break;
                case 5:
                    if (_analyzed) { GoTo(6); break; }
                    if (Time.frameCount % 10 == 0) StepChanged?.Invoke(5, Instruction(5), RockDistanceText());
                    break;
                case StepCount + 1:
                    _doneTimer -= Time.deltaTime;
                    if (_doneTimer <= 0f) End();
                    break;
            }
        }

        // ---------- step 5/6: random marked rock ----------
        void PickRock()
        {
            _hasRock = false; _analyzed = false;
            if (rover == null || FieldRockIndex.Entries.Count == 0) return;
            var p = rover.position;
            var fwd = Vector3.ProjectOnPlane(rover.forward, Vector3.up).normalized;
            var candidates = new System.Collections.Generic.List<FieldRockIndex.Entry>();
            foreach (var e in FieldRockIndex.Entries)
            {
                var h = e.center - p; h.y = 0f;
                float d = h.magnitude;
                if (d < rockDistance.x || d > rockDistance.y) continue;
                if (Vector3.Dot(h.normalized, fwd) < 0.5f) continue;          // in front of the rover
                if (e.go.GetComponent<RockSampleTarget>() is RockSampleTarget t && t.IsSampled) continue;
                candidates.Add(e);
            }
            if (candidates.Count == 0)   // fallback: any rock in range, any direction
                foreach (var e in FieldRockIndex.Entries)
                {
                    var h = e.center - p; h.y = 0f;
                    if (h.magnitude >= rockDistance.x && h.magnitude <= rockDistance.y * 2f) candidates.Add(e);
                }
            if (candidates.Count == 0) return;

            _rock = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            _hasRock = true;
            CreateMarker();
        }

        string RockDistanceText()
        {
            if (!_hasRock || rover == null) return null;
            var h = _rock.center - rover.position; h.y = 0f;
            return string.Format(MissionTexts.TutorialAnalyzeDistance, Mathf.CeilToInt(Mathf.Max(0f, h.magnitude - _rock.radius)));
        }

        void CreateMarker()
        {
            if (_marker) Destroy(_marker);
            _marker = new GameObject("Tutorial_RockMarker");
            var lr = _marker.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.SetPosition(0, _rock.center);
            lr.SetPosition(1, _rock.center + Vector3.up * 8f);
            lr.startWidth = 0.3f; lr.endWidth = 0.06f;
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            var mat = new Material(sh);
            var col = new Color(0.24f, 0.96f, 0.82f, 1f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            mat.color = col;
            lr.sharedMaterial = mat;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void OnInspected(Vector3 rockCenter, RocasMarte.RockInfoUI ui)
        {
            if (!IsRunning || _step != 5) return;
            // F opens the closest rock in front of the drill, which may be a neighbor of the marked one:
            // any analyzed rock is accepted and becomes the rock to drill in the next step.
            int best = -1; float bestD = 0.36f;
            for (int i = 0; i < FieldRockIndex.Entries.Count; i++)
            {
                float d = (FieldRockIndex.Entries[i].center - rockCenter).sqrMagnitude;
                if (d < bestD) { bestD = d; best = i; }
            }
            if (best < 0) return;   // not a mission rock type: keep waiting
            _rock = FieldRockIndex.Entries[best];
            _hasRock = true;
            var t = _rock.go.GetComponent<RockSampleTarget>();
            if (t != null && t.IsSampled) { t.ResetSample(); ui.ResetTag(); }   // drilled earlier in the tutorial: drillable again
            CreateMarker();
            _analyzed = true;
        }

        void OnSample(RockTypeData type, RockSampleTarget target)
        {
            if (!IsRunning || target == null) return;
            if (!_drilled.Contains(target)) _drilled.Add(target);   // any rock drilled during the tutorial is restored at the end
            if (_step != 6) return;   // any drilled rock completes the step (all of them are restored at the end)
            if (_marker) Destroy(_marker);
            _doneTimer = doneSeconds;
            GoTo(StepCount + 1);
        }

        void End()
        {
            if (!IsRunning) return;
            IsRunning = false;
            if (_marker) Destroy(_marker);
            // tutorial samples do not count for the mission: those rocks can be drilled again
            foreach (var t in _drilled) if (t != null) t.ResetSample();
            _drilled.Clear();
            Finished?.Invoke();
            if (manager != null) manager.ShowStartBriefing();
        }
    }
}
