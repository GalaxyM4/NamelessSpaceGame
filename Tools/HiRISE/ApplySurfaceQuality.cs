using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ApplySurfaceQuality
{
    public static object Run()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/Scenes/MarsScene.unity" || scene.isDirty)
            throw new InvalidOperationException("Open the saved MarsScene in Edit Mode first.");
        Terrain terrain = UnityEngine.Object.FindAnyObjectByType<MarsTerrainGenerator>().GetComponent<Terrain>();
        string before = HeightHash(terrain);
        MarsTerrainSurface.Apply(terrain, "Assets/Textures/HiRISE/GaleOrtho.png");
        string after = HeightHash(terrain);
        if (before != after) throw new Exception("Surface setup unexpectedly changed terrain heights.");
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        return new { heightsUnchanged = true, heightHash = after,
            material = terrain.materialTemplate.shader.name };
    }

    private static string HeightHash(Terrain terrain)
    {
        int n = terrain.terrainData.heightmapResolution;
        float[,] heights = terrain.terrainData.GetHeights(0, 0, n, n);
        byte[] bytes = new byte[heights.Length * sizeof(float)];
        Buffer.BlockCopy(heights, 0, bytes, 0, bytes.Length);
        using (var sha = System.Security.Cryptography.SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(bytes));
    }
}
