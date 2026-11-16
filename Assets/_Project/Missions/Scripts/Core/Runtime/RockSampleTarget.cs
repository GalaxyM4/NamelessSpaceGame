// RockSampleTarget.cs — en la raíz de cada roca muestreable.
// Estados: Disponible -> EnMuestreo -> Muestreada. Una roca solo se muestrea una vez; al hacerlo deja
// un agujero de perforación (disco oscuro de ~3 cm) en su DrillTarget y avisa por SampleEvents.
using System.Collections.Generic;
using UnityEngine;

namespace Game.Missions
{
    public enum SampleState { Disponible, EnMuestreo, Muestreada }

    [DisallowMultipleComponent]
    public class RockSampleTarget : MonoBehaviour
    {
        public RockTypeData rockType;
        [Tooltip("Punto donde se perfora. Si está vacío se busca un hijo *_DrillTarget; si no hay, la propia roca.")]
        public Transform drillTarget;
        [Tooltip("Diámetro del agujero de perforación (m).")]
        public float drillHoleDiameter = 0.03f;

        [SerializeField] SampleState state = SampleState.Disponible;
        GameObject _hole;

        public SampleState State => state;
        public bool IsSampled => state == SampleState.Muestreada;
        public bool CanSample => state == SampleState.Disponible && rockType != null;
        public Vector3 DrillPoint => drillTarget ? drillTarget.position : transform.position;

        /// <summary>Rocas muestreables activas (registro sin búsquedas por escena).</summary>
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

        /// <summary>Empieza a perforar. Falla si ya está muestreada o en muestreo.</summary>
        public bool BeginSampling()
        {
            if (!CanSample) return false;
            SetState(SampleState.EnMuestreo);
            return true;
        }

        /// <summary>Cancela una perforación a medias: la roca vuelve a estar disponible.</summary>
        public void CancelSampling()
        {
            if (state == SampleState.EnMuestreo) SetState(SampleState.Disponible);
        }

        /// <summary>Completa la muestra. Devuelve false si la roca ya estaba muestreada o no tiene tipo.</summary>
        public bool TryCompleteSample()
        {
            if (state == SampleState.Muestreada || rockType == null) return false;
            SetState(SampleState.Muestreada);
            CreateDrillHole();
            SampleEvents.RaiseSampleCollected(rockType, this);
            return true;
        }

        /// <summary>Deja la roca como nueva (para reintentar una misión).</summary>
        public void ResetSample()
        {
            if (_hole) Destroy(_hole);
            _hole = null;
            SetState(SampleState.Disponible);
        }

        void SetState(SampleState s)
        {
            state = s;
        }

        // ---------- agujero de perforación ----------
        static Material _holeMaterial;

        void CreateDrillHole()
        {
            var p = DrillPoint;
            var normal = Vector3.up;
            // se apoya sobre la superficie real de la roca (sus colliders sólidos)
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
            _hole.transform.localScale = Vector3.one; // escala del mundo, independiente del padre
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
