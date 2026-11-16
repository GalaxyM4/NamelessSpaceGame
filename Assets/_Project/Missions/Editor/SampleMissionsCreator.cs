// SampleMissionsCreator.cs — Tools/Missions/Create Sample Missions
// Crea (o actualiza) las 5 misiones de ejemplo y la MissionDatabase con ellas en orden.
// Los tiempos son iniciales: se ajustan probando.
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
            // los tipos de roca son necesarios: si faltan, se importan primero
            if (RockTypeImporter.Load("Rochette") == null) RockTypeImporter.ImportMenu();
            if (RockTypeImporter.Load("Rochette") == null) return;

            var db = Create();
            Selection.activeObject = db;
            EditorUtility.DisplayDialog("Misiones", $"{db.missions.Count} misiones de ejemplo creadas en\n{MissionsEditorPaths.DataMissions}", "OK");
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
                Mission("01_PrimerNucleo", "Primer núcleo",
                    "Perfora cinco rocas de basalto de la formación Máaz, como Rochette, de donde salieron las dos primeras muestras de Perseverance.",
                    180, 300, 480, (rochette, 5)),
                Mission("02_HistoriaVolcanica", "Historia volcánica",
                    "Reúne rocas ígneas del fondo de Jezero: tres cumulados de olivino de Séítah y dos basaltos de Máaz para reconstruir la historia volcánica del cráter.",
                    200, 330, 520, (seitah, 3), (rochette, 2)),
                Mission("03_PistasVidaAntigua", "Pistas de vida antigua",
                    "Toma tres muestras de lutita como Cheyava Falls: lodo endurecido en agua con «manchas de leopardo», una posible biofirma.",
                    150, 260, 420, (cheyava, 3)),
                Mission("04_CicatricesImpacto", "Cicatrices de impacto",
                    "Perfora cuatro brechas de impacto como Broom Point, rocas rotas y soldadas por los asteroides que golpearon el Marte primitivo.",
                    220, 360, 560, (broom, 4)),
                Mission("05_RocasMasAntiguas", "Las rocas más antiguas",
                    "Consigue dos gabros como Singita, de las rocas más antiguas y duras de Marte, y una muestra de cada uno de los otros tipos.",
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
            Debug.Log($"[Misiones] {list.Count} misiones de ejemplo creadas/actualizadas.");
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
