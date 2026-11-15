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
    
    [Header("Motor")]
    public float maxMotorTorque = 400f;
    public float maxSteerAngle = 30f;
    public float maxSpeed = 30f;
    public float brakeTorque = 800f;
    //private
    private float moveInput;
    private float turnInput;
    private bool isBraking;

    [Header("Audio")]
    public AudioClip startSound;
    public AudioClip loopSound;
    public AudioClip endSound;
    private AudioSource movementSound;
    private bool wasCameraMoving;

    [Header("Camara")]
    public float cameraSensitivity = 1f;
    public float maxVisualPitch = 85f;
    public float maxVisualYaw = 120f;
    public float cameraReturnSpeed = 8f;
    // private
    private float cameraHorizontalInput;
    private float cameraVerticalInput;
    private float visualPitch;
    private float visualYaw;

    void Start()
    {
        movementSound = GetComponentInChildren<AudioSource>();
        movementSound.playOnAwake = true;
        movementSound.Stop();
    }
    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        moveInput = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
        turnInput = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        cameraHorizontalInput = (kb.eKey.isPressed ? 1f : 0f) - (kb.qKey.isPressed ? 1f : 0f);
        cameraVerticalInput = (kb.leftCtrlKey.isPressed ? 1f : 0f) - (kb.leftShiftKey.isPressed ? 1f : 0f);

        isBraking = kb.spaceKey.isPressed;
    }

    private void UpdateSound(bool isCameraMoving)
    {
        if (movementSound == null) return;

        if (isCameraMoving && !wasCameraMoving)
        {
            if (startSound != null)
                movementSound.PlayOneShot(startSound);

            if (loopSound != null)
            {
                movementSound.clip = loopSound;
                movementSound.loop = true;
                movementSound.Play();
            }
        } else if (!isCameraMoving && wasCameraMoving)
        {
            movementSound.PlayOneShot(endSound);
            movementSound.Stop();
            movementSound.loop = false;

            if (endSound != null) movementSound.PlayOneShot(endSound);
        }

        wasCameraMoving = isCameraMoving;
    }

    void FixedUpdate()
    {
        Camera cameraComp = GetComponentInChildren<Camera>();
        float motor = moveInput * maxMotorTorque;
        float steering = turnInput * maxSteerAngle;
        float brake = isBraking ? brakeTorque : 0f;

        float prevYaw = visualYaw;
        float prevPitch = visualPitch;

        visualYaw += cameraSensitivity * cameraHorizontalInput;
        visualPitch += cameraSensitivity * cameraVerticalInput;

        bool isCameraMoving = !Mathf.Approximately(prevYaw, visualYaw) || !Mathf.Approximately(prevPitch, visualPitch);
        UpdateSound(isCameraMoving);

        visualYaw = Mathf.Clamp(visualYaw, -maxVisualYaw, maxVisualYaw);
        visualPitch = Mathf.Clamp(visualPitch, -maxVisualPitch, maxVisualPitch);

        cameraComp.transform.localRotation = Quaternion.Euler(visualPitch, visualYaw, 0f);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxSpeed);
        }

        foreach (RoverAxle axle in axles)
        {
            if (axle.canSteer)
            {
                axle.LWColl.steerAngle = steering;
                axle.RWColl.steerAngle = steering;
            }

            if (axle.hasMotor)
            {
                axle.LWColl.motorTorque = motor;
                axle.RWColl.motorTorque = motor;
            }

            axle.LWColl.brakeTorque = brake;
            axle.RWColl.brakeTorque = brake;

            SyncWheelVisuals(axle.LWColl, axle.LWMesh);
            SyncWheelVisuals(axle.RWColl, axle.RWMesh);
        }
    }

    private void SyncWheelVisuals(WheelCollider col, Transform visualMesh)
    {
        if (visualMesh == null) return;
        col.GetWorldPose(out Vector3 pos, out Quaternion rot);
        visualMesh.SetPositionAndRotation(pos, rot * Quaternion.Euler(0f, 0f, 90f));
    }
}

