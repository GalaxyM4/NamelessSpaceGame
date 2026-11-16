// RoverPowerSystem.cs — rover battery during missions.
// Capacity comes from the battery chosen in the lobby (RoverModuleSelection.SelectedBattery, Core module data):
// Niquelin 5 kAh … Infinitum 30 kAh. More capacity = more driving time.
//   • Driving drains it (proportional to speed).
//   • Drilling drains a little (drillDrainFactor of the driving drain).
//   • It only drains while a mission is in progress; it is refilled to 100% when a mission starts.
//   • At 0% the mission ends ("BATTERY DEPLETED").
// The percentage is shown with the rover's own battery UI (PerseveranceStatus.BatteryPercent), unmodified.
using System;
using UnityEngine;

namespace Game.Missions
{
    public class RoverPowerSystem : MonoBehaviour
    {
        [Tooltip("Battery capacity in Ah (taken from the lobby selection when available).")]
        public float capacityAh = 15000f;
        [Tooltip("Minutes of continuous driving per 1000 Ah.")]
        public float driveMinutesPerKAh = 1f;
        [Range(0f, 1f)] public float drillDrainFactor = 0.15f;
        [Tooltip("Speed (m/s) considered full-power driving.")]
        public float fullPowerSpeed = 4f;

        public float Percent { get; private set; } = 100f;
        public string BatteryName { get; private set; } = "Standard";
        public float CapacityKAh => capacityAh / 1000f;

        /// <summary>Short warning for the player (low battery).</summary>
        public event Action<string> Message;

        MissionManager _manager;
        SimpleSamplingInteractor _drill;
        PerseveranceStatus _status;
        Rigidbody _body;
        bool _warned20, _warned10;

        public void Configure(MissionsRuntimeSettings settings)
        {
            driveMinutesPerKAh = settings.driveMinutesPerKAh;
            drillDrainFactor = settings.drillDrainFactor;
            var selected = RoverModuleSelection.SelectedBattery;   // chosen in the lobby (previous scene)
            if (selected != null && selected.ah > 0f)
            {
                capacityAh = selected.ah;
                BatteryName = selected.name;                     // module names are kept as they are
            }
            else
            {
                capacityAh = settings.defaultBatteryAh;
                BatteryName = "Standard";
            }
        }

        void Start()
        {
            _manager = FindAnyObjectByType<MissionManager>();
            _drill = GetComponent<SimpleSamplingInteractor>();
            _status = GetComponentInChildren<PerseveranceStatus>(true);
            if (_status == null) _status = FindAnyObjectByType<PerseveranceStatus>();
            _body = GetComponent<Rigidbody>();
            if (_manager != null) _manager.OnMissionStarted += OnMissionStarted;
            Refill();
        }

        void OnDestroy()
        {
            if (_manager != null) _manager.OnMissionStarted -= OnMissionStarted;
        }

        void OnMissionStarted(MissionDefinition _) => Refill();

        public void Refill()
        {
            Percent = 100f;
            _warned20 = _warned10 = false;
            Push();
        }

        void Update()
        {
            if (_manager == null || _manager.State != MissionState.InProgress || Percent <= 0f) return;

            // % per second at full power: 100% lasts (kAh × minutesPerKAh) minutes
            float fullDrainPerSecond = 100f / Mathf.Max(1f, CapacityKAh * driveMinutesPerKAh * 60f);

            float speed = 0f;
            if (_body != null)
            {
                var v = _body.linearVelocity; v.y = 0f;
                speed = v.magnitude;
            }
            float load = Mathf.Clamp01(speed / Mathf.Max(0.1f, fullPowerSpeed));
            if (_drill != null && _drill.Drilling != null) load += drillDrainFactor;

            if (load <= 0f) return;
            Percent = Mathf.Max(0f, Percent - fullDrainPerSecond * load * Time.deltaTime);
            Push();

            if (!_warned20 && Percent <= 20f) { _warned20 = true; Message?.Invoke(string.Format(MissionTexts.BatteryLow, 20)); }
            if (!_warned10 && Percent <= 10f) { _warned10 = true; Message?.Invoke(string.Format(MissionTexts.BatteryLow, 10)); }
            if (Percent <= 0f) _manager.ReportBatteryDepleted();
        }

        void Push()
        {
            if (_status != null) _status.BatteryPercent = Percent;
        }
    }
}
