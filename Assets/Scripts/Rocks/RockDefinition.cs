using UnityEngine;

public enum RockRole { Decoration, Obstacle, Sample }

/// <summary>Shared visual assets and distribution preference; no gameplay/scientific properties.</summary>
[CreateAssetMenu(menuName = "Mars/Rock Definition")]
public sealed class RockDefinition : ScriptableObject
{
    public string displayName;
    public Mesh mesh;
    public Mesh collisionMesh;
    public Material material;
    [Range(0f, 1f)] public float preferredField;
}
