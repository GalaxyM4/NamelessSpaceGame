using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>One-time scene migration to density controls, keeping the existing target counts.</summary>
public static class EnableRockTuning
{
    public static object Run()
    {
        var settings = UnityEngine.Object.FindAnyObjectByType<RockScatterSettings>();
        if (Application.isPlaying || settings == null || settings.gameObject.scene.isDirty)
            throw new InvalidOperationException("Open the saved MarsScene in Edit Mode first.");
        string[] paths = {
            AssetDatabase.GetAssetPath(settings.terrain.terrainData),
            AssetDatabase.GetAssetPath(settings.terrain.materialTemplate)
        };
        string[] before = paths.Select(Hash).ToArray();
        Vector3 roverPosition = settings.rover.position;
        int decorations = settings.DecorationCount, obstacles = settings.ObstacleCount;
        Undo.RecordObject(settings, "Enable rock density tuning");
        settings.decorationDensity = decorations / settings.AreaHectares;
        settings.obstacleDensity = obstacles / settings.AreaHectares;
        settings.useDensity = true;
        settings.burialFraction = 0.08f;
        settings.maximumBurialFraction = 0.35f;
        var inspector = Editor.CreateEditor(settings);
        try
        {
            var ui = inspector.CreateInspectorGUI();
            if (ui.Q<Button>() == null || ui.Q("decorationDensity") == null ||
                ui.Q("maximumBurialFraction") == null)
                throw new Exception("Missing rock tuning Inspector controls.");
        }
        finally { UnityEngine.Object.DestroyImmediate(inspector); }
        var field = RockFieldBaker.Bake(settings);
        if (!paths.Select(Hash).SequenceEqual(before) || settings.rover.position != roverPosition)
            throw new Exception("Protected terrain or rover changed.");
        EditorSceneManager.SaveScene(settings.gameObject.scene);
        Selection.activeGameObject = settings.gameObject;
        return new { field.decorativeCount, field.obstacleCount, field.sampleCount,
            settings.decorationDensity, settings.obstacleDensity,
            settings.burialFraction, settings.maximumBurialFraction,
            terrainUnchanged = true, inspectorControlsPresent = true, field.layoutSha256 };
    }

    private static string Hash(string path)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));
    }
}
