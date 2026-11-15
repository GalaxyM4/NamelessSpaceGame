using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class MarsTerrainGenerator : MonoBehaviour
{
    private struct CraterData
    {
        public Vector2 Center;
        public float Radius;
        public float Depth;
        public float RimHeight;
    }

    [Header("Seed & Base Elevation")]
    public int seed = 2026;
    [Tooltip("Base plain elevation in meters, leaving headroom below for crater bowls.")]
    public float baseElevationMeters = 15f;
    public float maxReliefMeters = 28f;

    [Header("Domain-Warped fBm (Plains)")]
    [Range(1, 8)] public int octaves = 5;
    public float baseFrequency = 0.0025f;
    public float lacunarity = 2.05f;
    [Range(0.1f, 0.9f)] public float persistence = 0.45f;
    public float warpStrength = 45f;
    public float warpFrequency = 0.0018f;

    [Header("Ridged Multifractal (Escarpments)")]
    public float ridgeFrequency = 0.0032f;
    [Range(0f, 1.5f)] public float ridgeWeight = 0.45f;
    [Range(1f, 4f)] public float ridgeSharpness = 2.0f;

    [Header("Wind Dunes & Micro-Relief")]
    public float duneFrequency = 0.045f;
    public float duneAmplitudeMeters = 0.85f;
    public float duneDirectionDegrees = 32f;
    public float microNoiseFrequency = 0.18f;
    public float microNoiseAmplitudeMeters = 0.18f;

    [Header("Procedural Impact Craters")]
    [Min(0)] public int craterCount = 24;
    public Vector2 craterRadiusRange = new Vector2(14f, 65f);
    [Range(0.05f, 0.35f)] public float craterDepthRatio = 0.16f;
    [Range(0.02f, 0.15f)] public float craterRimHeightRatio = 0.055f;

    [Header("Rover Spawn Plateau")]
    public Vector2 spawnWorldXZ = Vector2.zero;
    public float spawnSafeRadius = 22f;
    public float spawnBlendWidth = 28f;
    public Transform snapTarget;
    public float snapHeightOffset = 1.3f;

    [ContextMenu("Generate Mars Terrain")]
    public void GenerateTerrain()
    {
        Terrain terrain = GetComponent<Terrain>();
        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogError("MarsTerrainGenerator requires a Terrain with valid TerrainData.", this);
            return;
        }

        TerrainData data = terrain.terrainData;
        int res = data.heightmapResolution;
        Vector3 terrainSize = data.size;
        Vector3 terrainPos = transform.position;

        System.Random rng = new System.Random(seed);
        Vector2[] octaveOffsets = CreateOffsets(rng, octaves);
        Vector2 warpOffsetX = new Vector2((float)rng.NextDouble() * 10000f, (float)rng.NextDouble() * 10000f);
        Vector2 warpOffsetZ = new Vector2((float)rng.NextDouble() * 10000f, (float)rng.NextDouble() * 10000f);
        Vector2 ridgeOffset = new Vector2((float)rng.NextDouble() * 10000f, (float)rng.NextDouble() * 10000f);
        List<CraterData> craters = GenerateCraters(rng, terrainPos, terrainSize);

        float duneRad = duneDirectionDegrees * Mathf.Deg2Rad;
        Vector2 duneDir = new Vector2(Mathf.Cos(duneRad), Mathf.Sin(duneRad));

        float[,] heights = new float[res, res];
        float invMaxHeight = 1f / Mathf.Max(0.001f, terrainSize.y);

        for (int z = 0; z < res; z++)
        {
            float normZ = z / (float)(res - 1);
            float worldZ = terrainPos.z + normZ * terrainSize.z;

            for (int x = 0; x < res; x++)
            {
                float normX = x / (float)(res - 1);
                float worldX = terrainPos.x + normX * terrainSize.x;

                float heightMeters = SampleMarsHeightMeters(
                    worldX, worldZ,
                    octaveOffsets, warpOffsetX, warpOffsetZ, ridgeOffset,
                    duneDir, craters);

                heights[z, x] = Mathf.Clamp01(heightMeters * invMaxHeight);
            }
        }

        data.SetHeights(0, 0, heights);

        if (snapTarget != null)
        {
            Vector3 pos = snapTarget.position;
            float groundY = terrain.SampleHeight(pos) + terrainPos.y;
            snapTarget.position = new Vector3(pos.x, groundY + snapHeightOffset, pos.z);
        }
    }

    private float SampleMarsHeightMeters(
        float worldX,
        float worldZ,
        Vector2[] octaveOffsets,
        Vector2 warpOffsetX,
        Vector2 warpOffsetZ,
        Vector2 ridgeOffset,
        Vector2 duneDir,
        List<CraterData> craters)
    {
        float warpX = (Mathf.PerlinNoise(worldX * warpFrequency + warpOffsetX.x, worldZ * warpFrequency + warpOffsetX.y) - 0.5f) * 2f * warpStrength;
        float warpZ = (Mathf.PerlinNoise(worldX * warpFrequency + warpOffsetZ.x, worldZ * warpFrequency + warpOffsetZ.y) - 0.5f) * 2f * warpStrength;

        float wx = worldX + warpX;
        float wz = worldZ + warpZ;

        float fbm = SampleFbm(wx, wz, octaveOffsets);
        float ridge = SampleRidged(wx, wz, ridgeOffset);
        float baseSurface = baseElevationMeters + (fbm * 0.65f + ridge * ridgeWeight) * maxReliefMeters;

        float dunePhase = (wx * duneDir.x + wz * duneDir.y) * duneFrequency;
        float duneMod = Mathf.PerlinNoise(wx * 0.006f + 13.7f, wz * 0.006f + 29.3f);
        float asymmetricCrest = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(dunePhase), 1.6f) - 0.35f;
        float dunes = asymmetricCrest * duneAmplitudeMeters * duneMod * (1f - Mathf.Clamp01(ridge));
        float microNoise = (Mathf.PerlinNoise(worldX * microNoiseFrequency + 41.2f, worldZ * microNoiseFrequency + 87.9f) - 0.5f) * microNoiseAmplitudeMeters;

        float craterDelta = 0f;
        for (int i = 0; i < craters.Count; i++)
        {
            CraterData c = craters[i];
            float dx = worldX - c.Center.x;
            float dz = worldZ - c.Center.y;
            float maxDist = c.Radius * 1.35f;
            if (dx * dx + dz * dz > maxDist * maxDist) continue;

            float r = Mathf.Sqrt(dx * dx + dz * dz) / c.Radius;
            craterDelta += EvaluateCraterProfile(r, c.Depth, c.RimHeight);
        }

        float combined = baseSurface + dunes + microNoise + craterDelta;

        float spawnDist = Vector2.Distance(new Vector2(worldX, worldZ), spawnWorldXZ);
        if (spawnDist < spawnSafeRadius + spawnBlendWidth)
        {
            float blend = Mathf.SmoothStep(0f, 1f, (spawnDist - spawnSafeRadius) / Mathf.Max(0.01f, spawnBlendWidth));
            combined = Mathf.Lerp(baseElevationMeters, combined, blend);
        }

        return combined;
    }

    private float SampleFbm(float x, float z, Vector2[] offsets)
    {
        float amplitude = 1f;
        float frequency = baseFrequency;
        float total = 0f;
        float maxAmplitude = 0f;

        for (int i = 0; i < offsets.Length; i++)
        {
            float n = Mathf.PerlinNoise(x * frequency + offsets[i].x, z * frequency + offsets[i].y);
            total += (n - 0.5f) * amplitude;
            maxAmplitude += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return (total / Mathf.Max(0.001f, maxAmplitude)) + 0.5f;
    }

    private float SampleRidged(float x, float z, Vector2 offset)
    {
        float n = Mathf.PerlinNoise(x * ridgeFrequency + offset.x, z * ridgeFrequency + offset.y);
        float ridge = 1f - Mathf.Abs(n * 2f - 1f);
        return Mathf.Pow(ridge, ridgeSharpness);
    }

    private static float EvaluateCraterProfile(float r, float depth, float rimHeight)
    {
        if (r >= 1.35f) return 0f;

        if (r < 1f)
        {
            float t = r * r;
            return Mathf.Lerp(-depth, rimHeight, t * t);
        }

        float rimT = (r - 1f) / 0.35f;
        return rimHeight * (1f - Mathf.SmoothStep(0f, 1f, rimT));
    }

    private List<CraterData> GenerateCraters(System.Random rng, Vector3 terrainPos, Vector3 terrainSize)
    {
        List<CraterData> list = new List<CraterData>(craterCount);
        float margin = craterRadiusRange.y;

        for (int i = 0; i < craterCount; i++)
        {
            float radius = Mathf.Lerp(craterRadiusRange.x, craterRadiusRange.y, (float)rng.NextDouble());
            float cx = terrainPos.x + margin + (float)rng.NextDouble() * Mathf.Max(1f, terrainSize.x - margin * 2f);
            float cz = terrainPos.z + margin + (float)rng.NextDouble() * Mathf.Max(1f, terrainSize.z - margin * 2f);
            Vector2 center = new Vector2(cx, cz);

            if (Vector2.Distance(center, spawnWorldXZ) < spawnSafeRadius + spawnBlendWidth + radius)
            {
                continue;
            }

            list.Add(new CraterData
            {
                Center = center,
                Radius = radius,
                Depth = radius * craterDepthRatio,
                RimHeight = radius * craterRimHeightRatio
            });
        }

        return list;
    }

    private static Vector2[] CreateOffsets(System.Random rng, int count)
    {
        Vector2[] offsets = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            offsets[i] = new Vector2((float)rng.NextDouble() * 10000f, (float)rng.NextDouble() * 10000f);
        }
        return offsets;
    }
}
