// MissionsEditorPaths.cs — rutas comunes de los menús Tools/Missions.
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

        /// <summary>Crea la carpeta (y sus padres) en el AssetDatabase si no existe.</summary>
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
