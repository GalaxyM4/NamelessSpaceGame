using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class RoverController : MonoBehaviour
{
    [System.Serializable]
    public struct RoverAxle
    {
        public WheelCollider LWColl;
        public WheelCollider RWColl;
        public Transform LWMesh;
        public Transform RWMesh;
        public bool canSteer;
        public bool hasMotor;
    }

    [Header("Ruedas")]
    public RoverAxle[] axles;
    public Vector3 wheelMeshRotationOffset = new Vector3(0f, 0f, 90f);

    [Header("Motor")]
    public float maxMotorTorque = 400f;
    public float maxSteerAngle = 30f;
    public float maxSpeed = 30f;
    public float brakeTorque = 800f;

    [Header("Camara")]
    public float cameraSensitivity = 1f;
    public float maxVisualPitch = 85f;
    public float maxVisualYaw = 120f;
    public float cameraReturnSpeed = 8f;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    private Rigidbody rb;
    private Transform cameraTransform;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction brakeAction;

    private float moveInput;
    private float turnInput;
    private bool isBraking;

    private float cameraHorizontalInput;
    private float cameraVerticalInput;
    private float visualPitch;
    private float visualYaw;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        Camera cameraComp = GetComponentInChildren<Camera>();
        if (cameraComp != null)
        {
            cameraTransform = cameraComp.transform;
        }

        InputActionAsset asset = inputActions != null ? inputActions : InputSystem.actions;
        if (asset != null)
        {
            InputActionMap playerMap = asset.FindActionMap("Player", false);
            if (playerMap != null)
            {
                moveAction = playerMap.FindAction("Move", false);
                lookAction = playerMap.FindAction("Look", false);
                brakeAction = playerMap.FindAction("Jump", false);
            }
        }
    }

    void OnEnable()
    {
        moveAction?.Enable();
        lookAction?.Enable();
        brakeAction?.Enable();
    }

    void Update()
    {
        Vector2 move = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        moveInput = move.y;
        turnInput = move.x;

        Vector2 look = lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
        cameraHorizontalInput = look.x;
        cameraVerticalInput = -look.y;

        isBraking = brakeAction != null && brakeAction.IsPressed();
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        if (Mathf.Abs(cameraHorizontalInput) > 0.01f)
        {
            visualYaw += cameraSensitivity * cameraHorizontalInput * Time.deltaTime;
        }
        else
        {
            visualYaw = Mathf.Lerp(visualYaw, 0f, cameraReturnSpeed * Time.deltaTime);
        }

        if (Mathf.Abs(cameraVerticalInput) > 0.01f)
        {
            visualPitch += cameraSensitivity * cameraVerticalInput * Time.deltaTime;
        }
        else
        {
            visualPitch = Mathf.Lerp(visualPitch, 0f, cameraReturnSpeed * Time.deltaTime);
        }

        visualYaw = Mathf.Clamp(visualYaw, -maxVisualYaw, maxVisualYaw);
        visualPitch = Mathf.Clamp(visualPitch, -maxVisualPitch, maxVisualPitch);

        cameraTransform.localRotation = Quaternion.Euler(visualPitch, visualYaw, 0f);
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (horizontalVelocity.magnitude > maxSpeed)
            {
                Vector3 clamped = Vector3.ClampMagnitude(horizontalVelocity, maxSpeed);
                rb.linearVelocity = new Vector3(clamped.x, rb.linearVelocity.y, clamped.z);
            }
        }

        if (axles == null) return;

        float motor = moveInput * maxMotorTorque;
        float steering = turnInput * maxSteerAngle;
        float brake = isBraking ? brakeTorque : 0f;

        foreach (RoverAxle axle in axles)
        {
            if (axle.canSteer)
            {
                if (axle.LWColl != null) axle.LWColl.steerAngle = steering;
                if (axle.RWColl != null) axle.RWColl.steerAngle = steering;
            }

            if (axle.hasMotor)
            {
                if (axle.LWColl != null) axle.LWColl.motorTorque = motor;
                if (axle.RWColl != null) axle.RWColl.motorTorque = motor;
            }

            if (axle.LWColl != null) axle.LWColl.brakeTorque = brake;
            if (axle.RWColl != null) axle.RWColl.brakeTorque = brake;

            SyncWheelVisuals(axle.LWColl, axle.LWMesh);
            SyncWheelVisuals(axle.RWColl, axle.RWMesh);
        }
    }

    private void SyncWheelVisuals(WheelCollider col, Transform visualMesh)
    {
        if (col == null || visualMesh == null) return;

        col.GetWorldPose(out Vector3 pos, out Quaternion rot);
        visualMesh.SetPositionAndRotation(pos, rot * Quaternion.Euler(wheelMeshRotationOffset));
    }
}
