using System;
using System.IO;
using UnityEngine;

/// <summary>Temporary cameras only; no scene objects or assets are saved.</summary>
public static class CaptureTerrain
{
    public static object Run()
    {
        var settings = UnityEngine.Object.FindAnyObjectByType<RockScatterSettings>();
        var source = settings.rover.GetComponentInChildren<Camera>();
        var obj = new GameObject("Temporary terrain verification camera")
            { hideFlags = HideFlags.HideAndDontSave };
        var camera = obj.AddComponent<Camera>();
        camera.CopyFrom(source);
        camera.enabled = false;
        camera.farClipPlane = 10000;
        camera.fieldOfView = 60;
        camera.aspect = 16f / 9f;
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Temp/opencode");
        var rt = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = rt;
            camera.transform.position = new Vector3(650, 450, -1100);
            camera.transform.LookAt(new Vector3(-1000, 100, 600));
            Capture(camera, rt, Path.Combine(folder, "jezero-overview.png"));
            camera.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            Capture(camera, rt, Path.Combine(folder, "jezero-spawn.png"));
            Vector3 approach = new Vector3(-350, 0, 400);
            approach.y = settings.terrain.SampleHeight(approach) + settings.terrain.transform.position.y + 2;
            camera.transform.position = approach;
            camera.transform.LookAt(new Vector3(-1100, 115, 450));
            Capture(camera, rt, Path.Combine(folder, "jezero-delta-approach.png"));
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(obj);
        }
        return new { overview = Path.Combine(folder, "jezero-overview.png"),
            spawn = Path.Combine(folder, "jezero-spawn.png"),
            approach = Path.Combine(folder, "jezero-delta-approach.png") };
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
}
