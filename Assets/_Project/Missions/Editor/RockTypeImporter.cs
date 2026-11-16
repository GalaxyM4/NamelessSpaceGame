// RockTypeImporter.cs — Tools/Missions/Import Rock Types From rocks.json
// Lee el JSON de rocas y crea o actualiza los 5 RockTypeData (id, nombres en español, color y dureza).
// Fuente por defecto: rocks_info.json de la ventana de rocas; si no está, busca cualquier rocks.json del proyecto.
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

        // Solo leemos lo necesario; JsonUtility ignora el resto de campos.
        [Serializable] class JsonRock { public string id; public int hardness; }
        [Serializable] class JsonRockList { public JsonRock[] rocks; }

        // Nombres en español y color de UI de cada tipo (los textos de juego se ajustan aquí).
        static readonly Dictionary<string, (string display, string shortName, Color color)> Names =
            new Dictionary<string, (string, string, Color)>
        {
            ["Rochette"]     = ("Basalto de Máaz (Rochette)",            "Basalto", new Color(0.55f, 0.58f, 0.62f)),
            ["Seitah"]       = ("Cumulado de olivino (Séítah)",          "Olivino", new Color(0.47f, 0.66f, 0.33f)),
            ["CheyavaFalls"] = ("Lutita de Bright Angel (Cheyava Falls)", "Lutita",  new Color(0.80f, 0.42f, 0.30f)),
            ["BroomPoint"]   = ("Brecha de impacto (Broom Point)",       "Brecha",  new Color(0.85f, 0.72f, 0.45f)),
            ["Singita"]      = ("Gabro (Singita)",                       "Gabro",   new Color(0.90f, 0.88f, 0.80f)),
        };

        [MenuItem("Tools/Missions/Import Rock Types From rocks.json", priority = 1)]
        public static void ImportMenu()
        {
            var path = FindJson();
            if (path == null)
            {
                EditorUtility.DisplayDialog("Misiones", "No encuentro rocks_info.json ni rocks.json en el proyecto.", "OK");
                return;
            }
            int n = Import(path);
            EditorUtility.DisplayDialog("Misiones", $"{n} tipos de roca importados desde\n{path}", "OK");
        }

        /// <summary>Importa y devuelve cuántos tipos se crearon o actualizaron.</summary>
        public static int Import(string jsonPath)
        {
            var list = JsonUtility.FromJson<JsonRockList>(File.ReadAllText(jsonPath));
            if (list?.rocks == null) { Debug.LogError("[Misiones] JSON sin lista 'rocks': " + jsonPath); return 0; }

            MissionsEditorPaths.EnsureFolder(MissionsEditorPaths.DataRockTypes);
            int n = 0;
            foreach (var r in list.rocks)
            {
                // las entradas genéricas de la ventana de rocas (Generic_*) no son tipos de misión
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
                // el icono no se toca: se conserva si ya estaba asignado

                if (isNew) AssetDatabase.CreateAsset(data, assetPath);
                else EditorUtility.SetDirty(data);
                n++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Misiones] {n} RockTypeData importados desde {jsonPath}");
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

        /// <summary>Carga el RockTypeData de un id (null si no existe).</summary>
        public static RockTypeData Load(string id) =>
            AssetDatabase.LoadAssetAtPath<RockTypeData>($"{MissionsEditorPaths.DataRockTypes}/RockType_{id}.asset");
    }
}
