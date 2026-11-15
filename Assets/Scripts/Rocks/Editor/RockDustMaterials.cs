using System;
using UnityEditor;
using UnityEngine;

/// <summary>Appearance only. Edits the existing shared materials, never the field or scene.</summary>
public static class RockDustMaterials
{
    public static void Configure(Material material, Mesh mesh)
    {
        Shader shader = Shader.Find("Mars/Rock Dust");
        if (shader == null || ShaderUtil.ShaderHasError(shader))
            throw new InvalidOperationException("Mars/Rock Dust shader must import without errors first.");
        bool firstApplication = material.shader != shader;
        material.shader = shader;
        material.enableInstancing = true;
        material.SetTexture("_DustMap", AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Textures/Rock029_8K-JPG_Color.jpg"));
        material.SetFloat("_RockBottom", mesh.bounds.min.y);
        material.SetFloat("_RockHeight", mesh.bounds.size.y);
        if (firstApplication)
        {
            material.SetColor("_DustTint", new Color(0.62f, 0.47f, 0.34f));
            material.SetFloat("_DustCoverage", 0.72f);
            material.SetFloat("_DustVeil", 0.12f);
            material.SetFloat("_DustTopStrength", 0.85f);
            material.SetFloat("_DustBaseStrength", 0.95f);
            material.SetFloat("_DustBaseHeight", 0.5f);
            material.SetFloat("_DustNormalRetention", 0.3f);
            material.SetFloat("_DustSmoothness", 0.02f);
            material.SetFloat("_DustTextureStrength", 0.65f);
            material.SetFloat("_DustVariation", 0.22f);
            material.SetFloat("_DustMeters", 0.8f);
            material.SetFloat("_DustPatchMeters", 0.32f);
        }
        EditorUtility.SetDirty(material);
    }

    [MenuItem("Tools/Mars Rocks/Apply Dust To Shared Materials")]
    public static void Apply()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:RockDefinition", new[] { "Assets/Models/Rocks/Prepared" }))
        {
            var definition = AssetDatabase.LoadAssetAtPath<RockDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            Undo.RecordObject(definition.material, "Apply Mars rock dust");
            Configure(definition.material, definition.mesh);
            AssetDatabase.SaveAssetIfDirty(definition.material);
        }
    }
}
