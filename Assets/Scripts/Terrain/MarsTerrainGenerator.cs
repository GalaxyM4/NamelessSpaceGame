using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Editor bake settings. The packaged game uses TerrainData, not source rasters.</summary>
[RequireComponent(typeof(Terrain))]
public class MarsTerrainGenerator : MonoBehaviour
{
    [Header("Prepared HiRISE Source")]
    [Tooltip("Project-relative manifest produced by Tools/HiRISE/prepare.py.")]
    public string manifestPath = "TerrainSources/Prepared/Gale/manifest.json";
    public Vector2 terrainCenterWorldXZ = Vector2.zero;

    [Header("Artistic Mars Surface")]
    [Tooltip("Repeat size of the rock color texture and its aligned normal map, in metres. HiRISE supplies heights only.")]
    [Min(0.1f)] public float surfaceTileMeters = 3f;

    [Header("Rover Spawn Adjustment")]
    public bool flattenSpawn = true;
    public Vector2 spawnWorldXZ = Vector2.zero;
    [Min(0f)] public float spawnSafeRadius = 6f;
    [Min(0.1f)] public float spawnBlendWidth = 12f;
    public Transform snapTarget;
    [Min(0f)] public float snapHeightOffset = 1.3f;

    [Header("Optional Localized Gameplay Edits")]
    public List<HeightAdjustment> adjustments = new List<HeightAdjustment>();

    [Serializable]
    public class HeightAdjustment
    {
        public Vector2 worldXZ;
        [Min(0.1f)] public float radiusMeters = 5f;
        [Min(0.1f)] public float blendMeters = 10f;
        public float offsetMeters;
    }
}
