using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BakeRockField
{
    public static object Run()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/Scenes/MarsScene.unity")
            throw new InvalidOperationException("MarsScene in Edit Mode required.");
        RockScatterSettings settings = UnityEngine.Object.FindAnyObjectByType<RockScatterSettings>();
        Terrain terrain = settings.terrain;
        string terrainPath = AssetDatabase.GetAssetPath(terrain.terrainData);
        string before = Hash(terrainPath);
        Material surface = terrain.materialTemplate;
        string surfaceBefore = EditorJsonUtility.ToJson(surface);
        Vector3 roverBefore = settings.rover.position;
        BakedRockField field = RockFieldBaker.Bake(settings);
        if (before != Hash(terrainPath) || terrain.materialTemplate != surface ||
            surfaceBefore != EditorJsonUtility.ToJson(surface) || settings.rover.position != roverBefore)
            throw new Exception("Terrain, surface or rover unexpectedly changed.");
        EditorSceneManager.SaveScene(scene);
        var report = new
        {
            seed = field.seed, layoutSha256 = field.layoutSha256,
            decorativeCount = field.decorativeCount, obstacleCount = field.obstacleCount,
            sampleCount = field.sampleCount, terrainSha256 = before,
            terrainAndSurfaceUnchanged = true, roverPositionPreserved = true
        };
        File.WriteAllText("Tools/Rocks/bake-report.json", JsonUtility.ToJson(new Report
        {
            seed = field.seed, layoutSha256 = field.layoutSha256,
            decorativeCount = field.decorativeCount, obstacleCount = field.obstacleCount,
            sampleCount = field.sampleCount, terrainSha256 = before
        }, true) + "\n");
        return report;
    }

    [Serializable] private sealed class Report
    {
        public int seed, decorativeCount, obstacleCount, sampleCount;
        public string layoutSha256, terrainSha256;
    }

    private static string Hash(string path)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }
}
