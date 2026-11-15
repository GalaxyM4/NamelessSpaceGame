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
            throw new Exception("Expected one artistic rock layer, with no satellite imagery blend.");
        string imagePath = AssetDatabase.GetAssetPath(terrain.terrainData.terrainLayers[0].diffuseTexture);
        var image = (TextureImporter)AssetImporter.GetAtPath(imagePath);
        var normal = (TextureImporter)AssetImporter.GetAtPath(MarsTerrainSurface.NormalPath);
        foreach (var importer in new[] { image, normal })
            if (importer.filterMode != FilterMode.Trilinear || importer.anisoLevel != 16 || !importer.mipmapEnabled)
                throw new Exception("Expected trilinear filtering, mipmaps, and 16x anisotropy.");
        if (normal.textureType != TextureImporterType.NormalMap || normal.sRGBTexture ||
            normal.wrapMode != TextureWrapMode.Repeat || image.wrapMode != TextureWrapMode.Repeat)
            throw new Exception("Incorrect normal encoding or texture wrap mode.");
        var colorDesktop = image.GetPlatformTextureSettings("Standalone");
        var normalDesktop = normal.GetPlatformTextureSettings("Standalone");
        if (!colorDesktop.overridden || colorDesktop.format != TextureImporterFormat.BC7 ||
            !normalDesktop.overridden || normalDesktop.format != TextureImporterFormat.BC5)
            throw new Exception("Desktop BC7 rock color / BC5 normal overrides are missing.");
        if (imagePath != MarsTerrainSurface.ColorPath ||
            AssetDatabase.GetAssetPath(material.GetTexture("_MarsDetailNormal")) != MarsTerrainSurface.NormalPath)
            throw new Exception("Expected the existing rock color and matching normal map, not HiRISE imagery.");
        var layer = terrain.terrainData.terrainLayers[0];
        var generator = terrain.GetComponent<MarsTerrainGenerator>();
        if (layer.tileSize != Vector2.one * generator.surfaceTileMeters ||
            layer.tileOffset != Vector2.zero || layer.normalMapTexture != null || layer.maskMapTexture != null)
            throw new Exception("Color and shader normal UV alignment is incorrect, or a second normal/mask is applied.");
        if (terrain.basemapDistance < terrain.terrainData.size.magnitude * 2f ||
            material.GetFloat("_MarsMacroMeters") < 1f ||
            material.GetFloat("_MarsMacroStrength") <= 0f)
            throw new Exception("Stochastic surface coverage or broad color variation is not configured.");
        object result = new
        {
            passed = true, shader = material.shader.name, satelliteImageryDisplayed = false,
            colorTextureSource = imagePath,
            colorLayerCount = terrain.terrainData.terrainLayers.Length,
            filter = image.filterMode.ToString(), anisotropy = image.anisoLevel,
            colorFormat = colorDesktop.format.ToString(), normalFormat = normalDesktop.format.ToString(),
            surfaceTileMeters = layer.tileSize.x,
            sampling = "three smoothly blended, spatially randomized offsets and rotations",
            macroPatchMeters = material.GetFloat("_MarsMacroMeters"),
            macroStrength = material.GetFloat("_MarsMacroStrength"),
            basemapDistanceMeters = terrain.basemapDistance,
            detailStrength = material.GetFloat("_MarsDetailStrength"),
            fadeStartMeters = material.GetFloat("_MarsDetailFadeStart"),
            fadeEndMeters = material.GetFloat("_MarsDetailFadeEnd"),
            detailNormalSource = MarsTerrainSurface.NormalPath,
            note = "HiRISE supplies elevation only. Stochastic rock sampling and broad color variation are artistic, not measured Mars composition."
        };
        File.WriteAllText("TerrainSources/Prepared/Gale/surface-quality.json",
            Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented) + "\n");
        return result;
    }
}
