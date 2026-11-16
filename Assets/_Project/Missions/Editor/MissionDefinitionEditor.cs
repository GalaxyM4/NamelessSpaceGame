// MissionDefinitionEditor.cs — inspector de MissionDefinition con avisos (tubos del rover y umbrales).
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
                    $"Esta misión pide {m.TotalRequired} rocas y el rover solo lleva {MissionDefinition.RoverTubeCapacity} tubos de muestra.",
                    MessageType.Warning);
            if (!m.gradeThresholds.IsOrdered)
                EditorGUILayout.HelpBox("Los umbrales deben cumplir 0 < A ≤ B ≤ C (segundos).", MessageType.Warning);
            foreach (var o in m.objectives)
                if (o == null || o.rockType == null)
                {
                    EditorGUILayout.HelpBox("Hay un objetivo sin tipo de roca.", MessageType.Error);
                    break;
                }

            EditorGUILayout.LabelField("Total de rocas", $"{m.TotalRequired} / {MissionDefinition.RoverTubeCapacity} tubos");
            DrawDefaultInspector();
        }
    }
}
