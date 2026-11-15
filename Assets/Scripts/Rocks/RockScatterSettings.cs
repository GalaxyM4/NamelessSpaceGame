using System;
using UnityEngine;

[Serializable]
public sealed class RockPopulation
{
    [Min(0)] public int count;
    [Min(0.01f)] public float minimumDiameter;
    [Min(0.01f)] public float maximumDiameter;
    [Min(1f)] public float cullDistance;
}

/// <summary>Editor bake settings only. Does not generate rocks or modify heights at runtime.</summary>
public sealed class RockScatterSettings : MonoBehaviour
{
    public Terrain terrain;
    public Transform rover;
    public RockDefinition[] definitions;
    public int seed = 24175;
    public RockPopulation decoration = new RockPopulation
        { count = 10000, minimumDiameter = 0.08f, maximumDiameter = 0.24f, cullDistance = 140f };
    public RockPopulation obstacles = new RockPopulation
        { count = 1600, minimumDiameter = 0.5f, maximumDiameter = 2.4f, cullDistance = 450f };
    [Tooltip("Passive study-candidate rocks. Does not add collection or study behaviour.")]
    public RockPopulation samples = new RockPopulation
        { count = 60, minimumDiameter = 0.16f, maximumDiameter = 0.3f, cullDistance = 180f };
    [Min(2f)] public float fieldScale = 85f;
    [Range(0f, 1f)] public float clustering = 0.85f;
    [Range(0f, 50f)] public float maximumSlope = 32f;
    [Min(0f)] public float spawnClearance = 10f;
    [Min(0.1f)] public float minimumGap = 0.15f;
    [Range(0f, 0.4f)] public float burialFraction = 0.16f;
    [Range(0f, 1f)] public float groundAlignment = 0.45f;
    [Min(0.5f)] public float roverClearanceRadius = 2f;
    [Tooltip("Settled rover-center height, not its potentially airborne Editor spawn position.")]
    [Min(0.1f)] public float settledRoverHeight = 1.4f;
    [Tooltip("Placement-only proximity screen for candidate approach points. Gameplay owns actual interaction reach.")]
    [Min(0.5f)] public float candidateApproachRange = 4.5f;
    [Range(1f, 30f)] public float drivingSlopeLimit = 15f;
    [Min(2f)] public float routeGridMeters = 4f;
    [Min(5f)] public float starterSampleRadius = 45f;
    [Min(0)] public int starterSamples = 5;
    [Tooltip("Optional objective points. A nearby sample is required for each.")]
    public Transform[] studySites = Array.Empty<Transform>();
    [Tooltip("Assign RockExclusionZone objects to protect objectives or driving corridors.")]
    public RockExclusionZone[] exclusions = Array.Empty<RockExclusionZone>();
    [HideInInspector] public Transform bakedRoot;
    [HideInInspector] public GameObject layoutPrefab;
}
