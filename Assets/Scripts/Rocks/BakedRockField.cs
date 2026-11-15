using UnityEngine;

/// <summary>Ownership marker: regeneration only replaces the linked generated field.</summary>
public sealed class BakedRockField : MonoBehaviour
{
    public RockScatterSettings owner;
    public int seed;
    public int decorativeCount;
    public int obstacleCount;
    public int sampleCount;
    public string layoutSha256;
}
