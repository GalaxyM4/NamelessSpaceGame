// SampleMissionsCreator.cs — Tools/Missions/Create Sample Missions
// Creates (or updates) the 5 sample missions and the MissionDatabase with them in order.
// The times are initial values: tune them by playtesting.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Missions.EditorTools
{
    public static class SampleMissionsCreator
    {
        [MenuItem("Tools/Missions/Create Sample Missions", priority = 2)]
        public static void CreateMenu()
        {
            // rock types are required: if missing, they are imported first
            if (RockTypeImporter.Load("Rochette") == null) RockTypeImporter.ImportMenu();
            if (RockTypeImporter.Load("Rochette") == null) return;

            var db = Create();
            Selection.activeObject = db;
            EditorUtility.DisplayDialog("Missions", $"{db.missions.Count} sample missions created in\n{MissionsEditorPaths.DataMissions}", "OK");
        }

        public static MissionDatabase Create()
        {
            var rochette = RockTypeImporter.Load("Rochette");
            var seitah = RockTypeImporter.Load("Seitah");
            var cheyava = RockTypeImporter.Load("CheyavaFalls");
            var broom = RockTypeImporter.Load("BroomPoint");
            var singita = RockTypeImporter.Load("Singita");

            MissionsEditorPaths.EnsureFolder(MissionsEditorPaths.DataMissions);
            var list = new List<MissionDefinition>
            {
                Mission("01_FirstCore", "First core",
                    "Drill five basalt rocks of the Máaz formation, like Rochette, where Perseverance's first two samples came from.",
                    180, 300, 480, (rochette, 5)),
                Mission("02_VolcanicHistory", "Volcanic history",
                    "Collect igneous rocks from the floor of Jezero: three olivine cumulates from Séítah and two Máaz basalts to rebuild the crater's volcanic history.",
                    200, 330, 520, (seitah, 3), (rochette, 2)),
                Mission("03_AncientLifeClues", "Clues of ancient life",
                    "Take three mudstone samples like Cheyava Falls: mud hardened in water with \"leopard spots\", a potential biosignature.",
                    150, 260, 420, (cheyava, 3)),
                Mission("04_ImpactScars", "Impact scars",
                    "Drill four impact breccias like Broom Point, rocks shattered and welded by the asteroids that hit early Mars.",
                    220, 360, 560, (broom, 4)),
                Mission("05_OldestRocks", "The oldest rocks",
                    "Get two gabbros like Singita, among the oldest and hardest rocks on Mars, and one sample of each of the other types.",
                    300, 480, 720, (singita, 2), (rochette, 1), (seitah, 1), (cheyava, 1), (broom, 1)),
            };

            var db = AssetDatabase.LoadAssetAtPath<MissionDatabase>(MissionsEditorPaths.DatabasePath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<MissionDatabase>();
                AssetDatabase.CreateAsset(db, MissionsEditorPaths.DatabasePath);
            }
            db.missions = list;
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Missions] {list.Count} sample missions created/updated.");
            return db;
        }

        static MissionDefinition Mission(string id, string title, string description, float a, float b, float c,
                                         params (RockTypeData type, int amount)[] objectives)
        {
            var path = $"{MissionsEditorPaths.DataMissions}/Mission_{id}.asset";
            var m = AssetDatabase.LoadAssetAtPath<MissionDefinition>(path);
            bool isNew = m == null;
            if (isNew) m = ScriptableObject.CreateInstance<MissionDefinition>();

            m.id = id;
            m.title = title;
            m.description = description;
            m.gradeThresholds = new GradeThresholds(a, b, c);
            m.timeLimit = 0f;
            m.objectives = new List<MissionObjective>();
            foreach (var (type, amount) in objectives)
                m.objectives.Add(new MissionObjective { rockType = type, amount = amount });

            if (isNew) AssetDatabase.CreateAsset(m, path);
            else EditorUtility.SetDirty(m);
            return m;
        }
    }
}
