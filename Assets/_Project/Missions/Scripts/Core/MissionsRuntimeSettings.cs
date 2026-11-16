// MissionsRuntimeSettings.cs — mission settings read at runtime (Resources/MissionsRuntimeSettings).
// Created by the menu Tools/Missions/Create Runtime Settings.
using System.Collections.Generic;
using UnityEngine;

namespace Game.Missions
{
    public class MissionsRuntimeSettings : ScriptableObject
    {
        public const string ResourcePath = "MissionsRuntimeSettings";

        public MissionDatabase database;
        public List<RockTypeData> rockTypes = new List<RockTypeData>();

        [Header("Game scenes")]
        [Tooltip("When a scene with a rover and a rock field loads: adds the drill to the rover and makes the field rocks drillable.")]
        public bool autoSetupInGameScenes = true;
        [Tooltip("Also creates MissionManager + MissionUI in those scenes if they don't exist.")]
        public bool missionsInGameScenes = true;
        [Tooltip("Mission UI prefab (assigned by Tools/Missions/Create Mission UI Prefab).")]
        public GameObject missionUIPrefab;
        [Tooltip("Show the controls tutorial before the mission briefing.")]
        public bool showTutorial = true;

        [Header("Drilling")]
        [Tooltip("Horizontal distance from the drill tip (or rover center) to the edge of the rock to be able to drill (m). Same as the analysis window.")]
        public float roverReach = 4f;
        [Tooltip("Drilling seconds per hardness point (hardness 5 = 10 s).")]
        public float secondsPerHardness = 2f;

        [Header("Battery")]
        [Tooltip("Minutes of continuous driving per 1000 Ah of battery capacity (Niquelin 5 kAh = 5 × this value).")]
        public float driveMinutesPerKAh = 1.5f;
        [Tooltip("Drilling drain as a fraction of driving drain (small).")]
        [Range(0f, 1f)] public float drillDrainFactor = 0.15f;
        [Tooltip("Capacity used when the scene is played without choosing a battery in the lobby (Ah).")]
        public float defaultBatteryAh = 15000f;

        public static MissionsRuntimeSettings Load() => Resources.Load<MissionsRuntimeSettings>(ResourcePath);
    }
}
