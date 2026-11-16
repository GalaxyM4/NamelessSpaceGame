// MissionsRuntimeSettings.cs — ajustes de las misiones que se leen en runtime (Resources/MissionsRuntimeSettings).
// Lo crea el menú Tools/Missions/Create Runtime Settings.
using System.Collections.Generic;
using UnityEngine;

namespace Game.Missions
{
    public class MissionsRuntimeSettings : ScriptableObject
    {
        public const string ResourcePath = "MissionsRuntimeSettings";

        public MissionDatabase database;
        public List<RockTypeData> rockTypes = new List<RockTypeData>();

        [Header("Escenas del juego")]
        [Tooltip("Al cargar una escena con rover y campo de rocas: añade el perforador al rover y hace muestreables las rocas 'Sample'.")]
        public bool autoSetupInGameScenes = true;
        [Tooltip("Además, crea MissionManager + MissionUI en esas escenas si no existen (muestra el briefing de la primera misión).")]
        public bool missionsInGameScenes = true;
        [Tooltip("Prefab de la UI de misiones (lo asigna Tools/Missions/Create Mission UI Prefab).")]
        public GameObject missionUIPrefab;

        [Header("Perforación")]
        [Tooltip("Distancia horizontal desde el centro del rover hasta el borde de la roca para poder perforar (m). Igual que la de la ventana de análisis.")]
        public float roverReach = 4f;
        [Tooltip("Segundos de perforación por punto de dureza (dureza 5 = 10 s).")]
        public float secondsPerHardness = 2f;


        public static MissionsRuntimeSettings Load() => Resources.Load<MissionsRuntimeSettings>(ResourcePath);
    }
}
