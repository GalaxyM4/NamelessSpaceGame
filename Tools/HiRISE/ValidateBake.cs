using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ValidateBake
{
    public static object Run()
    {
        MarsTerrainGenerator generator = UnityEngine.Object.FindAnyObjectByType<MarsTerrainGenerator>();
        Terrain terrain = generator.GetComponent<Terrain>();
        TerrainData data = terrain.terrainData;
        string directory = Path.GetDirectoryName(generator.manifestPath);
        MarsTerrainGeneratorEditor.SourceManifest source =
            JsonUtility.FromJson<MarsTerrainGeneratorEditor.SourceManifest>(
                File.ReadAllText(generator.manifestPath));
        string bakeJson = File.ReadAllText(Path.Combine(directory, "bake-report.json"));
        // Read the recorded absolute-elevation offset without requiring the private report type.
        Newtonsoft.Json.Linq.JObject bake = Newtonsoft.Json.Linq.JObject.Parse(bakeJson);
        float offset = (float)bake["elevationOffsetMeters"];
        float maxError = 0f;
        int checkedSamples = 0;
        float[,] actual = data.GetHeights(0, 0, source.resolution, source.resolution);
        using (BinaryReader reader = new BinaryReader(File.OpenRead(Path.Combine(directory, source.heightFile))))
        {
            for (int z = 0; z < source.resolution; z++)
            for (int x = 0; x < source.resolution; x++)
            {
                float expected = reader.ReadSingle();
                Vector2 world = new Vector2(terrain.transform.position.x, terrain.transform.position.z) +
                    new Vector2(x, z) * (source.sizeMeters / (source.resolution - 1));
                bool edited = generator.flattenSpawn && Vector2.Distance(world, generator.spawnWorldXZ) <
                    generator.spawnSafeRadius + generator.spawnBlendWidth;
                foreach (var edit in generator.adjustments)
                    edited |= Vector2.Distance(world, edit.worldXZ) < edit.radiusMeters + edit.blendMeters;
                if (edited) continue;
                float restored = actual[z, x] * data.size.y + offset;
                maxError = Mathf.Max(maxError, Mathf.Abs(restored - expected));
                checkedSamples++;
            }
        }
        if (maxError > data.size.y / 65535f + 0.002f)
            throw new Exception($"Baked elevations or orientation differ from source: {maxError} m.");
        if (terrain.GetComponent<TerrainCollider>().terrainData != data)
            throw new Exception("TerrainCollider is not using the baked TerrainData.");
        if (data.terrainLayers.Length != 1 ||
            UnityEditor.AssetDatabase.GetAssetPath(data.terrainLayers[0].diffuseTexture) != MarsTerrainSurface.ColorPath ||
            data.terrainLayers[0].tileSize != Vector2.one * generator.surfaceTileMeters)
            throw new Exception("Expected a repeating artistic rock surface over the real elevations.");

        // Conservative connected driving-area screening at 4 m spacing, <=15 degrees.
        int n = (source.resolution - 1) / 4 + 1;
        bool[,] passable = new bool[n, n];
        int traversable = 0;
        for (int z = 1; z < n - 1; z++)
        for (int x = 1; x < n - 1; x++)
        {
            float dx = (actual[z * 4, x * 4 + 4] - actual[z * 4, x * 4 - 4]) * data.size.y / 8f;
            float dz = (actual[z * 4 + 4, x * 4] - actual[z * 4 - 4, x * 4]) * data.size.y / 8f;
            passable[z, x] = Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz)) * Mathf.Rad2Deg <= 15f;
            if (passable[z, x]) traversable++;
        }
        int sx = Mathf.RoundToInt((generator.spawnWorldXZ.x - terrain.transform.position.x) / 4f);
        int sz = Mathf.RoundToInt((generator.spawnWorldXZ.y - terrain.transform.position.z) / 4f);
        if (!passable[sz, sx]) throw new Exception("Spawn is not in the passable driving area.");
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(sx, sz));
        passable[sz, sx] = false;
        int connected = 0;
        float farthestMeters = 0f;
        Vector2Int[] steps = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();
            connected++;
            farthestMeters = Mathf.Max(farthestMeters, Vector2.Distance(cell, new Vector2(sx, sz)) * 4f);
            foreach (Vector2Int step in steps)
            {
                Vector2Int next = cell + step;
                if (next.x < 0 || next.y < 0 || next.x >= n || next.y >= n || !passable[next.y, next.x])
                    continue;
                passable[next.y, next.x] = false;
                queue.Enqueue(next);
            }
        }
        if (farthestMeters < 250f)
            throw new Exception("No connected 250 m driving route from spawn.");
        object result = new
        {
            checkedUnmodifiedSamples = checkedSamples, maximumElevationErrorMeters = maxError,
            connectedDrivingAreaFraction = connected / (float)(n * n),
            connectedPassableSamples = connected, passableSamples = traversable,
            farthestConnectedDistanceMeters = farthestMeters,
            slopeLimitDegrees = 15, routeScreeningSpacingMeters = 4,
            note = "Slope screening is not a substitute for rover physics testing."
        };
        File.WriteAllText(Path.Combine(directory, "validation.json"),
            Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented) + "\n");
        return result;
    }
}
