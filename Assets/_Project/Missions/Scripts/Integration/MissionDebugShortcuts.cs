// MissionDebugShortcuts.cs — debug shortcuts (Editor and Development Build only, DEBUG symbol).
//   F9  completes the nearest sampleable rock (done by SimpleSamplingInteractor).
//   F10 ends the mission in progress with the current time.
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
