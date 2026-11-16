// SimpleSamplingInteractor.cs — perforador PROVISIONAL (el rover aún no tiene sistema de perforación).
// Cuando la punta (INT_ROV_Drill_Tip, o el rover / la cámara) está cerca de una roca muestreable aparece
// "Mantén [R] para perforar". Mantener R llena la barra en 2 s × dureza; soltar antes cancela.
// Al terminar guarda la muestra en un tubo (10 tubos) y llama a RockSampleTarget.TryCompleteSample().
// No toca RoverController: es un componente aparte. Se comunica por eventos (la UI los escucha).
using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game.Missions
{
    public class SimpleSamplingInteractor : MonoBehaviour
    {
        [Tooltip("Punta del taladro. Vacío = busca INT_ROV_Drill_Tip en los hijos; si no hay, el propio objeto.")]
        public Transform tip;
        [Tooltip("Alcance horizontal desde la punta del taladro (o el centro del rover) hasta el borde de la roca (m). Igual que el de la ventana de análisis.")]
        public float reach = 4f;
        [Tooltip("Segundos de perforación por punto de dureza.")]
        public float secondsPerHardness = 2f;
        [Tooltip("Tubos de muestra del rover.")]
        public int tubeCapacity = MissionDefinition.RoverTubeCapacity;
#if ENABLE_INPUT_SYSTEM
        public Key drillKey = Key.R;
#endif

        public RockSampleTarget Target { get; private set; }
        public RockSampleTarget Drilling { get; private set; }
        public float Progress { get; private set; }           // 0..1 de la perforación en curso
        public int TubesUsed { get; private set; }
        public int TubesLeft => tubeCapacity - TubesUsed;
        public string KeyLabel =>
#if ENABLE_INPUT_SYSTEM
            drillKey.ToString().ToUpper();
#else
            "R";
#endif

        /// <summary>La roca al alcance cambió (null = ninguna).</summary>
        public event Action<RockSampleTarget> TargetChanged;
        /// <summary>Progreso de la perforación (0..1). Se lanza también con 0 al cancelar.</summary>
        public event Action<float> ProgressChanged;
        /// <summary>Mensaje breve para el jugador (cancelado, sin tubos, muestra guardada…).</summary>
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
            // al comenzar cada misión se vacían los tubos
            _manager = FindAnyObjectByType<MissionManager>();
            if (_manager != null) _manager.OnMissionStarted += OnMissionStarted;
        }

        void OnDestroy()
        {
            if (_manager != null) _manager.OnMissionStarted -= OnMissionStarted;
        }

        void OnMissionStarted(MissionDefinition _) => ResetTubes();

        void OnDisable() => Cancel(false);

        /// <summary>Vacía los tubos (al empezar o reintentar una misión).</summary>
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

            // perforación en curso
            if (Drilling != null)
            {
                if (!held || Drilling != Target) { Cancel(true); return; }
                float duration = Mathf.Max(0.1f, secondsPerHardness * Drilling.rockType.hardness);
                Progress = Mathf.Min(1f, Progress + Time.deltaTime / duration);
                ProgressChanged?.Invoke(Progress);
                if (Progress >= 1f) Complete();
                return;
            }

            // empezar: basta con mantener R (aunque se pulsara antes de llegar a la roca);
            // los avisos de "ya muestreada" / "sin tubos" solo al pulsar, para no repetirlos cada frame
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
            // F9: completa la roca muestreable más cercana (depuración)
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
            bool ok = rock.TryCompleteSample();       // primero se marca como muestreada…
            if (ok) TubesUsed++;
            ProgressChanged?.Invoke(0f);              // …y luego se avisa, así el HUD ya ve "Roca ya muestreada"
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
        /// La roca más cercana a la punta dentro del alcance (distancia a su superficie), con preferencia por la que
        /// está delante del rover (la misma que elige la ventana de análisis). Busca en colliders/triggers y en las
        /// rocas del campo de rocas del terreno (FieldRockIndex), que no tienen collider.
        /// </summary>
        RockSampleTarget FindTarget()
        {
            var p = tip.position;
            var fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            RockSampleTarget best = null;
            float bestScore = float.MaxValue;
            FieldRockIndex.Entry bestEntry = default;
            bool bestIsEntry = false;

            int n = Physics.OverlapSphereNonAlloc(p, reach + 1.5f, _hits, ~0, QueryTriggerInteraction.Collide);   // margen vertical
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
                var h = e.center - p; h.y = 0;              // horizontal, como la ventana de análisis
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
