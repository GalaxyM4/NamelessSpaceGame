// Editor-only importer; no GIS or source-raster dependencies enter player builds.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(MarsTerrainGenerator))]
public class MarsTerrainGeneratorEditor : Editor
{
    [Serializable]
    public class SourceManifest
    {
        public int schemaVersion;
        public string region, sourcePage, dtmProduct, orthoProduct, credit;
        public string heightFile, heightSha256, textureAssetPath, textureSha256;
        public string orientation, crsWkt;
        public int resolution;
        public float sizeMeters, minimumElevationMeters, maximumElevationMeters;
        public double[] projectedBounds;
    }

    [Serializable]
    private class BakeReport
    {
        public string dtmProduct, orthoProduct, sourceHeightSha256, sourceTextureSha256;
        public string orientation, credit;
        public double[] projectedBounds;
        public float elevationOffsetMeters, verticalRangeMeters, sizeMeters;
        public int resolution;
        public Vector3 terrainWorldPosition;
        public bool flattenSpawn;
        public Vector2 spawnWorldXZ;
        public string heightMode;
        public float verticalScale;
        public string surfaceMode, surfaceTextureAssetPath;
        public float surfaceTileMeters;
        public float maximumGameplayHeightChangeMeters;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Bakes real HiRISE elevation with the artistic Mars rock surface. Satellite imagery is not displayed. " +
            "Run the preparation tool first. Elevations are baked at real scale without smoothing, spawn flattening or gameplay height edits.",
            MessageType.Info);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("Bake HiRISE Mars Terrain", GUILayout.Height(30)))
            {
                try
                {
                    BakeTerrain((MarsTerrainGenerator)target);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, target);
                }
            }
        }
    }

    public static void BakeTerrain(MarsTerrainGenerator generator, bool applySurface = true)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Bake terrain in Edit Mode only.");
        Terrain terrain = generator.GetComponent<Terrain>();
        if (terrain.terrainData == null)
            throw new InvalidOperationException("TerrainData is required.");
        if (generator.transform.rotation != Quaternion.identity ||
            generator.transform.lossyScale != Vector3.one)
            throw new InvalidOperationException("Georeferenced terrain requires identity rotation and unit scale.");

        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string manifestPath = Path.GetFullPath(Path.Combine(root, generator.manifestPath));
        SourceManifest source = JsonUtility.FromJson<SourceManifest>(File.ReadAllText(manifestPath));
        ValidateManifest(source);
        string heightPath = Path.Combine(Path.GetDirectoryName(manifestPath), source.heightFile);
        byte[] bytes = File.ReadAllBytes(heightPath);
        int res = source.resolution;
        if (bytes.Length != checked(res * res * sizeof(float)) || Hash(bytes) != source.heightSha256)
            throw new InvalidDataException("Prepared heights are incomplete or differ from the manifest.");
        // Imagery is retained as provenance only; rebaking requires elevation, not orthoimages.
        if (applySurface) MarsTerrainSurface.Validate(terrain);
        if (!Finite(generator.terrainCenterWorldXZ.x) || !Finite(generator.terrainCenterWorldXZ.y) ||
            !Finite(generator.snapHeightOffset) || generator.snapHeightOffset < 0f)
            throw new InvalidDataException("Invalid terrain or rover placement settings.");

        float size = source.sizeMeters;
        Vector2 origin = generator.terrainCenterWorldXZ - Vector2.one * (size * 0.5f);
        ValidatePoint(generator.spawnWorldXZ, origin, size);
        if (generator.snapTarget != null)
            ValidatePoint(new Vector2(generator.snapTarget.position.x, generator.snapTarget.position.z), origin, size);

        float[,] heights = new float[res, res];
        float minimum = float.PositiveInfinity, maximum = float.NegativeInfinity;
        using (BinaryReader reader = new BinaryReader(new MemoryStream(bytes)))
        {
            for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
            {
                float height = reader.ReadSingle(); // Explicit little-endian IEEE float32.
                if (!Finite(height) || height < source.minimumElevationMeters - 0.01f ||
                    height > source.maximumElevationMeters + 0.01f)
                    throw new InvalidDataException("Invalid or missing elevation in prepared heights.");
                heights[z, x] = height;
                minimum = Mathf.Min(minimum, height);
                maximum = Mathf.Max(maximum, height);
            }
        }

        // Normalize for TerrainData storage only. The matching vertical range restores
        // source metres exactly (within Unity's height quantization), without exaggeration.
        float elevationOffset = minimum - 4f;
        float verticalRange = Mathf.Max(8f, maximum - minimum + 8f);
        for (int z = 0; z < res; z++)
        for (int x = 0; x < res; x++)
            heights[z, x] = (heights[z, x] - elevationOffset) / verticalRange;

        // All source validation finishes before mutating the existing scene or TerrainData.
        TerrainData data = terrain.terrainData;
        Undo.RegisterCompleteObjectUndo(data, "Bake HiRISE Terrain");
        Undo.RecordObject(generator.transform, "Position HiRISE Terrain");
        Undo.RecordObject(terrain, "Bake HiRISE Terrain");
        generator.transform.position = new Vector3(origin.x, generator.transform.position.y, origin.y);
        data.heightmapResolution = res;
        data.size = new Vector3(size, verticalRange, size);
        data.SetHeights(0, 0, heights);

        if (applySurface) MarsTerrainSurface.Apply(terrain);
        TerrainCollider collider = terrain.GetComponent<TerrainCollider>();
        if (collider != null)
        {
            Undo.RecordObject(collider, "Update HiRISE Collider");
            collider.terrainData = data;
        }
        terrain.Flush();
        if (generator.snapTarget != null)
        {
            Undo.RecordObject(generator.snapTarget, "Snap Rover To HiRISE Terrain");
            Vector3 position = generator.snapTarget.position;
            position.y = terrain.SampleHeight(position) + terrain.transform.position.y + generator.snapHeightOffset;
            generator.snapTarget.position = position;
        }
        EditorUtility.SetDirty(data);
        EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
        AssetDatabase.SaveAssets();
        BakeReport report = new BakeReport
        {
            dtmProduct = source.dtmProduct, orthoProduct = source.orthoProduct,
            sourceHeightSha256 = source.heightSha256, sourceTextureSha256 = source.textureSha256,
            orientation = source.orientation, credit = source.credit, projectedBounds = source.projectedBounds,
            elevationOffsetMeters = elevationOffset, verticalRangeMeters = verticalRange,
            sizeMeters = size, resolution = res, terrainWorldPosition = terrain.transform.position,
            flattenSpawn = false, spawnWorldXZ = generator.spawnWorldXZ,
            heightMode = "unmodified-source-elevations", verticalScale = 1f,
            surfaceMode = "artistic-rock", surfaceTextureAssetPath = MarsTerrainSurface.ColorPath,
            surfaceTileMeters = generator.surfaceTileMeters,
            maximumGameplayHeightChangeMeters = 0f
        };
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(manifestPath), "bake-report.json"),
            JsonUtility.ToJson(report, true) + "\n");
        Debug.Log($"Baked {source.region}: {size} m, {res}² heights, " +
            $"unmodified source elevations at 1:1 scale. {source.credit}", generator);
    }

    private static void ValidateManifest(SourceManifest source)
    {
        if (source == null || source.schemaVersion != 1 || source.resolution < 33 ||
            source.resolution > 4097 || !Mathf.IsPowerOfTwo(source.resolution - 1) ||
            !Finite(source.sizeMeters) || source.sizeMeters <= 0 ||
            !Finite(source.minimumElevationMeters) || !Finite(source.maximumElevationMeters) ||
            source.maximumElevationMeters < source.minimumElevationMeters ||
            source.orientation != "+X=east,+Z=north; float32 LE rows south-to-north" ||
            string.IsNullOrEmpty(source.heightFile) || Path.GetFileName(source.heightFile) != source.heightFile)
            throw new InvalidDataException("Unsupported or invalid HiRISE manifest.");
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private static void ValidatePoint(Vector2 point, Vector2 origin, float size)
    {
        if (!Finite(point.x) || !Finite(point.y) || point.x < origin.x || point.y < origin.y ||
            point.x > origin.x + size || point.y > origin.y + size)
            throw new InvalidDataException("Spawn and rover must be inside the terrain.");
    }

    private static string Hash(byte[] bytes)
    {
        using (SHA256 sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }
}
#endif
