// MissionsSceneBootstrap.cs — engancha las misiones a las escenas del juego SIN modificarlas.
// Al cargar una escena con RoverController (si MissionsRuntimeSettings.autoSetupInGameScenes está activo):
//   1) añade SimpleSamplingInteractor al rover (perforador provisional, tecla R);
//   2) FieldRockIndex: indexa todas las rocas del campo de rocas; la que el perforador tiene delante recibe
//      RockSampleTarget + un DrillTarget en su parte superior con una esfera trigger de 0.3 m (al momento).
//   3) si está activado, crea MissionManager + MissionUI.
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Missions
{
    public static class MissionsSceneBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Setup(SceneManager.GetActiveScene());
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Setup(scene);

        static void Setup(Scene scene)
        {
            var settings = MissionsRuntimeSettings.Load();
            if (settings == null || !settings.autoSetupInGameScenes) return;

            RoverController rover = null;
            foreach (var root in scene.GetRootGameObjects())
                if (rover == null) rover = root.GetComponentInChildren<RoverController>(true);
            if (rover == null) return;

            if (rover.GetComponent<SimpleSamplingInteractor>() == null)
            {
                var it = rover.gameObject.AddComponent<SimpleSamplingInteractor>();
                it.reach = settings.roverReach;
                it.secondsPerHardness = settings.secondsPerHardness;
            }

            int n = FieldRockIndex.Build(scene, settings);
            if (n > 0) Debug.Log($"[Misiones] {n} rocas del terreno se pueden perforar en '{scene.name}' (tecla R).");

            // misiones (briefing, HUD y resultados) sin tocar la escena
            if (settings.missionsInGameScenes && settings.database != null && settings.missionUIPrefab != null
                && Object.FindAnyObjectByType<MissionManager>() == null)
            {
                var mgrGo = new GameObject("MissionManager (auto)");
                var mgr = mgrGo.AddComponent<MissionManager>();
                mgr.database = settings.database;
                mgrGo.AddComponent<MissionDebugShortcuts>();
                Object.Instantiate(settings.missionUIPrefab).name = "MissionUI (auto)";
                Debug.Log($"[Misiones] Misiones activas en '{scene.name}': briefing de '{settings.database.missions[0].title}'.");
            }
        }
    }

    /// <summary>
    /// Índice de TODAS las rocas del campo de rocas ("Decoration/Obstacle/Sample &lt;Tipo&gt;", ~11 700).
    /// No añade nada por adelantado: RockSampleTarget se crea solo en la roca que el perforador tiene delante.
    /// El tipo de roca sale del nombre (Rochette = basalto, Seitah = olivino…), igual que en la ventana de análisis.
    /// </summary>
    public static class FieldRockIndex
    {
        public const float TriggerRadius = 0.3f;

        public struct Entry
        {
            public GameObject go;
            public Vector3 center;
            public float radius;
            public RockTypeData type;
        }

        public static readonly System.Collections.Generic.List<Entry> Entries = new System.Collections.Generic.List<Entry>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Entries.Clear();

        public static int Build(Scene scene, MissionsRuntimeSettings settings)
        {
            Entries.Clear();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var n = mr.gameObject.name;
                    if (!n.StartsWith("Sample ") && !n.StartsWith("Decoration ") && !n.StartsWith("Obstacle ")) continue;
                    RockTypeData type = null;
                    foreach (var t in settings.rockTypes)
                        if (t != null && n.Contains(t.id)) { type = t; break; }
                    if (type == null) continue;
                    var b = mr.bounds;
                    Entries.Add(new Entry { go = mr.gameObject, center = b.center, radius = Mathf.Max(b.extents.x, b.extents.z), type = type });
                }
            return Entries.Count;
        }

        /// <summary>Devuelve el RockSampleTarget de la roca, creándolo la primera vez (DrillTarget arriba + trigger de 0.3 m).</summary>
        public static RockSampleTarget Acquire(Entry e)
        {
            if (e.go == null) return null;
            var target = e.go.GetComponent<RockSampleTarget>();
            if (target != null) return target;

            var r = e.go.GetComponent<Renderer>();
            var top = r != null ? r.bounds.center + Vector3.up * r.bounds.extents.y : e.go.transform.position;
            var drill = new GameObject($"INT_ROCK_{e.type.id}_DrillTarget");
            drill.transform.SetParent(e.go.transform, false);
            drill.transform.position = top;
            var trg = drill.AddComponent<SphereCollider>();
            trg.isTrigger = true;
            trg.radius = TriggerRadius / Mathf.Max(0.0001f, drill.transform.lossyScale.x);

            target = e.go.AddComponent<RockSampleTarget>();
            target.rockType = e.type;
            target.drillTarget = drill.transform;
            return target;
        }
    }
}
