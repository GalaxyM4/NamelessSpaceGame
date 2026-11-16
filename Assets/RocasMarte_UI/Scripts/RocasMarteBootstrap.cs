// RocasMarteBootstrap.cs — conecta la ventana de rocas con el campo de rocas del terreno, sin tocar la escena.
// Al cargar una escena con RoverController y el campo de rocas (objetos "Decoration/Obstacle/Sample <Tipo>"):
//   - registra todas las rocas en RockRegistry:
//       Sample <Tipo>            -> ficha de la roca real (rocks_info.json: CheyavaFalls, Seitah, ...)
//       Decoration/Obstacle ...  -> nombre de su tipo de roca: BASALTO, GABRO... (Generic_<Tipo>)
//   - crea UI_RockInfo (UIDocument + RockInfoUI) y añade RoverRockScanner al rover (tecla F).
// Para desactivarlo: Resources/RocasMarte_Config -> Auto Setup = off.
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

            Debug.Log($"[Rocas Marte] {named + generic} rocas inspeccionables en '{scene.name}' ({named} muestras con ficha, {generic} rocas sin catalogar). Tecla {scanner.interactKey}.");
        }

        static string Family(string objectName)
        {
            foreach (var f in Families)
                if (objectName.Contains(f)) return f;
            return null;
        }
    }
}
