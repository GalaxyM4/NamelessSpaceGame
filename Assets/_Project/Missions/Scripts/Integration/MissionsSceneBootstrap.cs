// MissionsSceneBootstrap.cs — hooks the missions into the game scenes WITHOUT modifying them.
// When a scene with a RoverController loads (if MissionsRuntimeSettings.autoSetupInGameScenes is on):
//   1) adds SimpleSamplingInteractor (provisional drill, R key) and RoverPowerSystem (battery) to the rover;
//   2) FieldRockIndex: indexes every rock of the rock field; the one in front of the drill gets a
//      RockSampleTarget + a DrillTarget on its top with a 0.3 m trigger sphere (on demand);
//   3) if enabled, creates MissionManager + MissionUI, and the controls tutorial before the briefing.
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

            var drill = rover.GetComponent<SimpleSamplingInteractor>();
            if (drill == null)
            {
                drill = rover.gameObject.AddComponent<SimpleSamplingInteractor>();
                drill.reach = settings.roverReach;
                drill.secondsPerHardness = settings.secondsPerHardness;
            }
            var power = rover.GetComponent<RoverPowerSystem>();
            if (power == null) power = rover.gameObject.AddComponent<RoverPowerSystem>();
            power.Configure(settings);

            int n = FieldRockIndex.Build(scene, settings);
            if (n > 0) Debug.Log($"[Missions] {n} terrain rocks can be drilled in '{scene.name}' (R key).");

            // missions (tutorial, briefing, HUD and results) without touching the scene
            if (settings.missionsInGameScenes && settings.database != null && settings.missionUIPrefab != null
                && Object.FindAnyObjectByType<MissionManager>() == null)
            {
                var mgrGo = new GameObject("MissionManager (auto)");
                var mgr = mgrGo.AddComponent<MissionManager>();
                mgr.database = settings.database;
                mgrGo.AddComponent<MissionDebugShortcuts>();

                // the tutorial must exist before the UI so the UI can subscribe to it
                if (settings.showTutorial)
                {
                    mgr.briefingOnStart = false;
                    var tut = mgrGo.AddComponent<MissionTutorial>();
                    tut.manager = mgr;
                    tut.rover = rover.transform;
                }
                Object.Instantiate(settings.missionUIPrefab).name = "MissionUI (auto)";
                Debug.Log($"[Missions] Missions active in '{scene.name}'" + (settings.showTutorial ? " (tutorial first)." : "."));
            }
        }
    }

    /// <summary>
    /// Index of EVERY rock of the rock field ("Decoration/Obstacle/Sample &lt;Type&gt;", ~11,700).
    /// Nothing is added up front: a RockSampleTarget is created only on the rock in front of the drill.
    /// The rock type comes from the name (Rochette = basalt, Seitah = olivine…), like in the analysis window.
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

        /// <summary>Returns the rock's RockSampleTarget, creating it the first time (DrillTarget on top + 0.3 m trigger).</summary>
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
