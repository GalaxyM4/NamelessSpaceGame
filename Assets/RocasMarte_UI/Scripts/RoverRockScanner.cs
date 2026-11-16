// RoverRockScanner.cs — en el rover. Detecta la roca más cercana (preferentemente delante), muestra el aviso
// "F · ANALIZAR ..." y abre/cierra la ventana con la tecla. La ventana se cierra sola si el rover se aleja.
// Rocas: las del campo de rocas del terreno (RockRegistry, lo rellena RocasMarteBootstrap).
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RocasMarte
{
    public class RoverRockScanner : MonoBehaviour
    {
        public RockInfoUI ui;
        [Tooltip("Punto del rover desde el que se mide. Por defecto, el propio rover.")]
        public Transform scanOrigin;
        [Tooltip("Distancia horizontal máxima desde el centro del rover hasta el borde de la roca (m).")]
        public float range = 4f;
#if ENABLE_INPUT_SYSTEM
        public Key interactKey = Key.F;   // E/Q giran la cámara del rover
        public Key closeKey = Key.Escape;
#else
        public KeyCode interactKey = KeyCode.F;
        public KeyCode closeKey = KeyCode.Escape;
#endif

        /// <summary>Se abrió la ficha de una roca (centro de la roca, ventana). Otros sistemas pueden añadir datos.</summary>
        public static event System.Action<Vector3, RockInfoUI> Inspected;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Inspected = null;

        struct Target { public bool valid; public string id; public Vector3 pos; public float radius; }
        Target _current, _opened;

        void Start()
        {
            if (ui == null) ui = FindAnyObjectByType<RockInfoUI>();
        }

        bool Pressed(bool close)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && kb[close ? closeKey : interactKey].wasPressedThisFrame;
#else
            return Input.GetKeyDown(close ? closeKey : interactKey);
#endif
        }

        void Consider(ref Target best, ref float bestScore, Vector3 origin, Vector3 fwd, Vector3 pos, float radius, string id)
        {
            var to = pos - origin;
            to.y = 0;                                   // distancia en horizontal: el centro del rover va alto
            float d = to.magnitude - radius;
            if (d > range) return;
            float score = d + (Vector3.Dot(to.normalized, fwd) < 0.2f ? range : 0f); // delante del rover primero
            if (score < bestScore) { bestScore = score; best = new Target { valid = true, id = id, pos = pos, radius = radius }; }
        }

        void Update()
        {
            if (ui == null) return;

            var origin = scanOrigin ? scanOrigin.position : transform.position;
            var fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            var best = new Target();
            float bestScore = float.MaxValue;
            var field = RockRegistry.Field;
            for (int i = 0; i < field.Count; i++)
                Consider(ref best, ref bestScore, origin, fwd, field[i].position, field[i].radius, field[i].id);

            if (ui.IsOpen)
            {
                // se mantiene abierta mientras la roca abierta siga al alcance
                var dx = _opened.pos - origin; dx.y = 0;
                bool stillNear = _opened.valid && dx.magnitude - _opened.radius <= range;
                if (Pressed(true) || Pressed(false) || !stillNear) { ui.Hide(); _opened = default; }
            }
            else if (best.valid && Pressed(false))
            {
                ui.Show(best.id, $"~{best.radius * 2f:0.0} m");
                _opened = best;
                Inspected?.Invoke(best.pos, ui);
            }

            _current = best;
            ui.SetPrompt(_current.valid && !ui.IsOpen ? _current.id : null, interactKey.ToString().ToUpper());
        }
    }
}
