using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MarsRockScatterer))]
public class MarsRockScattererEditor : Editor
{
    private const string GeneratedFolder = "Assets/Generated/MarsRocks";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MarsRockScatterer scatterer = (MarsRockScatterer)target;
        EditorGUILayout.Space();

        if (GUILayout.Button("Scatter Mars Rocks", GUILayout.Height(30)))
        {
            EnsureSavedRockAssets(scatterer);
            Undo.RegisterFullObjectHierarchyUndo(scatterer.gameObject, "Scatter Mars Rocks");
            scatterer.ScatterRocks();
            EditorUtility.SetDirty(scatterer);
        }

        if (GUILayout.Button("Randomize Seed & Scatter", GUILayout.Height(24)))
        {
            EnsureSavedRockAssets(scatterer);
            Undo.RegisterFullObjectHierarchyUndo(scatterer.gameObject, "Randomize & Scatter Mars Rocks");
            scatterer.seed = Random.Range(1, 999999);
            scatterer.ScatterRocks();
            EditorUtility.SetDirty(scatterer);
        }

        if (GUILayout.Button("Clear Mars Rocks", GUILayout.Height(22)))
        {
            Undo.RegisterFullObjectHierarchyUndo(scatterer.gameObject, "Clear Mars Rocks");
            scatterer.ClearRocks();
            EditorUtility.SetDirty(scatterer);
        }
    }

    public static void EnsureSavedRockAssets(MarsRockScatterer scatterer)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Generated"))
        {
            AssetDatabase.CreateFolder("Assets", "Generated");
        }
        if (!AssetDatabase.IsValidFolder(GeneratedFolder))
        {
            AssetDatabase.CreateFolder("Assets/Generated", "MarsRocks");
        }

        if (scatterer.rockMeshes == null || scatterer.rockMeshes.Length == 0)
        {
            Mesh[] meshes = new Mesh[4];
            for (int i = 0; i < 4; i++)
            {
                string meshPath = $"{GeneratedFolder}/MarsRock_{i}.asset";
                Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (existing == null)
                {
                    existing = MarsRockMeshBuilder.CreateRockMesh(scatterer.seed + i * 97);
                    AssetDatabase.CreateAsset(existing, meshPath);
                }
                meshes[i] = existing;
            }
            scatterer.rockMeshes = meshes;
        }

        if (scatterer.rockMaterial == null)
        {
            string matPath = $"{GeneratedFolder}/MarsBasaltRock.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
                if (urpLit == null) urpLit = Shader.Find("Standard");
                mat = new Material(urpLit)
                {
                    color = new Color(0.36f, 0.21f, 0.16f, 1f)
                };
                mat.SetFloat("_Smoothness", 0.15f);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            scatterer.rockMaterial = mat;
        }

        AssetDatabase.SaveAssets();
    }
}
