// MissionDefinitionEditor.cs — MissionDefinition inspector with warnings (rover tubes and thresholds).
using UnityEditor;

namespace Game.Missions.EditorTools
{
    [CustomEditor(typeof(MissionDefinition))]
    public class MissionDefinitionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var m = (MissionDefinition)target;

            if (m.ExceedsTubeCapacity)
                EditorGUILayout.HelpBox(
                    $"This mission asks for {m.TotalRequired} rocks but the rover only carries {MissionDefinition.RoverTubeCapacity} sample tubes.",
                    MessageType.Warning);
            if (!m.gradeThresholds.IsOrdered)
                EditorGUILayout.HelpBox("Thresholds must satisfy 0 < A ≤ B ≤ C (seconds).", MessageType.Warning);
            foreach (var o in m.objectives)
                if (o == null || o.rockType == null)
                {
                    EditorGUILayout.HelpBox("An objective has no rock type.", MessageType.Error);
                    break;
                }

            EditorGUILayout.LabelField("Total rocks", $"{m.TotalRequired} / {MissionDefinition.RoverTubeCapacity} tubes");
            DrawDefaultInspector();
        }
    }
}
