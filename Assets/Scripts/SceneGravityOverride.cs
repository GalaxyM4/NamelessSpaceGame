using UnityEngine;

public class SceneGravityOverride : MonoBehaviour
{
    [SerializeField] private Vector3 gravity = new Vector3(0f, -3.72f, 0f);

    private Vector3 previousGravity;

    void OnEnable()
    {
        previousGravity = Physics.gravity;
        Physics.gravity = gravity;
    }

    void OnDisable()
    {
        Physics.gravity = previousGravity;
    }
}
