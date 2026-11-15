using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

/// <summary>Apply/verify the appearance change and capture an identical-camera comparison in Edit Mode.</summary>
public static class ApplyRockDust
{
    public static object Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Edit Mode required.");
        var settings = UnityEngine.Object.FindAnyObjectByType<RockScatterSettings>();
        var terrain = settings.terrain;
        string[] protectedPaths = {
            settings.gameObject.scene.path,
            AssetDatabase.GetAssetPath(terrain.terrainData),
            AssetDatabase.GetAssetPath(terrain.materialTemplate),
            AssetDatabase.GetAssetPath(settings.layoutPrefab)
        };
        string[] hashes = protectedPaths.Select(Hash).ToArray();
        string surface = EditorJsonUtility.ToJson(terrain.materialTemplate);
        var definitions = settings.definitions;
        var colors = definitions.Select(d => d.material.GetTexture("_BaseMap")).ToArray();
        var normals = definitions.Select(d => d.material.GetTexture("_BumpMap")).ToArray();
        RockDustMaterials.Apply();
        var cameraObject = new GameObject("Temporary dust comparison camera") { hideFlags = HideFlags.HideAndDontSave };
        var camera = cameraObject.AddComponent<Camera>();
        var source = settings.rover.GetComponentInChildren<Camera>();
        camera.CopyFrom(source);
        camera.enabled = false;
        camera.fieldOfView = 60;
        camera.aspect = 16f / 9f;
        var target = settings.bakedRoot.GetComponentsInChildren<MeshRenderer>()
            .Where(r => r.GetComponent<Collider>() != null && r.bounds.size.magnitude > 1.1f)
            .OrderBy(r => (r.transform.position - settings.rover.position).sqrMagnitude).First();
        Vector3 center = target.bounds.center;
        Vector3 position = center + new Vector3(2.6f, 0, -3.1f);
        position.y = terrain.SampleHeight(position) + terrain.transform.position.y + 1.4f;
        camera.transform.position = position;
        camera.transform.LookAt(center);
        var rt = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active;
        float[] coverage = definitions.Select(d => d.material.GetFloat("_DustCoverage")).ToArray();
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Temp/opencode");
        try
        {
            camera.targetTexture = rt;
            for (int i = 0; i < definitions.Length; i++) definitions[i].material.SetFloat("_DustCoverage", 0);
            Capture(camera, rt, Path.Combine(folder, "rock-dust-before.png"));
            for (int i = 0; i < definitions.Length; i++) definitions[i].material.SetFloat("_DustCoverage", coverage[i]);
            Capture(camera, rt, Path.Combine(folder, "rock-dust-after.png"));
        }
        finally
        {
            for (int i = 0; i < definitions.Length; i++)
            {
                definitions[i].material.SetFloat("_DustCoverage", coverage[i]);
                AssetDatabase.SaveAssetIfDirty(definitions[i].material);
            }
            camera.targetTexture = null;
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
        for (int i = 0; i < definitions.Length; i++)
            if (definitions[i].material.GetTexture("_BaseMap") != colors[i] ||
                definitions[i].material.GetTexture("_BumpMap") != normals[i])
                throw new Exception("Original rock texture changed.");
        if (!protectedPaths.Select(Hash).SequenceEqual(hashes) ||
            surface != EditorJsonUtility.ToJson(terrain.materialTemplate))
            throw new Exception("Protected scene, field or terrain changed.");
        var shader = Shader.Find("Mars/Rock Dust");
        var messages = ShaderUtil.GetShaderMessages(shader);
        if (ShaderUtil.ShaderHasError(shader))
            throw new Exception(string.Join("\n", messages.Select(m => m.message)));
        return new { materials = definitions.Length, shaderSupported = shader.isSupported,
            protectedAssetsUnchanged = true, originalTexturesPreserved = true,
            comparisonBefore = Path.Combine(folder, "rock-dust-before.png"),
            comparisonAfter = Path.Combine(folder, "rock-dust-after.png"),
            shaderMessages = messages.Select(m => m.message).ToArray() };
    }

    private static void Capture(Camera camera, RenderTexture rt, string path)
    {
        camera.Render();
        RenderTexture.active = rt;
        var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        try
        {
            image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally { UnityEngine.Object.DestroyImmediate(image); }
    }

    private static string Hash(string path)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));
    }
}
