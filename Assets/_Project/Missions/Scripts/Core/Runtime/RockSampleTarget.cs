// RockSampleTarget.cs — on the root of every rock that can be sampled.
// States: Available -> Sampling -> Sampled. A rock can only be sampled once; when it is, it leaves
// a drill hole (dark disc of ~3 cm) on its DrillTarget and announces it through SampleEvents.
using System.Collections.Generic;
using UnityEngine;

namespace Game.Missions
{
    public enum SampleState { Available, Sampling, Sampled }

    [DisallowMultipleComponent]
    public class RockSampleTarget : MonoBehaviour
    {
        public RockTypeData rockType;
        [Tooltip("Point where the rock is drilled. Empty = a child named *_DrillTarget, or the rock itself.")]
        public Transform drillTarget;
        [Tooltip("Drill hole diameter (m).")]
        public float drillHoleDiameter = 0.03f;

        [SerializeField] SampleState state = SampleState.Available;
        GameObject _hole;

        public SampleState State => state;
        public bool IsSampled => state == SampleState.Sampled;
        public bool CanSample => state == SampleState.Available && rockType != null;
        public Vector3 DrillPoint => drillTarget ? drillTarget.position : transform.position;

        /// <summary>Active sampleable rocks (registry, no scene searches).</summary>
        public static readonly List<RockSampleTarget> All = new List<RockSampleTarget>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => All.Clear();

        void Reset() => FindDrillTarget();
        void Awake() { if (drillTarget == null) FindDrillTarget(); }
        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() => All.Remove(this);

        void FindDrillTarget()
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t != transform && t.name.EndsWith("_DrillTarget")) { drillTarget = t; return; }
        }

        /// <summary>Starts drilling. Fails if already sampled or being sampled.</summary>
        public bool BeginSampling()
        {
            if (!CanSample) return false;
            SetState(SampleState.Sampling);
            return true;
        }

        /// <summary>Cancels an unfinished drilling: the rock becomes available again.</summary>
        public void CancelSampling()
        {
            if (state == SampleState.Sampling) SetState(SampleState.Available);
        }

        /// <summary>Completes the sample. Returns false if the rock was already sampled or has no type.</summary>
        public bool TryCompleteSample()
        {
            if (state == SampleState.Sampled || rockType == null) return false;
            SetState(SampleState.Sampled);
            CreateDrillHole();
            SampleEvents.RaiseSampleCollected(rockType, this);
            return true;
        }

        /// <summary>Resets the rock (to retry a mission).</summary>
        public void ResetSample()
        {
            if (_hole) Destroy(_hole);
            _hole = null;
            SetState(SampleState.Available);
        }

        void SetState(SampleState s)
        {
            state = s;
        }

        // ---------- drill hole ----------
        static Material _holeMaterial;

        void CreateDrillHole()
        {
            var p = DrillPoint;
            var normal = Vector3.up;
            // placed on the real surface of the rock (its solid colliders)
            var ray = new Ray(p + Vector3.up * 0.5f, Vector3.down);
            float best = float.MaxValue;
            foreach (var col in GetComponentsInChildren<Collider>())
                if (!col.isTrigger && col.Raycast(ray, out var hit, 1.5f) && hit.distance < best)
                {
                    best = hit.distance; p = hit.point; normal = hit.normal;
                }

            _hole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _hole.name = "DrillHole";
            Destroy(_hole.GetComponent<Collider>());
            _hole.transform.SetParent(drillTarget ? drillTarget : transform, true);
            _hole.transform.position = p + normal * 0.001f;
            _hole.transform.up = normal;
            _hole.transform.localScale = Vector3.one; // world scale, independent of the parent
            var s = _hole.transform.lossyScale;
            _hole.transform.localScale = new Vector3(drillHoleDiameter / s.x, 0.0015f / s.y, drillHoleDiameter / s.z);

            if (_holeMaterial == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Unlit");
                if (sh == null) sh = Shader.Find("Unlit/Color");
                _holeMaterial = new Material(sh) { name = "DrillHole" };
                var dark = new Color(0.05f, 0.035f, 0.03f);
                if (_holeMaterial.HasProperty("_BaseColor")) _holeMaterial.SetColor("_BaseColor", dark);
                _holeMaterial.color = dark;
            }
            var r = _hole.GetComponent<MeshRenderer>();
            r.sharedMaterial = _holeMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
