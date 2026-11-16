// RocasMarteBootstrap.cs — connects the rock window with the terrain rock field, without touching the scene.
// When a scene with a RoverController and the rock field (objects "Decoration/Obstacle/Sample <Type>") loads:
//   - registers every rock in RockRegistry:
//       Sample <Type>            -> panel of the real rock (rocks_info.json: CheyavaFalls, Seitah, ...)
//       Decoration/Obstacle ...  -> name of its rock type: BASALT, GABBRO... (Generic_<Type>)
//   - creates UI_RockInfo (UIDocument + RockInfoUI) and adds RoverRockScanner to the rover (F key).
// To disable it: Resources/RocasMarte_Config -> Auto Setup = off.
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace RocasMarte
{
    public static class RocasMarteBootstrap
    {
        static readonly string[] Families = { "CheyavaFalls", "Seitah", "Rochette", "BroomPoint", "Singita" };

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
            var cfg = Resources.Load<RocasMarteConfig>("RocasMarte_Config");
            if (cfg != null && !cfg.autoSetup) return;

            RoverController rover = null;
            foreach (var root in scene.GetRootGameObjects())
                if (rover == null) rover = root.GetComponentInChildren<RoverController>(true);
            if (rover == null) return;

            RockRegistry.Field.Clear();
            int named = 0, generic = 0;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var n = mr.gameObject.name;
                    bool sample = n.StartsWith("Sample ");
                    if (!sample && !n.StartsWith("Decoration ") && !n.StartsWith("Obstacle ")) continue;
                    var fam = Family(n);
                    if (fam == null) continue;
                    var b = mr.bounds;
                    RockRegistry.Field.Add(new RockRegistry.Entry
                    {
                        position = b.center,
                        radius = Mathf.Max(b.extents.x, b.extents.z),
                        id = sample ? fam : "Generic_" + fam,
                    });
                    if (sample) named++; else generic++;
                }
            if (RockRegistry.Field.Count == 0) return;

            var ui = Object.FindAnyObjectByType<RockInfoUI>();
            if (ui == null)
            {
                var go = new GameObject("UI_RockInfo");
                go.SetActive(false);
                var doc = go.AddComponent<UIDocument>();
                doc.panelSettings = cfg != null ? cfg.panelSettings : null;
                doc.visualTreeAsset = cfg != null ? cfg.panel : null;
                doc.sortingOrder = 50;
                ui = go.AddComponent<RockInfoUI>();
                if (cfg != null) { ui.titleFont = cfg.titleFont; ui.headFont = cfg.headFont; }
                go.SetActive(true);
            }

            var scanner = rover.GetComponent<RoverRockScanner>();
            if (scanner == null) scanner = rover.gameObject.AddComponent<RoverRockScanner>();
            scanner.ui = ui;
            if (cfg != null) scanner.range = cfg.range;
            // measured from the rover's drill tip if it exists (the same reference the drilling uses)
            if (scanner.scanOrigin == null)
                foreach (var t in rover.GetComponentsInChildren<Transform>(true))
                    if (t.name == "INT_ROV_Drill_Tip") { scanner.scanOrigin = t; break; }

            Debug.Log($"[Mars Rocks] {named + generic} inspectable rocks in '{scene.name}' ({named} catalogued samples, {generic} uncatalogued rocks). Key {scanner.interactKey}.");
        }

        static string Family(string objectName)
        {
            foreach (var f in Families)
                if (objectName.Contains(f)) return f;
            return null;
        }
    }
}
