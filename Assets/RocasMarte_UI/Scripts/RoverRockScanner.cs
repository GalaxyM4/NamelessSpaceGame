// RoverRockScanner.cs — on the rover. Detects the closest rock (preferably in front), shows the
// "F · ANALYZE" prompt and opens/closes the window with the key. The window closes if the rover moves away.
// Rocks: those of the terrain rock field (RockRegistry, filled by RocasMarteBootstrap).
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RocasMarte
{
    public class RoverRockScanner : MonoBehaviour
    {
        public RockInfoUI ui;
        [Tooltip("Point of the rover distances are measured from. Defaults to the rover itself.")]
        public Transform scanOrigin;
        [Tooltip("Maximum horizontal distance from the scan origin to the edge of the rock (m).")]
        public float range = 4f;
#if ENABLE_INPUT_SYSTEM
        public Key interactKey = Key.F;   // E/Q rotate the rover camera
        public Key closeKey = Key.Escape;
#else
        public KeyCode interactKey = KeyCode.F;
        public KeyCode closeKey = KeyCode.Escape;
#endif

        /// <summary>A rock panel was opened (rock center, window). Other systems can add data.</summary>
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
            to.y = 0;                                   // horizontal distance: the rover center sits high
            float d = to.magnitude - radius;
            if (d > range) return;
            float score = d + (Vector3.Dot(to.normalized, fwd) < 0.2f ? range : 0f); // rocks in front of the rover first
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
                // stays open while the opened rock is still in reach
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
