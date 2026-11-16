// MissionsEditorPaths.cs — common paths for the Tools/Missions menus.
using System.IO;
using UnityEditor;

namespace Game.Missions.EditorTools
{
    internal static class MissionsEditorPaths
    {
        public const string Root = "Assets/_Project/Missions";
        public const string DataRockTypes = Root + "/Data/RockTypes";
        public const string DataMissions = Root + "/Data/Missions";
        public const string DatabasePath = Root + "/Data/MissionDatabase.asset";

        /// <summary>Creates the folder (and its parents) in the AssetDatabase if it doesn't exist.</summary>
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
