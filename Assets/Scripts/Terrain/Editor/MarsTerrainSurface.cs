using System;
using UnityEditor;
using UnityEngine;

/// <summary>Repeatable desktop surface setup, shared by rebakes and surface-only updates.</summary>
public static class MarsTerrainSurface
{
    public const string MaterialPath = "Assets/Textures/HiRISE/MarsTerrainDetail.mat";
    public const string NormalPath = "Assets/Textures/Rock029_8K-JPG_NormalGL.jpg";

    public static void Apply(Terrain terrain, string imagePath)
    {
        Shader shader = Shader.Find("NamelessSpaceGame/Mars Terrain Detail");
        if (shader == null || ShaderUtil.ShaderHasError(shader))
            throw new InvalidOperationException("Mars terrain detail shader is missing or has compile errors.");
        TextureImporter image = AssetImporter.GetAtPath(imagePath) as TextureImporter;
        TextureImporter normal = AssetImporter.GetAtPath(NormalPath) as TextureImporter;
        if (image == null || normal == null)
            throw new InvalidOperationException("HiRISE imagery and the existing rock normal map are required.");
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
        Undo.RecordObject(terrain, "Apply Mars surface detail");
        terrain.materialTemplate = material;
        terrain.basemapDistance = Mathf.Max(terrain.basemapDistance, 150f);
        EditorUtility.SetDirty(material);
        EditorUtility.SetDirty(terrain);
    }

    private static void ConfigureTexture(TextureImporter importer, bool normal)
    {
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !normal;
        importer.maxTextureSize = normal ? 2048 : 4096;
        importer.wrapMode = normal ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 16;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        // Explicit desktop formats preserve imagery and normal quality without uncompressed 4K textures.
        var desktop = importer.GetPlatformTextureSettings("Standalone");
        desktop.overridden = true;
        desktop.maxTextureSize = normal ? 2048 : 4096;
        desktop.format = normal ? TextureImporterFormat.BC5 : TextureImporterFormat.BC7;
        desktop.compressionQuality = 100;
        importer.SetPlatformTextureSettings(desktop);
        importer.SaveAndReimport();
    }
}
