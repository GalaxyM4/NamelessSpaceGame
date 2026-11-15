using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BakeCurrentScene
{
    public static object Run()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MarsScene.unity")
            throw new InvalidOperationException("Open MarsScene before running this bake.");
        if (scene.isDirty)
            throw new InvalidOperationException("Save or discard your scene edits before running the scripted bake.");
        MarsTerrainGenerator generator = UnityEngine.Object.FindAnyObjectByType<MarsTerrainGenerator>();
        RoverController rover = UnityEngine.Object.FindAnyObjectByType<RoverController>();
        if (generator == null || rover == null)
            throw new InvalidOperationException("The existing terrain generator and rover must be present.");
        const string imagePath = "Assets/Textures/HiRISE/GaleOrtho.png";
        AssetDatabase.ImportAsset(imagePath, ImportAssetOptions.ForceSynchronousImport);

        Undo.RecordObject(generator, "Configure HiRISE Bake");
        generator.manifestPath = "TerrainSources/Prepared/Gale/manifest.json";
        generator.terrainCenterWorldXZ = Vector2.zero;
        generator.flattenSpawn = true;
        generator.spawnWorldXZ = Vector2.zero;
        generator.spawnSafeRadius = 6f;
        generator.spawnBlendWidth = 12f;
        generator.snapTarget = rover.transform;
        generator.snapHeightOffset = 1.3f;
        generator.detailLayer = null;
        generator.detailWeight = 0f;
        EditorUtility.SetDirty(generator);
        MarsTerrainGeneratorEditor.BakeTerrain(generator);
        EditorSceneManager.SaveScene(scene);
        Terrain terrain = generator.GetComponent<Terrain>();
        return new
        {
            scene = scene.path, resolution = terrain.terrainData.heightmapResolution,
            size = terrain.terrainData.size.ToString(), rover = rover.transform.position.ToString(),
            terrainAsset = AssetDatabase.GetAssetPath(terrain.terrainData)
        };
    }
}
