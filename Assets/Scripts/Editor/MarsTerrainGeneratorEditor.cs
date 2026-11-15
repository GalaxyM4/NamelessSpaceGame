using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MarsTerrainGenerator))]
public class MarsTerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MarsTerrainGenerator generator = (MarsTerrainGenerator)target;
        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Mars Terrain", GUILayout.Height(30)))
        {
            BakeTerrain(generator);
        }

        if (GUILayout.Button("Randomize Seed & Generate", GUILayout.Height(24)))
        {
            Undo.RecordObject(generator, "Randomize Mars Terrain Seed");
            generator.seed = Random.Range(1, 999999);
            EditorUtility.SetDirty(generator);
            BakeTerrain(generator);
        }
    }

    private static void BakeTerrain(MarsTerrainGenerator generator)
    {
        Terrain terrain = generator.GetComponent<Terrain>();
        if (terrain != null && terrain.terrainData != null)
        {
            Undo.RegisterCompleteObjectUndo(terrain.terrainData, "Generate Mars Terrain");
            generator.GenerateTerrain();
            EditorUtility.SetDirty(terrain.terrainData);
            AssetDatabase.SaveAssets();
        }
        else
        {
            generator.GenerateTerrain();
        }
    }
}
