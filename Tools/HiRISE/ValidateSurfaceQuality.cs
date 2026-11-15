using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ValidateSurfaceQuality
{
    public static object Run()
    {
        Terrain terrain = UnityEngine.Object.FindAnyObjectByType<MarsTerrainGenerator>().GetComponent<Terrain>();
        Material material = terrain.materialTemplate;
        if (material == null || material.shader.name != "NamelessSpaceGame/Mars Terrain Detail" ||
            !material.shader.isSupported || ShaderUtil.ShaderHasError(material.shader))
            throw new Exception("Mars detail shader is unavailable, unsupported, or has errors.");
        if (terrain.terrainData.terrainLayers.Length != 1)
            throw new Exception("Expected the HiRISE color layer only, with no repeating color blend.");
        string imagePath = AssetDatabase.GetAssetPath(terrain.terrainData.terrainLayers[0].diffuseTexture);
        var image = (TextureImporter)AssetImporter.GetAtPath(imagePath);
        var normal = (TextureImporter)AssetImporter.GetAtPath(MarsTerrainSurface.NormalPath);
        foreach (var importer in new[] { image, normal })
            if (importer.filterMode != FilterMode.Trilinear || importer.anisoLevel != 16 || !importer.mipmapEnabled)
                throw new Exception("Expected trilinear filtering, mipmaps, and 16x anisotropy.");
        if (normal.textureType != TextureImporterType.NormalMap || normal.sRGBTexture ||
            normal.wrapMode != TextureWrapMode.Repeat || image.wrapMode != TextureWrapMode.Clamp)
            throw new Exception("Incorrect normal encoding or texture wrap mode.");
        var colorDesktop = image.GetPlatformTextureSettings("Standalone");
        var normalDesktop = normal.GetPlatformTextureSettings("Standalone");
        if (!colorDesktop.overridden || colorDesktop.format != TextureImporterFormat.BC7 ||
            !normalDesktop.overridden || normalDesktop.format != TextureImporterFormat.BC5)
            throw new Exception("Desktop BC7 imagery / BC5 normal overrides are missing.");
        var manifest = JsonUtility.FromJson<MarsTerrainGeneratorEditor.SourceManifest>(
            File.ReadAllText("TerrainSources/Prepared/Gale/manifest.json"));
        string imageHash;
        using (var sha = System.Security.Cryptography.SHA256.Create())
            imageHash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(imagePath)))
                .Replace("-", "").ToLowerInvariant();
        if (imageHash != manifest.textureSha256)
            throw new Exception("The HiRISE color imagery was changed by surface quality setup.");
        object result = new
        {
            passed = true, shader = material.shader.name, imageUnchanged = true,
            colorLayerCount = terrain.terrainData.terrainLayers.Length,
            filter = image.filterMode.ToString(), anisotropy = image.anisoLevel,
            colorFormat = colorDesktop.format.ToString(), normalFormat = normalDesktop.format.ToString(),
            detailTileMeters = material.GetFloat("_MarsDetailTileMeters"),
            detailStrength = material.GetFloat("_MarsDetailStrength"),
            fadeStartMeters = material.GetFloat("_MarsDetailFadeStart"),
            fadeEndMeters = material.GetFloat("_MarsDetailFadeEnd"),
            detailNormalSource = MarsTerrainSurface.NormalPath,
            note = "Normal detail is artistic lighting detail, not measured terrain or collider displacement."
        };
        File.WriteAllText("TerrainSources/Prepared/Gale/surface-quality.json",
            Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented) + "\n");
        return result;
    }
}
