using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

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

    [Header("Interacción")]
    public float interactDistance = 10f;
    public LayerMask interactLayerMask = ~0;
    public PanelRenderer hudRenderer;

    private Rigidbody rb;
    private Vector3 moveInput;
    private Vector3 rotationInput;
    private bool isBraking;
    private IInteractable currentInteractable;
    private Label promptLabel;
    private Label keyLabel;
    private bool isInteractingWithUI;

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

        if (hudRenderer == null)
        {
            hudRenderer = GetComponentInChildren<PanelRenderer>();
        }

        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        SetupMainScreenInteractables();
    }

    void OnEnable()
    {
        if (hudRenderer == null)
        {
            hudRenderer = GetComponentInChildren<PanelRenderer>();
        }
        if (hudRenderer != null)
        {
            hudRenderer.RegisterUIReloadCallback(OnHUDReload);
        }
    }

    private void OnHUDReload(PanelRenderer renderer, VisualElement root, int version)
    {
        promptLabel = root.Q<Label>("promptText");
        keyLabel = root.Q<Label>("keyLabel");
        UpdatePromptHUD(null);
    }

    private void UpdatePromptHUD(string text)
    {
        if (promptLabel == null) return;

        if (string.IsNullOrEmpty(text))
        {
            promptLabel.text = string.Empty;
            promptLabel.AddToClassList("prompt-hidden");
            if (keyLabel != null) keyLabel.AddToClassList("prompt-hidden");
        }
        else
        {
            promptLabel.text = text;
            promptLabel.RemoveFromClassList("prompt-hidden");
            if (keyLabel != null) keyLabel.RemoveFromClassList("prompt-hidden");
        }
    }

    public void SetInteractingWithUI(bool interacting)
    {
        isInteractingWithUI = interacting;
        if (interacting)
        {
            moveInput = Vector3.zero;
            rotationInput = Vector3.zero;
            visualPitch = 0f;
            visualYaw = 0f;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.constraints = RigidbodyConstraints.FreezeAll;
            }
            if (cameraTransform != null)
            {
                cameraTransform.localPosition = Vector3.zero;
                cameraTransform.localRotation = Quaternion.identity;
            }
            UpdatePromptHUD(null);
            if (hudRenderer != null)
            {
                hudRenderer.enabled = false;
            }
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }
        else
        {
            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints.None;
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            if (hudRenderer != null)
            {
                hudRenderer.enabled = true;
            }
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }
    }

    private void SetupMainScreenInteractables()
    {
        foreach (Transform t in FindObjectsByType<Transform>())
        {
            if (t.name.StartsWith("INT_CMD_MainScreen", StringComparison.OrdinalIgnoreCase))
            {
                t.gameObject.tag = "Interactible";
                if (t.GetComponent<Collider>() == null)
                {
                    t.gameObject.AddComponent<BoxCollider>();
                }
                if (t.GetComponent<IInteractable>() == null)
                {
                    t.gameObject.AddComponent<MainScreenInteractable>();
                }
            }
            else if (t.name.StartsWith("INT_CMD_TouchPanel_01", StringComparison.OrdinalIgnoreCase) ||
                     t.name.StartsWith("INT_CMD_TouchPanel_1", StringComparison.OrdinalIgnoreCase))
            {
                t.gameObject.tag = "Interactible";
                BoxCollider box = t.GetComponent<BoxCollider>();
                if (box == null && t.GetComponent<Collider>() == null)
                {
                    box = t.gameObject.AddComponent<BoxCollider>();
                }
                if (box != null && box.size.z < 0.001f)
                {
                    box.size = new Vector3(
                        Mathf.Max(box.size.x, 0.0042f),
                        Mathf.Max(box.size.y, 0.0024f),
                        0.0015f
                    );
                }
                if (t.GetComponent<IInteractable>() == null)
                {
                    t.gameObject.AddComponent<BackgroundMusicInteractable>();
                }
            }
        }
    }

    void OnDisable()
    {
        if (hudRenderer != null)
        {
            hudRenderer.UnregisterUIReloadCallback(OnHUDReload);
        }
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }

    void Start()
    {
        changedControllMode?.Invoke(currentMode);
    }

    void Update()
    {
        if (isInteractingWithUI) return;

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

        HandleInteraction(keyboard);
    }

    private void HandleInteraction(Keyboard keyboard)
    {
        if (cameraTransform == null) return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.cyan);

        RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance, interactLayerMask, QueryTriggerInteraction.Collide);
        if (hits != null && hits.Length > 0)
        {
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null) continue;
                if (hit.collider.transform.IsChildOf(transform)) continue;

                bool hasInteractTag = hit.collider.CompareTag("Interactible") || hit.collider.CompareTag("interactable");
                IInteractable interactable = hit.collider.GetComponent<IInteractable>()
                    ?? hit.collider.GetComponentInParent<IInteractable>();

                if (interactable != null && (hasInteractTag || interactable is MonoBehaviour))
                {
                    currentInteractable = interactable;
                    UpdatePromptHUD(currentInteractable.GetPromptText());

                    if (keyboard.fKey.wasPressedThisFrame)
                    {
                        currentInteractable.Interact();
                    }
                    return;
                }
            }
        }

        currentInteractable = null;
        UpdatePromptHUD(null);
    }

    void FixedUpdate()
    {
        if (isInteractingWithUI) return;

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
