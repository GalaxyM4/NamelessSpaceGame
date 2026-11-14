using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ControlMode
{
    Rotational,
    Visual
}

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Modo de Vista")]
    public ControlMode currentMode = ControlMode.Rotational;
    public Transform cameraTransform;

    [Header("Fuerzas para movimiento")]
    public float thrustForce = 10f;
    public float rollTorque = 5f;
    public float mouseSensitivity = 0.1f;

    [Header("Camara Visual")]
    public float visualMouseSensitivity = 0.25f;
    public float maxVisualPitch = 85f;
    public float maxVisualYaw = 120f;
    public float cameraReturnSpeed = 8f;

    [Header("Límites y Estabilizador")]
    public float maxSpeed = 20f;
    public float brakeStrength = 3f;
    public float maxMouseTorque = 5f;
    public float maxAngularSpeed = 3f;

    private Rigidbody rb;
    private Vector3 moveInput;
    private Vector3 rotationInput;
    private bool isBraking;

    private float visualPitch;
    private float visualYaw;
    //Events
    public Action<ControlMode> changedControllMode;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.maxAngularVelocity = maxAngularSpeed;

        if (cameraTransform == null)
        {
            Camera childCam = GetComponentInChildren<Camera>();
            if (childCam != null)
            {
                cameraTransform = childCam.transform;
            }
        }

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Start()
    {
        changedControllMode?.Invoke(currentMode);
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null || mouse == null) return;

        if (keyboard.vKey.wasPressedThisFrame)
        {
            currentMode = (currentMode == ControlMode.Rotational)
                ? ControlMode.Visual
                : ControlMode.Rotational;
            changedControllMode?.Invoke(currentMode);
        }

        float moveX = 0f;
        if (keyboard.dKey.isPressed) moveX += 1f;
        if (keyboard.aKey.isPressed) moveX -= 1f;

        float moveZ = 0f;
        if (keyboard.wKey.isPressed) moveZ += 1f;
        if (keyboard.sKey.isPressed) moveZ -= 1f;

        float moveY = 0f;
        if (keyboard.spaceKey.isPressed || keyboard.leftShiftKey.isPressed) moveY += 1f;
        if (keyboard.cKey.isPressed || keyboard.leftCtrlKey.isPressed) moveY -= 1f;

        moveInput = new Vector3(moveX, moveY, moveZ).normalized;

        Vector2 mouseDelta = mouse.delta.ReadValue();

        float roll = 0f;
        if (keyboard.qKey.isPressed) roll += 1f;
        if (keyboard.eKey.isPressed) roll -= 1f;

        if (currentMode == ControlMode.Rotational)
        {
            Vector2 mouseTorque = new Vector2(-mouseDelta.y, mouseDelta.x) * mouseSensitivity;
            mouseTorque = Vector2.ClampMagnitude(mouseTorque, maxMouseTorque);

            rotationInput = new Vector3(mouseTorque.x, mouseTorque.y, roll * rollTorque);

            if (cameraTransform != null)
            {
                visualPitch = Mathf.Lerp(visualPitch, 0f, cameraReturnSpeed * Time.deltaTime);
                visualYaw = Mathf.Lerp(visualYaw, 0f, cameraReturnSpeed * Time.deltaTime);
                cameraTransform.localRotation = Quaternion.Euler(visualPitch, visualYaw, 0f);
            }
        }
        else 
        {
            rotationInput = new Vector3(0f, 0f, roll * rollTorque);
            if (cameraTransform != null)
            {
                visualPitch -= mouseDelta.y * visualMouseSensitivity;
                visualYaw += mouseDelta.x * visualMouseSensitivity;

                visualPitch = Mathf.Clamp(visualPitch, -maxVisualPitch, maxVisualPitch);
                visualYaw = Mathf.Clamp(visualYaw, -maxVisualYaw, maxVisualYaw);

                cameraTransform.localRotation = Quaternion.Euler(visualPitch, visualYaw, 0f);
            }
        }

        isBraking = keyboard.xKey.isPressed;
    }

    void FixedUpdate()
    {
        if (moveInput.sqrMagnitude > 0.01f)
        {
            rb.AddRelativeForce(moveInput * thrustForce, ForceMode.Force);
        }

        if (rotationInput.sqrMagnitude > 0.0001f)
        {
            rb.AddRelativeTorque(rotationInput, ForceMode.Acceleration);
        }

        if (isBraking)
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, brakeStrength * Time.fixedDeltaTime);
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, brakeStrength * Time.fixedDeltaTime);
        }

        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxSpeed);
        }

        if (rb.angularVelocity.magnitude > maxAngularSpeed)
        {
            rb.angularVelocity = Vector3.ClampMagnitude(rb.angularVelocity, maxAngularSpeed);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        rb.angularVelocity = Vector3.zero;
    }
}
