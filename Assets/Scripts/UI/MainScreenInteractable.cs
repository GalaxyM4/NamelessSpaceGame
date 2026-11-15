using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainScreenInteractable : MonoBehaviour, IInteractable
{
    [Header("Interacción y Cámara")]
    public string promptText = "Usar Pantalla";
    public float transitionDuration = 0.4f;
    public float screenDistanceMultiplier = 1.15f;

    [Header("UI de la Pantalla")]
    public PanelRenderer screenPanelRenderer;

    private PlayerController playerController;
    private Rigidbody playerRb;
    private Transform cameraTransform;
    private Camera playerCamera;

    private Vector3 savedPlayerPos;
    private Quaternion savedPlayerRot;
    private Vector3 savedCameraLocalPos;
    private Quaternion savedCameraLocalRot;
    private float savedNearClip = 0.3f;

    private Vector3 currentTargetPos;
    private Quaternion currentTargetRot;

    private bool isFocused;
    private bool isTransitioning;
    private Coroutine transitionCoroutine;

    private int currentMissionIndex;
    private VisualElement rootElement;
    private VisualElement missionOneCard;
    private Button missionImageButton;
    private Label missionTitleLabel;
    private Label missionInfoLabel;
    private Label urlInfoLabel;
    private Button startMissionButton;
    private int lastClickFrame = -1;

    private void Awake()
    {
        FindScreenPanelRenderer();
    }

    private void OnEnable()
    {
        FindScreenPanelRenderer();
        if (screenPanelRenderer != null)
        {
            screenPanelRenderer.RegisterUIReloadCallback(OnScreenUIReload);
            SetupScreenToPanelSpace();
        }
    }

    private void OnDisable()
    {
        if (screenPanelRenderer != null)
        {
            screenPanelRenderer.UnregisterUIReloadCallback(OnScreenUIReload);
            if (screenPanelRenderer.panelSettings != null)
            {
                screenPanelRenderer.panelSettings.SetScreenToPanelSpaceFunction(null);
            }
        }
        UnbindButton();
    }

    private void FindScreenPanelRenderer()
    {
        if (screenPanelRenderer != null) return;

        PanelRenderer[] renderers = FindObjectsByType<PanelRenderer>();
        foreach (PanelRenderer r in renderers)
        {
            if (r.gameObject.name.Contains("MainScreen", System.StringComparison.OrdinalIgnoreCase) ||
                (r.panelSettings != null && r.panelSettings.name == "ScreenPanelSettings"))
            {
                screenPanelRenderer = r;
                break;
            }
        }
    }

    private void OnScreenUIReload(PanelRenderer renderer, VisualElement root, int version)
    {
        UnbindButton();

        rootElement = root;
        missionOneCard = root.Q<VisualElement>("misionOne");
        if (missionOneCard != null)
        {
            missionOneCard.SetEnabled(true);
        }

        missionImageButton = root.Q<Button>("MissionButtonImage");
        missionTitleLabel = root.Q<Label>("MissionTitle");
        missionInfoLabel = root.Q<Label>(className: "info-mision");
        urlInfoLabel = root.Q<Label>("urlInfo");
        startMissionButton = root.Q<Button>("Button");

        if (startMissionButton != null)
        {
            startMissionButton.SetEnabled(true);
            startMissionButton.RegisterCallback<ClickEvent>(OnStartMissionClicked);
        }

        UpdateMissionUI();
    }

    private void UnbindButton()
    {
        if (startMissionButton != null)
        {
            startMissionButton.UnregisterCallback<ClickEvent>(OnStartMissionClicked);
        }
    }

    private void OnStartMissionClicked(ClickEvent evt)
    {
        TriggerStartMission();
    }

    private void TriggerStartMission()
    {
        if (Time.frameCount == lastClickFrame) return;
        lastClickFrame = Time.frameCount;
        LoadingManegment.TargetScene = "MarsScene";
        SceneManager.LoadScene("LoadingScene");
    }

    private void UpdateMissionUI()
    {
        IReadOnlyList<Mission> missions = MissionsImplement.GetMissions;
        if (missions == null || missions.Count == 0) return;

        if (currentMissionIndex < 0) currentMissionIndex = missions.Count - 1;
        if (currentMissionIndex >= missions.Count) currentMissionIndex = 0;

        Mission current = missions[currentMissionIndex];

        if (missionTitleLabel != null)
        {
            missionTitleLabel.text = $"Misión #{currentMissionIndex + 1} - {current.name}";
        }

        if (missionInfoLabel != null)
        {
            missionInfoLabel.text = current.information;
        }

        if (urlInfoLabel != null)
        {
            urlInfoLabel.text = current.urlInformation;
        }

        if (missionImageButton != null)
        {
            if (current.imageReference != null)
            {
                missionImageButton.style.backgroundImage = new StyleBackground(current.imageReference);
            }
            else
            {
                missionImageButton.style.backgroundImage = StyleKeyword.Null;
            }
        }
    }

    public string GetPromptText()
    {
        return promptText;
    }

    public void Interact()
    {
        if (isTransitioning) return;

        if (!isFocused)
        {
            EnterScreenFocus();
        }
        else
        {
            ExitScreenFocus();
        }
    }

    private void EnterScreenFocus()
    {
        if (playerController == null)
        {
            playerController = FindAnyObjectByType<PlayerController>();
        }

        if (playerController == null || playerController.cameraTransform == null) return;

        playerRb = playerController.GetComponent<Rigidbody>();
        cameraTransform = playerController.cameraTransform;
        playerCamera = cameraTransform.GetComponent<Camera>();

        savedPlayerPos = playerController.transform.position;
        savedPlayerRot = playerController.transform.rotation;
        savedCameraLocalPos = cameraTransform.localPosition;
        savedCameraLocalRot = cameraTransform.localRotation;

        if (playerCamera != null)
        {
            savedNearClip = playerCamera.nearClipPlane;
            playerCamera.nearClipPlane = 0.05f;
        }

        isFocused = true;
        playerController.SetInteractingWithUI(true);
        UpdateMissionUI();

        GetScreenCameraTarget(out currentTargetPos, out currentTargetRot);

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionToScreen(currentTargetPos, currentTargetRot));
    }

    public void ExitScreenFocus()
    {
        if (!isFocused || isTransitioning) return;

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionBackToPlayer());
    }

    private void Update()
    {
        if (!isFocused || isTransitioning) return;

        FixPlayerAndCamera(currentTargetPos, currentTargetRot);

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.wasPressedThisFrame)
            {
                ChangeMission(-1);
            }
            else if (keyboard.dKey.wasPressedThisFrame)
            {
                ChangeMission(1);
            }
            else if (keyboard.escapeKey.wasPressedThisFrame || keyboard.fKey.wasPressedThisFrame)
            {
                ExitScreenFocus();
                return;
            }
        }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && startMissionButton != null)
        {
            Vector2 mousePos = mouse.position.ReadValue();
            if (TryGetPanelPointFromScreen(mousePos, true, out Vector2 panelPoint))
            {
                if (startMissionButton.worldBound.Contains(panelPoint))
                {
                    TriggerStartMission();
                }
            }
        }
    }

    private void ChangeMission(int delta)
    {
        IReadOnlyList<Mission> missions = MissionsImplement.GetMissions;
        if (missions == null || missions.Count == 0) return;

        currentMissionIndex = (currentMissionIndex + delta % missions.Count + missions.Count) % missions.Count;
        UpdateMissionUI();
    }

    private IEnumerator TransitionToScreen(Vector3 targetPos, Quaternion targetRot)
    {
        isTransitioning = true;
        Vector3 startPos = playerController.transform.position;
        Quaternion startRot = playerController.transform.rotation;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, transitionDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            Vector3 pos = Vector3.Lerp(startPos, targetPos, t);
            Quaternion rot = Quaternion.Slerp(startRot, targetRot, t);

            FixPlayerAndCamera(pos, rot);
            yield return null;
        }

        FixPlayerAndCamera(targetPos, targetRot);
        isTransitioning = false;
    }

    private IEnumerator TransitionBackToPlayer()
    {
        isTransitioning = true;
        Vector3 startPos = playerController.transform.position;
        Quaternion startRot = playerController.transform.rotation;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, transitionDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            Vector3 pos = Vector3.Lerp(startPos, savedPlayerPos, t);
            Quaternion rot = Quaternion.Slerp(startRot, savedPlayerRot, t);

            FixPlayerAndCamera(pos, rot);
            cameraTransform.localPosition = Vector3.Lerp(Vector3.zero, savedCameraLocalPos, t);
            cameraTransform.localRotation = Quaternion.Slerp(Quaternion.identity, savedCameraLocalRot, t);
            yield return null;
        }

        FixPlayerAndCamera(savedPlayerPos, savedPlayerRot);
        cameraTransform.localPosition = savedCameraLocalPos;
        cameraTransform.localRotation = savedCameraLocalRot;

        if (playerCamera != null)
        {
            playerCamera.nearClipPlane = savedNearClip;
        }

        isFocused = false;
        isTransitioning = false;

        if (playerController != null)
        {
            playerController.SetInteractingWithUI(false);
        }
    }

    private void FixPlayerAndCamera(Vector3 worldPos, Quaternion worldRot)
    {
        if (playerController != null)
        {
            playerController.transform.SetPositionAndRotation(worldPos, worldRot);
        }
        if (playerRb != null)
        {
            playerRb.position = worldPos;
            playerRb.rotation = worldRot;
        }
        if (cameraTransform != null)
        {
            cameraTransform.localPosition = Vector3.zero;
            cameraTransform.localRotation = Quaternion.identity;
        }
    }

    private void GetScreenGeometry(out Vector3 worldCenter, out Vector3 normal, out Vector3 up, out Vector3 right, out float worldWidth, out float worldHeight)
    {
        Vector3 localCenter = Vector3.zero;
        Vector3 localSize = new Vector3(0.0176f, 0.0106f, 0.001f);

        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            localCenter = box.center;
            localSize = box.size;
        }
        else
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                localCenter = mf.sharedMesh.bounds.center;
                localSize = mf.sharedMesh.bounds.size;
            }
        }

        worldCenter = transform.TransformPoint(localCenter);

        Vector3 forward = transform.forward;
        if (cameraTransform != null)
        {
            Vector3 toCam = cameraTransform.position - worldCenter;
            if (Vector3.Dot(forward, toCam) < 0f)
            {
                forward = -forward;
            }
        }

        normal = forward.normalized;
        up = transform.up.normalized;
        if (Vector3.Dot(up, Vector3.up) < 0f)
        {
            up = -up;
        }
        right = Vector3.Cross(normal, up).normalized;

        worldWidth = transform.TransformVector(new Vector3(localSize.x, 0f, 0f)).magnitude;
        worldHeight = transform.TransformVector(new Vector3(0f, localSize.y, 0f)).magnitude;
    }

    private void GetScreenCameraTarget(out Vector3 targetPos, out Quaternion targetRot)
    {
        GetScreenGeometry(out Vector3 worldCenter, out Vector3 normal, out Vector3 up, out _, out float worldWidth, out float worldHeight);

        float fov = playerCamera != null ? playerCamera.fieldOfView : 80f;
        float aspect = playerCamera != null ? playerCamera.aspect : (16f / 9f);

        float halfVertRad = fov * 0.5f * Mathf.Deg2Rad;
        float distForHeight = (worldHeight * 0.5f) / Mathf.Tan(halfVertRad);

        float halfHorizTan = Mathf.Tan(halfVertRad) * aspect;
        float distForWidth = (worldWidth * 0.5f) / Mathf.Max(0.001f, halfHorizTan);

        float distance = Mathf.Max(distForHeight, distForWidth) * screenDistanceMultiplier;

        targetPos = worldCenter + normal * distance;
        targetRot = Quaternion.LookRotation(-normal, up);
    }

    private void SetupScreenToPanelSpace()
    {
        if (screenPanelRenderer == null || screenPanelRenderer.panelSettings == null) return;

        screenPanelRenderer.panelSettings.SetScreenToPanelSpaceFunction((Vector2 screenPosition) =>
        {
            if (!isFocused)
            {
                return new Vector2(float.NaN, float.NaN);
            }

            if (TryGetPanelPointFromScreen(screenPosition, false, out Vector2 panelPoint))
            {
                return panelPoint;
            }

            return new Vector2(float.NaN, float.NaN);
        });
    }

    private bool TryGetPanelPointFromScreen(Vector2 screenPos, bool isBottomLeftOrigin, out Vector2 panelPoint)
    {
        panelPoint = new Vector2(float.NaN, float.NaN);
        if (playerCamera == null)
        {
            if (cameraTransform != null) playerCamera = cameraTransform.GetComponent<Camera>();
            if (playerCamera == null) playerCamera = Camera.main;
            if (playerCamera == null) return false;
        }

        Vector3 rayScreenPos = isBottomLeftOrigin
            ? new Vector3(screenPos.x, screenPos.y, 0f)
            : new Vector3(screenPos.x, Screen.height - screenPos.y, 0f);

        Ray ray = playerCamera.ScreenPointToRay(rayScreenPos);

        GetScreenGeometry(out Vector3 worldCenter, out Vector3 normal, out Vector3 up, out Vector3 right, out float worldWidth, out float worldHeight);

        Plane screenPlane = new Plane(normal, worldCenter);
        if (!screenPlane.Raycast(ray, out float enter))
        {
            return false;
        }

        Vector3 hitPoint = ray.GetPoint(enter);
        Vector3 offset = hitPoint - worldCenter;

        float u = (Vector3.Dot(offset, right) / Mathf.Max(0.0001f, worldWidth)) + 0.5f;
        float vTopDown = 0.5f - (Vector3.Dot(offset, up) / Mathf.Max(0.0001f, worldHeight));

        if (u < 0f || u > 1f || vTopDown < 0f || vTopDown > 1f)
        {
            return false;
        }

        float panelWidth = 1200f;
        float panelHeight = 800f;

        if (rootElement != null && !float.IsNaN(rootElement.layout.width) && rootElement.layout.width > 1f)
        {
            panelWidth = rootElement.layout.width;
            panelHeight = rootElement.layout.height;
        }
        else if (screenPanelRenderer != null && screenPanelRenderer.panelSettings != null)
        {
            Vector2Int refRes = screenPanelRenderer.panelSettings.referenceResolution;
            if (refRes.x > 0 && refRes.y > 0)
            {
                panelWidth = refRes.x;
                panelHeight = refRes.y;
            }
        }

        panelPoint = new Vector2(u * panelWidth, vTopDown * panelHeight);
        return true;
    }
}
