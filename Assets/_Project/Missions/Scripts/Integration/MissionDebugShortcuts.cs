// MissionDebugShortcuts.cs — atajos de depuración (solo Editor y Development Build (símbolo DEBUG)).
//   F9  completa la roca muestreable más cercana (lo hace SimpleSamplingInteractor).
//   F10 termina la misión en curso con el tiempo actual.
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game.Missions
{
    [RequireComponent(typeof(MissionManager))]
    public class MissionDebugShortcuts : MonoBehaviour
    {
        MissionManager _manager;

        void Awake() => _manager = GetComponent<MissionManager>();

#if UNITY_EDITOR || DEBUG
        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.f10Key.wasPressedThisFrame) _manager.DebugFinishNow();
#else
            if (Input.GetKeyDown(KeyCode.F10)) _manager.DebugFinishNow();
#endif
        }
#endif
    }
}
