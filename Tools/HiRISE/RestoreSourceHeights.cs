using System;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Restore source elevations without reapplying or reimporting surface assets.</summary>
public static class RestoreSourceHeights
{
    public static object Run()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/Scenes/MarsScene.unity" || scene.isDirty)
            throw new InvalidOperationException("Open the saved MarsScene in Edit Mode first.");
        var generator = UnityEngine.Object.FindAnyObjectByType<MarsTerrainGenerator>();
        Terrain terrain = generator.GetComponent<Terrain>();
        TerrainData data = terrain.terrainData;
        TerrainCollider collider = terrain.GetComponent<TerrainCollider>();
        Material material = terrain.materialTemplate;
        string materialBefore = EditorJsonUtility.ToJson(material);
        TerrainLayer[] layers = data.terrainLayers;
        string[] layerBefore = Array.ConvertAll(layers, layer => EditorJsonUtility.ToJson(layer));
        string weightsBefore = WeightHash(data);
        float basemapDistance = terrain.basemapDistance;

        MarsTerrainGeneratorEditor.BakeTerrain(generator, applySurface: false);

        if (terrain.terrainData != data || collider.terrainData != data ||
            terrain.materialTemplate != material || EditorJsonUtility.ToJson(material) != materialBefore ||
            terrain.basemapDistance != basemapDistance || WeightHash(data) != weightsBefore ||
            data.terrainLayers.Length != layers.Length)
            throw new Exception("Height-only bake unexpectedly changed surface or terrain references.");
        for (int i = 0; i < layers.Length; i++)
            if (data.terrainLayers[i] != layers[i] || EditorJsonUtility.ToJson(layers[i]) != layerBefore[i])
                throw new Exception("Height-only bake unexpectedly changed a terrain layer.");

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        return new
        {
            sourceElevationsRestored = true, gameplayHeightModificationMeters = 0,
            surfaceUnchanged = true, terrainAndColliderReferencesPreserved = true,
            resolution = data.heightmapResolution,
            note = "Rover is positioned above the unmodified ground; no spawn flattening."
        };
    }

    private static string WeightHash(TerrainData data)
    {
        float[,,] weights = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);
        byte[] bytes = new byte[weights.Length * sizeof(float)];
        Buffer.BlockCopy(weights, 0, bytes, 0, bytes.Length);
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(bytes));
    }
}
