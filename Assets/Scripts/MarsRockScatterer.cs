using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class MarsRockScatterer : MonoBehaviour
{
    private const string ContainerName = "MarsRocks";

    [Header("Scatter Settings")]
    public int seed = 4042;
    [Min(0)] public int rockCount = 450;
    [Range(0f, 50f)] public float marginMeters = 12f;

    [Header("Size & Slope Bias")]
    public Vector2 plainScaleRange = new Vector2(0.28f, 0.95f);
    public Vector2 slopeBoulderScaleRange = new Vector2(0.95f, 2.8f);
    [Range(1f, 45f)] public float steepSlopeDegrees = 8f;
    [Range(0f, 1f)] public float slopeNormalAlign = 0.35f;
    [Range(0f, 0.5f)] public float embedDepthRatio = 0.25f;

    [Header("Collision & Spawn Safety")]
    public float colliderMinScale = 0.65f;
    public Vector2 spawnWorldXZ = Vector2.zero;
    public float spawnSafeRadius = 24f;

    [Header("Procedural Rock Assets")]
    public Mesh[] rockMeshes;
    public Material rockMaterial;

    [ContextMenu("Clear Mars Rocks")]
    public void ClearRocks()
    {
        Transform existing = transform.Find(ContainerName);
        if (existing != null)
        {
            if (Application.isPlaying)
            {
                Destroy(existing.gameObject);
            }
            else
            {
                DestroyImmediate(existing.gameObject);
            }
        }
    }

    [ContextMenu("Scatter Mars Rocks")]
    public void ScatterRocks()
    {
        Terrain terrain = GetComponent<Terrain>();
        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogError("MarsRockScatterer requires a Terrain with valid TerrainData.", this);
            return;
        }

        EnsureFallbackAssets();
        if (rockMeshes == null || rockMeshes.Length == 0)
        {
            Debug.LogError("No rock meshes available to scatter.", this);
            return;
        }

        ClearRocks();

        GameObject container = new GameObject(ContainerName);
        container.transform.SetParent(transform, false);

        TerrainData data = terrain.terrainData;
        Vector3 terrainPos = transform.position;
        Vector3 terrainSize = data.size;
        System.Random rng = new System.Random(seed);

        int placed = 0;
        int maxAttempts = rockCount * 3;

        for (int attempt = 0; attempt < maxAttempts && placed < rockCount; attempt++)
        {
            float normX = Mathf.Lerp(marginMeters / terrainSize.x, 1f - marginMeters / terrainSize.x, (float)rng.NextDouble());
            float normZ = Mathf.Lerp(marginMeters / terrainSize.z, 1f - marginMeters / terrainSize.z, (float)rng.NextDouble());

            float worldX = terrainPos.x + normX * terrainSize.x;
            float worldZ = terrainPos.z + normZ * terrainSize.z;

            if (Vector2.Distance(new Vector2(worldX, worldZ), spawnWorldXZ) < spawnSafeRadius)
            {
                continue;
            }

            Vector3 normal = data.GetInterpolatedNormal(normX, normZ);
            float slope = Vector3.Angle(Vector3.up, normal);
            bool isSteep = slope >= steepSlopeDegrees;

            // Bias toward denser rock clusters on slopes/crater rims
            if (!isSteep && rng.NextDouble() > 0.55)
            {
                continue;
            }

            Vector2 scaleRange = isSteep ? slopeBoulderScaleRange : plainScaleRange;
            float baseScale = Mathf.Lerp(scaleRange.x, scaleRange.y, Mathf.Pow((float)rng.NextDouble(), 1.5f));
            Vector3 scale = new Vector3(
                baseScale * Mathf.Lerp(0.85f, 1.2f, (float)rng.NextDouble()),
                baseScale * Mathf.Lerp(0.8f, 1.15f, (float)rng.NextDouble()),
                baseScale * Mathf.Lerp(0.85f, 1.2f, (float)rng.NextDouble()));

            float groundY = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + terrainPos.y;
            float sinkY = scale.y * embedDepthRatio;

            Quaternion yaw = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            Quaternion tilt = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(Vector3.up, normal), slopeNormalAlign);

            Mesh chosenMesh = rockMeshes[rng.Next(rockMeshes.Length)];
            GameObject rock = new GameObject($"MarsRock_{placed:D3}");
            rock.transform.SetParent(container.transform, false);
            rock.transform.position = new Vector3(worldX, groundY - sinkY, worldZ);
            rock.transform.rotation = tilt * yaw;
            rock.transform.localScale = scale;
            rock.isStatic = true;

            MeshFilter mf = rock.AddComponent<MeshFilter>();
            mf.sharedMesh = chosenMesh;

            MeshRenderer mr = rock.AddComponent<MeshRenderer>();
            mr.sharedMaterial = rockMaterial;

            if (baseScale >= colliderMinScale)
            {
                MeshCollider mc = rock.AddComponent<MeshCollider>();
                mc.sharedMesh = chosenMesh;
                mc.convex = true;
            }

            placed++;
        }
    }

    private void EnsureFallbackAssets()
    {
        if (rockMeshes == null || rockMeshes.Length == 0)
        {
            List<Mesh> generated = new List<Mesh>(4);
            for (int i = 0; i < 4; i++)
            {
                generated.Add(MarsRockMeshBuilder.CreateRockMesh(seed + i * 97));
            }
            rockMeshes = generated.ToArray();
        }

        if (rockMaterial == null)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");
            rockMaterial = new Material(urpLit)
            {
                name = "MarsBasaltRock",
                color = new Color(0.36f, 0.21f, 0.16f, 1f)
            };
            rockMaterial.SetFloat("_Smoothness", 0.15f);
        }
    }
}
