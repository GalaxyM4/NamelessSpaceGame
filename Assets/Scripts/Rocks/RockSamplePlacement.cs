using UnityEngine;

/// <summary>Passive placement metadata only. Interaction and scientific data belong to the gameplay system.</summary>
public sealed class RockSamplePlacement : MonoBehaviour
{
    public RockDefinition definition;
    public string placementId;
    [Min(0.01f)] public float diameterMeters;
    [Tooltip("Nearby rover-center position screened by the placement algorithm, not a gameplay interaction contract.")]
    public Vector3 approachPosition;
}
