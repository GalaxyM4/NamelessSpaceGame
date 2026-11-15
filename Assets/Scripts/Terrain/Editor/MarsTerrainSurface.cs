using System;
using UnityEditor;
using UnityEngine;

/// <summary>Repeatable desktop surface setup, shared by rebakes and surface-only updates.</summary>
public static class MarsTerrainSurface
{
    public const string MaterialPath = "Assets/Textures/HiRISE/MarsTerrainDetail.mat";
    public const string LayerPath = "Assets/Textures/HiRISE/MarsRock.terrainlayer";
    public const string ColorPath = "Assets/Textures/Rock029_8K-JPG_Color.jpg";
    public const string NormalPath = "Assets/Textures/Rock029_8K-JPG_NormalGL.jpg";

    public static void Validate(Terrain terrain)
    {
        Shader shader = Shader.Find("NamelessSpaceGame/Mars Terrain Detail");
        if (shader == null || ShaderUtil.ShaderHasError(shader))
            throw new InvalidOperationException("Mars terrain detail shader is missing or has compile errors.");
        var generator = terrain.GetComponent<MarsTerrainGenerator>();
        if (terrain.terrainData == null || generator == null ||
            float.IsNaN(generator.surfaceTileMeters) || float.IsInfinity(generator.surfaceTileMeters) ||
            generator.surfaceTileMeters < 0.1f)
            throw new InvalidOperationException("TerrainData and a finite surface tile size of at least 0.1 m are required.");
        TextureImporter image = AssetImporter.GetAtPath(ColorPath) as TextureImporter;
        TextureImporter normal = AssetImporter.GetAtPath(NormalPath) as TextureImporter;
        if (image == null || normal == null)
            throw new InvalidOperationException("The existing rock color texture and normal map are required.");
    }

    public static void Apply(Terrain terrain)
    {
        Validate(terrain);
        Shader shader = Shader.Find("NamelessSpaceGame/Mars Terrain Detail");
        var image = (TextureImporter)AssetImporter.GetAtPath(ColorPath);
        var normal = (TextureImporter)AssetImporter.GetAtPath(NormalPath);
        ConfigureTexture(image, false);
        ConfigureTexture(normal, true);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else
        {
            Undo.RecordObject(material, "Configure Mars surface detail");
            material.shader = shader;
        }
        material.SetTexture("_MarsDetailNormal", AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath));
        // Shader defaults initialize new materials; preserve any subsequent artist tuning.
        TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerPath);
        if (layer == null)
        {
            layer = new TerrainLayer();
            AssetDatabase.CreateAsset(layer, LayerPath);
        }
        else Undo.RecordObject(layer, "Configure artistic Mars surface");
        layer.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ColorPath);
        // The shader samples the normal separately for distance fading, using these same UVs.
        layer.normalMapTexture = null;
        layer.maskMapTexture = null;
        layer.tileSize = Vector2.one * terrain.GetComponent<MarsTerrainGenerator>().surfaceTileMeters;
        layer.tileOffset = Vector2.zero;
        layer.metallic = 0f;
        layer.smoothness = 0f;
        layer.smoothnessSource = TerrainLayerSmoothnessSource.ConstantOnly;
        TerrainData data = terrain.terrainData;
        Undo.RegisterCompleteObjectUndo(data, "Replace satellite imagery with Mars rock surface");
        data.terrainLayers = new[] { layer };
        int resolution = data.alphamapResolution;
        float[,,] weights = new float[resolution, resolution, 1];
        for (int z = 0; z < resolution; z++)
        for (int x = 0; x < resolution; x++)
            weights[z, x, 0] = 1f;
        data.SetAlphamaps(0, 0, weights);
        Undo.RecordObject(terrain, "Apply Mars surface detail");
        terrain.materialTemplate = material;
        // The stock basemap generator cannot bake stochastic sampling or world-space color.
        // Keep this shader over the whole map instead of reverting to repeating distant color.
        terrain.basemapDistance = Mathf.Max(terrain.basemapDistance, data.size.magnitude * 2f);
        EditorUtility.SetDirty(material);
        EditorUtility.SetDirty(layer);
        EditorUtility.SetDirty(data);
        EditorUtility.SetDirty(terrain);
        terrain.Flush();
    }

    private static void ConfigureTexture(TextureImporter importer, bool normal)
    {
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !normal;
        importer.maxTextureSize = normal ? 2048 : 4096;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 16;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        // Explicit desktop formats preserve color and normal quality without uncompressed 4K textures.
        var desktop = importer.GetPlatformTextureSettings("Standalone");
        desktop.overridden = true;
        desktop.maxTextureSize = normal ? 2048 : 4096;
        desktop.format = normal ? TextureImporterFormat.BC5 : TextureImporterFormat.BC7;
        desktop.compressionQuality = 100;
        importer.SetPlatformTextureSettings(desktop);
        importer.SaveAndReimport();
    }
}
