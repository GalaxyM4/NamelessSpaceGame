using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Prepare Blender exports using Unity importers, then configure the existing saved MarsScene.</summary>
public static class SetupRockField
{
    private const string Folder = "Assets/Models/Rocks";
    private static readonly string[] Names = { "BroomPoint", "CheyavaFalls", "Rochette", "Seitah", "Singita" };

    public static object Run()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/Scenes/MarsScene.unity" || scene.isDirty)
            throw new InvalidOperationException("Open the saved MarsScene in Edit Mode. No other scene is changed.");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        if (!AssetDatabase.IsValidFolder(Folder + "/Prepared")) AssetDatabase.CreateFolder(Folder, "Prepared");
        var definitions = new RockDefinition[Names.Length];
        for (int i = 0; i < Names.Length; i++)
        {
            string name = Names[i];
            string renderPath = $"{Folder}/SM_ROCK_{name}.fbx";
            string collisionPath = $"{Folder}/COL_ROCK_{name}_Convex.fbx";
            foreach (string modelPath in new[] { renderPath, collisionPath })
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.isReadable = true;
                importer.importAnimation = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.SaveAndReimport();
            }
            var extracted = Extract(renderPath, name + "_Render");
            Mesh render = extracted.mesh;
            Mesh collision = Extract(collisionPath, name + "_Collision", extracted.unitScale).mesh;
            Texture2D color = Texture($"{Folder}/T_ROCK_{name}_BaseColor.png", false);
            Texture2D normal = Texture($"{Folder}/T_ROCK_{name}_Normal.png", true);
            string materialPath = $"{Folder}/Prepared/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.SetTexture("_BaseMap", color);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", 0.65f);
            material.SetFloat("_Smoothness", 0.12f);
            material.SetFloat("_Metallic", 0f);
            material.EnableKeyword("_NORMALMAP");
            material.enableInstancing = true;
            RockDustMaterials.Configure(material, render);
            EditorUtility.SetDirty(material);
            string definitionPath = $"{Folder}/Prepared/{name}.asset";
            RockDefinition definition = AssetDatabase.LoadAssetAtPath<RockDefinition>(definitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<RockDefinition>();
                definition.displayName = name;
                definition.preferredField = 0.3f + i * 0.1f;
                AssetDatabase.CreateAsset(definition, definitionPath);
            }
            definition.mesh = render;
            definition.collisionMesh = collision;
            definition.material = material;
            EditorUtility.SetDirty(definition);
            definitions[i] = definition;
        }
        var generator = UnityEngine.Object.FindAnyObjectByType<MarsTerrainGenerator>();
        RoverController rover = UnityEngine.Object.FindAnyObjectByType<RoverController>();
        if (generator == null || rover == null) throw new InvalidOperationException("Existing terrain and rover required.");
        RockScatterSettings settings = UnityEngine.Object.FindAnyObjectByType<RockScatterSettings>();
        if (settings == null)
        {
            var obj = new GameObject("Mars Rock Placement");
            SceneManager.MoveGameObjectToScene(obj, scene);
            Undo.RegisterCreatedObjectUndo(obj, "Create rock placement settings");
            settings = Undo.AddComponent<RockScatterSettings>(obj);
        }
        Undo.RecordObject(settings, "Configure Mars rock assets");
        settings.terrain = generator.GetComponent<Terrain>();
        settings.rover = rover.transform;
        settings.definitions = definitions;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = settings.gameObject;
        return new { preparedModels = Names, sourceBlendUnchanged = true, configured = settings.name,
            next = "Run BakeRockField.Run or Tools > Mars Rocks > Bake Selected Field" };
    }

    private static Texture2D Texture(string path, bool normal)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Export packed Blender textures first.", path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !normal;
        importer.maxTextureSize = 1024;
        importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 4;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static (Mesh mesh, float unitScale) Extract(string modelPath, string name, float unitScale = 0f)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        MeshFilter filter = model.GetComponentInChildren<MeshFilter>();
        if (filter == null) throw new InvalidOperationException("No mesh in " + modelPath);
        Mesh source = filter.sharedMesh;
        Mesh mesh = UnityEngine.Object.Instantiate(source);
        mesh.name = name;
        Matrix4x4 matrix = model.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
        mesh.vertices = source.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
        mesh.normals = source.normals.Select(n => matrix.inverse.transpose.MultiplyVector(n).normalized).ToArray();
        mesh.RecalculateBounds();
        if (unitScale == 0f) unitScale = 1f / Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z);
        mesh.vertices = mesh.vertices.Select(v => v * unitScale).ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        string path = $"{Folder}/Prepared/{name}.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return (mesh, unitScale); }
        EditorUtility.CopySerialized(mesh, existing);
        UnityEngine.Object.DestroyImmediate(mesh);
        EditorUtility.SetDirty(existing);
        return (existing, unitScale);
    }
}
