// RockTypeImporter.cs — Tools/Missions/Import Rock Types From rocks.json
// Reads the rocks JSON and creates or updates the 5 RockTypeData (id, display names, color and hardness).
// Default source: rocks_info.json from the rock window; otherwise any rocks.json in the project.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Missions.EditorTools
{
    public static class RockTypeImporter
    {
        const string DefaultJson = "Assets/RocasMarte_UI/Resources/rocks_info.json";

        // Only what is needed is read; JsonUtility ignores the other fields.
        [Serializable] class JsonRock { public string id; public int hardness; }
        [Serializable] class JsonRockList { public JsonRock[] rocks; }

        // Display names and UI color of each type (game texts are tuned here).
        static readonly Dictionary<string, (string display, string shortName, Color color)> Names =
            new Dictionary<string, (string, string, Color)>
        {
            ["Rochette"]     = ("Máaz basalt (Rochette)",                 "Basalt",  new Color(0.55f, 0.58f, 0.62f)),
            ["Seitah"]       = ("Olivine cumulate (Séítah)",             "Olivine", new Color(0.47f, 0.66f, 0.33f)),
            ["CheyavaFalls"] = ("Bright Angel mudstone (Cheyava Falls)", "Mudstone", new Color(0.80f, 0.42f, 0.30f)),
            ["BroomPoint"]   = ("Impact breccia (Broom Point)",          "Breccia", new Color(0.85f, 0.72f, 0.45f)),
            ["Singita"]      = ("Gabbro (Singita)",                      "Gabbro",  new Color(0.90f, 0.88f, 0.80f)),
        };

        [MenuItem("Tools/Missions/Import Rock Types From rocks.json", priority = 1)]
        public static void ImportMenu()
        {
            var path = FindJson();
            if (path == null)
            {
                EditorUtility.DisplayDialog("Missions", "Could not find rocks_info.json or rocks.json in the project.", "OK");
                return;
            }
            int n = Import(path);
            EditorUtility.DisplayDialog("Missions", $"{n} rock types imported from\n{path}", "OK");
        }

        /// <summary>Imports and returns how many types were created or updated.</summary>
        public static int Import(string jsonPath)
        {
            var list = JsonUtility.FromJson<JsonRockList>(File.ReadAllText(jsonPath));
            if (list?.rocks == null) { Debug.LogError("[Missions] JSON without a 'rocks' list: " + jsonPath); return 0; }

            MissionsEditorPaths.EnsureFolder(MissionsEditorPaths.DataRockTypes);
            int n = 0;
            foreach (var r in list.rocks)
            {
                // generic entries of the rock window (Generic_*) are not mission types
                if (string.IsNullOrEmpty(r.id) || !Names.TryGetValue(r.id, out var names)) continue;

                var assetPath = $"{MissionsEditorPaths.DataRockTypes}/RockType_{r.id}.asset";
                var data = AssetDatabase.LoadAssetAtPath<RockTypeData>(assetPath);
                bool isNew = data == null;
                if (isNew) data = ScriptableObject.CreateInstance<RockTypeData>();

                data.id = r.id;
                data.displayName = names.display;
                data.shortName = names.shortName;
                data.uiColor = names.color;
                data.hardness = Mathf.Clamp(r.hardness, 1, 5);
                // the icon is not touched: it is kept if already assigned

                if (isNew) AssetDatabase.CreateAsset(data, assetPath);
                else EditorUtility.SetDirty(data);
                n++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Missions] {n} RockTypeData imported from {jsonPath}");
            return n;
        }

        static string FindJson()
        {
            if (File.Exists(DefaultJson)) return DefaultJson;
            foreach (var guid in AssetDatabase.FindAssets("rocks t:TextAsset"))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(p) == "rocks.json") return p;
            }
            return null;
        }

        /// <summary>Loads the RockTypeData of an id (null if missing).</summary>
        public static RockTypeData Load(string id) =>
            AssetDatabase.LoadAssetAtPath<RockTypeData>($"{MissionsEditorPaths.DataRockTypes}/RockType_{id}.asset");
    }
}
