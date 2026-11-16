using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>One-time map migration. Keeps live rock tuning and the artistic surface.</summary>
public static class SwitchToJezero
{
    public static object Terrain()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/Scenes/MarsScene.unity")
            throw new InvalidOperationException("Open MarsScene in Edit Mode.");
        var generator = UnityEngine.Object.FindAnyObjectByType<MarsTerrainGenerator>();
        var scatter = UnityEngine.Object.FindAnyObjectByType<RockScatterSettings>();
        var terrain = generator.GetComponent<UnityEngine.Terrain>();
        TerrainData data = terrain.terrainData;
        var collider = terrain.GetComponent<TerrainCollider>();
        Material material = terrain.materialTemplate;
        TerrainLayer[] layers = data.terrainLayers;
        string materialBefore = EditorJsonUtility.ToJson(material);
        string[] layerBefore = layers.Select(layer => EditorJsonUtility.ToJson(layer)).ToArray();
        string weightsBefore = WeightHash(data);
        int candidatesBefore = scatter.samples.count;
        float minimumBurial = scatter.burialFraction, maximumBurial = scatter.maximumBurialFraction;

        const string manifest = "TerrainSources/Prepared/Jezero/manifest.json";
        var source = JsonUtility.FromJson<MarsTerrainGeneratorEditor.SourceManifest>(File.ReadAllText(manifest));
        if (source.sizeMeters != 4096 || source.resolution != 4097 ||
            source.dtmProduct != "DTEEC_045994_1985_046060_1985_U01")
            throw new InvalidOperationException("Prepare the verified 4096 m Jezero crop first.");
        Undo.RecordObject(generator, "Use western Jezero delta source");
        generator.manifestPath = manifest;
        generator.snapTarget = scatter.rover;
        generator.spawnWorldXZ = new Vector2(scatter.rover.position.x, scatter.rover.position.z);
        MarsTerrainGeneratorEditor.BakeTerrain(generator, applySurface: false);

        // The old map's basemap range is too short for a 4 km stochastic surface.
        terrain.basemapDistance = Mathf.Max(terrain.basemapDistance, data.size.magnitude * 2f);
        Undo.RecordObject(scatter.rover, "Face rover toward the delta");
        scatter.rover.rotation = Quaternion.Euler(0, 270, 0);
        foreach (Camera camera in scatter.rover.GetComponentsInChildren<Camera>(true))
        {
            Undo.RecordObject(camera, "Show full Jezero terrain");
            camera.farClipPlane = Mathf.Max(camera.farClipPlane, 6000f);
        }
        if (terrain.terrainData != data || collider.terrainData != data ||
            terrain.materialTemplate != material || EditorJsonUtility.ToJson(material) != materialBefore ||
            WeightHash(data) != weightsBefore || data.terrainLayers.Length != layers.Length)
            throw new Exception("Terrain references or artistic surface unexpectedly changed.");
        for (int i = 0; i < layers.Length; i++)
            if (data.terrainLayers[i] != layers[i] || EditorJsonUtility.ToJson(layers[i]) != layerBefore[i])
                throw new Exception("Terrain layers unexpectedly changed.");
        if (scatter.burialFraction != minimumBurial || scatter.maximumBurialFraction != maximumBurial ||
            scatter.samples.count != candidatesBefore)
            throw new Exception("Existing rock tuning unexpectedly changed.");
        EditorUtility.SetDirty(generator);
        Physics.SyncTransforms();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        // Save only after the rock bake succeeds; the old field is stale at this stage.
        return new { sizeMeters = data.size.x, resolution = data.heightmapResolution,
            reliefMeters = source.maximumElevationMeters - source.minimumElevationMeters,
            roverPosition = scatter.rover.position.ToString(), surfaceUnchanged = true,
            terrainAndColliderReferencesPreserved = true, scatter.DecorationCount, scatter.ObstacleCount,
            minimumBurial, maximumBurial };
    }

    public static object Rocks()
    {
        var generator = UnityEngine.Object.FindAnyObjectByType<MarsTerrainGenerator>();
        if (Application.isPlaying || generator.manifestPath != "TerrainSources/Prepared/Jezero/manifest.json")
            throw new InvalidOperationException("Migrate Jezero terrain first in Edit Mode.");
        var scatter = UnityEngine.Object.FindAnyObjectByType<RockScatterSettings>();
        Undo.RecordObject(scatter, "Use Jezero rock layout seed");
        scatter.seed = 45994;
        var field = RockFieldBaker.Bake(scatter);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(generator.gameObject.scene);
        return new { field.decorativeCount, field.obstacleCount, field.sampleCount, field.layoutSha256 };
    }

    public static object SplitExistingLayout()
    {
        var scatter = UnityEngine.Object.FindAnyObjectByType<RockScatterSettings>();
        if (Application.isPlaying) throw new InvalidOperationException("Edit Mode required.");
        string path = AssetDatabase.GetAssetPath(scatter.layoutPrefab);
        if (new FileInfo(path).Length <= 80000000)
            return new { splitRequired = false };
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var field = root.GetComponent<BakedRockField>();
            RockFieldBaker.SaveLargeFieldParts(root, path,
                field.decorativeCount + field.obstacleCount + field.sampleCount);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scatter.gameObject.scene);
        return new { splitRequired = true };
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
