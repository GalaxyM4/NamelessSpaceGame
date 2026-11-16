using System;
using System.Collections;
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

    [Header("Tutorial Lobby")]
    public bool enableTutorial = true;
    public float tutorialStepHoldTime = 0.4f;
    public Transform doorPositiveTransform;
    public Transform doorNegativeTransform;
    public float doorOpenOffset = 0.004f;
    public float doorOpenDuration = 0.8f;

    private Rigidbody rb;
    private Vector3 moveInput;
    private Vector3 rotationInput;
    private bool isBraking;
    private IInteractable currentInteractable;
    private Label promptLabel;
    private Label keyLabel;
    private VisualElement tutorialContainer;
    private Label tutorialPhaseLabel;
    private Label tutorialTextLabel;
    private VisualElement tutorialProgressFill;
    private int tutorialPhase = 1;
    private float tutorialPhaseTimer;
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
        SetupTutorialDoors();
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
        tutorialContainer = root.Q<VisualElement>("tutorialContainer");
        if (tutorialContainer == null)
        {
            BuildFallbackTutorialHUD(root);
        }
        tutorialPhaseLabel = root.Q<Label>("tutorialPhaseLabel");
        tutorialTextLabel = root.Q<Label>("tutorialTextLabel");
        tutorialProgressFill = root.Q<VisualElement>("tutorialProgressFill");
        UpdatePromptHUD(null);
        UpdateTutorialUI();
    }

    private void BuildFallbackTutorialHUD(VisualElement root)
    {
        VisualElement hudRoot = root.Q<VisualElement>("hudRoot") ?? root;

        tutorialContainer = new VisualElement { name = "tutorialContainer", pickingMode = PickingMode.Ignore };
        tutorialContainer.AddToClassList("tutorial-container");
        tutorialContainer.style.position = Position.Absolute;
        tutorialContainer.style.bottom = 48;
        tutorialContainer.style.minWidth = 520;
        tutorialContainer.style.paddingTop = 14;
        tutorialContainer.style.paddingBottom = 14;
        tutorialContainer.style.paddingLeft = 24;
        tutorialContainer.style.paddingRight = 24;
        tutorialContainer.style.backgroundColor = new Color(15f / 255f, 20f / 255f, 30f / 255f, 0.82f);
        tutorialContainer.style.borderTopLeftRadius = 10;
        tutorialContainer.style.borderTopRightRadius = 10;
        tutorialContainer.style.borderBottomLeftRadius = 10;
        tutorialContainer.style.borderBottomRightRadius = 10;
        tutorialContainer.style.borderTopWidth = 1;
        tutorialContainer.style.borderBottomWidth = 1;
        tutorialContainer.style.borderLeftWidth = 1;
        tutorialContainer.style.borderRightWidth = 1;
        Color teal = new Color(55f / 255f, 200f / 255f, 171f / 255f, 1f);
        tutorialContainer.style.borderTopColor = teal;
        tutorialContainer.style.borderBottomColor = teal;
        tutorialContainer.style.borderLeftColor = teal;
        tutorialContainer.style.borderRightColor = teal;
        tutorialContainer.style.alignItems = Align.Center;
        tutorialContainer.style.justifyContent = Justify.Center;

        var phaseLbl = new Label("Fase 1:") { name = "tutorialPhaseLabel", pickingMode = PickingMode.Ignore };
        phaseLbl.AddToClassList("tutorial-phase-label");
        phaseLbl.style.fontSize = 20;
        phaseLbl.style.color = teal;
        phaseLbl.style.marginBottom = 6;
        phaseLbl.style.unityTextAlign = TextAnchor.MiddleCenter;

        var textLbl = new Label("WASD, movimiento básico del personaje") { name = "tutorialTextLabel", pickingMode = PickingMode.Ignore };
        textLbl.AddToClassList("tutorial-text-label");
        textLbl.style.fontSize = 26;
        textLbl.style.color = Color.white;
        textLbl.style.marginBottom = 10;
        textLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
        textLbl.style.whiteSpace = WhiteSpace.Normal;

        var track = new VisualElement { name = "tutorialProgressTrack", pickingMode = PickingMode.Ignore };
        track.AddToClassList("tutorial-progress-track");
        track.style.width = Length.Percent(100);
        track.style.height = 6;
        track.style.backgroundColor = new Color(1f, 1f, 1f, 0.15f);
        track.style.borderTopLeftRadius = 3;
        track.style.borderTopRightRadius = 3;
        track.style.borderBottomLeftRadius = 3;
        track.style.borderBottomRightRadius = 3;
        track.style.overflow = Overflow.Hidden;

        var fill = new VisualElement { name = "tutorialProgressFill", pickingMode = PickingMode.Ignore };
        fill.AddToClassList("tutorial-progress-fill");
        fill.style.width = Length.Percent(0);
        fill.style.height = Length.Percent(100);
        fill.style.backgroundColor = teal;

        track.Add(fill);
        tutorialContainer.Add(phaseLbl);
        tutorialContainer.Add(textLbl);
        tutorialContainer.Add(track);
        hudRoot.Add(tutorialContainer);
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
            else if (t.name.StartsWith("INT_CMD_TouchPanel_02", StringComparison.OrdinalIgnoreCase) ||
                     t.name.StartsWith("INT_CMD_TouchPanel_2", StringComparison.OrdinalIgnoreCase))
            {
                if (t.GetComponent<TouchPanelModuleDisplay>() == null)
                {
                    t.gameObject.AddComponent<TouchPanelModuleDisplay>().SetSlotOffset(0);
                }
            }
            else if (t.name.StartsWith("INT_CMD_TouchPanel_03", StringComparison.OrdinalIgnoreCase) ||
                     t.name.StartsWith("INT_CMD_TouchPanel_3", StringComparison.OrdinalIgnoreCase))
            {
                if (t.GetComponent<TouchPanelModuleDisplay>() == null)
                {
                    t.gameObject.AddComponent<TouchPanelModuleDisplay>().SetSlotOffset(1);
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

        UpdateTutorial(keyboard);
        HandleInteraction(keyboard);
    }

    private void SetupTutorialDoors()
    {
        if (!enableTutorial) return;

        Transform[] allTransforms = FindObjectsByType<Transform>();
        if (doorPositiveTransform == null || doorNegativeTransform == null)
        {
            foreach (Transform t in allTransforms)
            {
                if (t == transform || t.IsChildOf(transform)) continue;

                if (doorPositiveTransform == null && Mathf.Abs(t.localPosition.x - doorOpenOffset) < 0.0005f)
                {
                    doorPositiveTransform = t;
                }
                else if (doorNegativeTransform == null && Mathf.Abs(t.localPosition.x + doorOpenOffset) < 0.0005f)
                {
                    doorNegativeTransform = t;
                }
            }
        }

        if (doorPositiveTransform == null || doorNegativeTransform == null)
        {
            foreach (Transform t in allTransforms)
            {
                if (doorPositiveTransform == null &&
                    (t.name.Equals("PREVIEW_CMD_JNT_DOOR_Slide_L", StringComparison.OrdinalIgnoreCase) ||
                     t.name.Equals("SM_DOOR_Leaf_L", StringComparison.OrdinalIgnoreCase)))
                {
                    doorPositiveTransform = t;
                }
                else if (doorNegativeTransform == null &&
                         (t.name.Equals("PREVIEW_CMD_JNT_DOOR_Slide_R", StringComparison.OrdinalIgnoreCase) ||
                          t.name.Equals("SM_DOOR_Leaf_R", StringComparison.OrdinalIgnoreCase)))
                {
                    doorNegativeTransform = t;
                }
            }
        }

        if (doorPositiveTransform != null)
        {
            Vector3 pos = doorPositiveTransform.localPosition;
            doorPositiveTransform.localPosition = new Vector3(0f, pos.y, pos.z);
        }

        if (doorNegativeTransform != null)
        {
            Vector3 pos = doorNegativeTransform.localPosition;
            doorNegativeTransform.localPosition = new Vector3(0f, pos.y, pos.z);
        }

        tutorialPhase = 1;
        tutorialPhaseTimer = 0f;
        UpdateTutorialUI();
    }

    private void UpdateTutorial(Keyboard keyboard)
    {
        if (!enableTutorial || tutorialPhase > 4) return;

        bool phaseInputActive = false;
        switch (tutorialPhase)
        {
            case 1:
                phaseInputActive = keyboard.wKey.isPressed || keyboard.aKey.isPressed ||
                                   keyboard.sKey.isPressed || keyboard.dKey.isPressed;
                break;
            case 2:
                phaseInputActive = keyboard.qKey.isPressed || keyboard.eKey.isPressed;
                break;
            case 3:
                phaseInputActive = keyboard.spaceKey.isPressed || keyboard.leftCtrlKey.isPressed ||
                                   keyboard.rightCtrlKey.isPressed || keyboard.cKey.isPressed;
                break;
            case 4:
                phaseInputActive = keyboard.xKey.isPressed;
                break;
        }

        if (phaseInputActive)
        {
            tutorialPhaseTimer += Time.deltaTime;
            if (tutorialPhaseTimer >= tutorialStepHoldTime)
            {
                tutorialPhase++;
                tutorialPhaseTimer = 0f;

                if (tutorialPhase > 4)
                {
                    StartCoroutine(OpenTutorialDoorsCoroutine());
                }
            }
            UpdateTutorialUI();
        }
    }

    private void UpdateTutorialUI()
    {
        if (tutorialContainer == null) return;

        if (!enableTutorial || tutorialPhase > 4)
        {
            tutorialContainer.AddToClassList("prompt-hidden");
            return;
        }

        tutorialContainer.RemoveFromClassList("prompt-hidden");

        if (tutorialPhaseLabel != null)
        {
            tutorialPhaseLabel.text = $"Fase {tutorialPhase}:";
        }

        if (tutorialTextLabel != null)
        {
            switch (tutorialPhase)
            {
                case 1:
                    tutorialTextLabel.text = "WASD, movimiento básico del personaje";
                    break;
                case 2:
                    tutorialTextLabel.text = "Q, E: Movimiento rotacional";
                    break;
                case 3:
                    tutorialTextLabel.text = "Space, Ctrl o C: Movimiento vertical";
                    break;
                case 4:
                    tutorialTextLabel.text = "X: Frenar al personaje";
                    break;
            }
        }

        if (tutorialProgressFill != null)
        {
            float pct = Mathf.Clamp01(tutorialPhaseTimer / Mathf.Max(0.01f, tutorialStepHoldTime)) * 100f;
            tutorialProgressFill.style.width = Length.Percent(pct);
        }
    }

    private IEnumerator OpenTutorialDoorsCoroutine()
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, doorOpenDuration);

        Vector3 posStart = doorPositiveTransform != null ? doorPositiveTransform.localPosition : Vector3.zero;
        Vector3 negStart = doorNegativeTransform != null ? doorNegativeTransform.localPosition : Vector3.zero;

        Vector3 posTarget = new Vector3(doorOpenOffset, posStart.y, posStart.z);
        Vector3 negTarget = new Vector3(-doorOpenOffset, negStart.y, negStart.z);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            if (doorPositiveTransform != null)
            {
                doorPositiveTransform.localPosition = Vector3.Lerp(posStart, posTarget, t);
            }
            if (doorNegativeTransform != null)
            {
                doorNegativeTransform.localPosition = Vector3.Lerp(negStart, negTarget, t);
            }

            yield return null;
        }

        if (doorPositiveTransform != null)
        {
            doorPositiveTransform.localPosition = posTarget;
        }
        if (doorNegativeTransform != null)
        {
            doorNegativeTransform.localPosition = negTarget;
        }
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
