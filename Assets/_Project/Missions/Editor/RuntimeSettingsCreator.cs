// RuntimeSettingsCreator.cs — Tools/Missions/Create Runtime Settings
// Creates or updates Resources/MissionsRuntimeSettings with the database and the 5 rock types.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Missions.EditorTools
{
    public static class RuntimeSettingsCreator
    {
        public const string Path = MissionsEditorPaths.Root + "/Resources/MissionsRuntimeSettings.asset";
        static readonly string[] Ids = { "CheyavaFalls", "Seitah", "Rochette", "BroomPoint", "Singita" };

        [MenuItem("Tools/Missions/Create Runtime Settings", priority = 3)]
        public static void CreateMenu()
        {
            var s = CreateOrUpdate();
            Selection.activeObject = s;
            Debug.Log("[Missions] Runtime settings: " + Path);
        }

        public static MissionsRuntimeSettings CreateOrUpdate()
        {
            MissionsEditorPaths.EnsureFolder(MissionsEditorPaths.Root + "/Resources");
            var s = AssetDatabase.LoadAssetAtPath<MissionsRuntimeSettings>(Path);
            bool isNew = s == null;
            if (isNew) s = ScriptableObject.CreateInstance<MissionsRuntimeSettings>();

            s.database = AssetDatabase.LoadAssetAtPath<MissionDatabase>(MissionsEditorPaths.DatabasePath);
            s.missionUIPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MissionUIPrefabCreator.PrefabPath);
            if (s.roverReach < 4f) s.roverReach = 4f;   // same reach as the analysis window
            s.rockTypes = new List<RockTypeData>();
            foreach (var id in Ids)
            {
                var t = RockTypeImporter.Load(id);
                if (t != null) s.rockTypes.Add(t);
            }

            if (isNew) AssetDatabase.CreateAsset(s, Path);
            else EditorUtility.SetDirty(s);
            AssetDatabase.SaveAssets();
            return s;
        }
    }
}
