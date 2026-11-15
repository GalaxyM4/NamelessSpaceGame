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

    [Header("Rover Placement (Does Not Modify Terrain)")]
    public Vector2 spawnWorldXZ = Vector2.zero;
    public Transform snapTarget;
    [Min(0f)] public float snapHeightOffset = 1.3f;
}
