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

    [Header("Audio Llantas")]
    public AudioClip wheelStartSound;
    public AudioClip wheelLoopSound;
    public AudioClip wheelEndSound;
    public AudioSource wheelAudioSource;
    public float wheelMoveThreshold = 0.15f;
    private bool wasWheelMoving;

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
        if (movementSound != null)
        {
            movementSound.playOnAwake = false;
            movementSound.Stop();
        }

        if (wheelAudioSource == null)
        {
            wheelAudioSource = gameObject.AddComponent<AudioSource>();
            wheelAudioSource.playOnAwake = false;
            wheelAudioSource.loop = false;
            if (movementSound != null)
            {
                wheelAudioSource.spatialBlend = movementSound.spatialBlend;
                wheelAudioSource.volume = movementSound.volume;
                wheelAudioSource.outputAudioMixerGroup = movementSound.outputAudioMixerGroup;
                wheelAudioSource.minDistance = movementSound.minDistance;
                wheelAudioSource.maxDistance = movementSound.maxDistance;
            }
        }
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
        UpdateLoopingSound(movementSound, startSound, loopSound, endSound, isCameraMoving, ref wasCameraMoving);
    }

    private void UpdateWheelSound(bool isWheelMoving)
    {
        UpdateLoopingSound(wheelAudioSource, wheelStartSound, wheelLoopSound, wheelEndSound, isWheelMoving, ref wasWheelMoving);
    }

    private void UpdateLoopingSound(
        AudioSource source,
        AudioClip startClip,
        AudioClip loopClip,
        AudioClip endClip,
        bool isMoving,
        ref bool wasMoving)
    {
        if (source == null) return;

        if (isMoving && !wasMoving)
        {
            source.Stop();

            if (startClip != null)
            {
                source.clip = startClip;
                source.loop = false;
                source.Play();
            }
            else if (loopClip != null)
            {
                source.clip = loopClip;
                source.loop = true;
                source.Play();
            }
        }
        else if (isMoving && wasMoving)
        {
            if (!source.isPlaying && loopClip != null)
            {
                source.clip = loopClip;
                source.loop = true;
                source.Play();
            }
        }
        else if (!isMoving && wasMoving)
        {
            source.Stop();
            source.loop = false;
            source.clip = null;

            if (endClip != null)
            {
                source.PlayOneShot(endClip);
            }
        }

        wasMoving = isMoving;
    }

    private bool IsAnyWheelGrounded()
    {
        if (axles == null) return false;
        foreach (RoverAxle axle in axles)
        {
            if ((axle.LWColl != null && axle.LWColl.isGrounded) ||
                (axle.RWColl != null && axle.RWColl.isGrounded))
            {
                return true;
            }
        }
        return false;
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

        bool hasMotorInput = Mathf.Abs(moveInput) > 0.01f && !isBraking;
        bool isRolling = rb.linearVelocity.magnitude > wheelMoveThreshold;
        bool isWheelMoving = IsAnyWheelGrounded() && (hasMotorInput || isRolling);
        UpdateWheelSound(isWheelMoving);

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

