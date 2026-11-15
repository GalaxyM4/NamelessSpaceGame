using UnityEngine;

/// <summary>XZ circle/corridor. A second endpoint makes it a capsule; never reshapes terrain.</summary>
public sealed class RockExclusionZone : MonoBehaviour
{
    [Min(0f)] public float radius = 5f;
    public Transform corridorEnd;

    public bool Contains(Vector3 point, float padding)
    {
        Vector2 a = new Vector2(transform.position.x, transform.position.z);
        Vector2 b = corridorEnd != null
            ? new Vector2(corridorEnd.position.x, corridorEnd.position.z) : a;
        Vector2 p = new Vector2(point.x, point.z);
        Vector2 delta = b - a;
        float t = delta.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(p - a, delta) / delta.sqrMagnitude) : 0;
        return Vector2.Distance(p, a + t * delta) < radius + padding;
    }
}
