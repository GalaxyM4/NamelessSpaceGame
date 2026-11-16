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
    private bool isConfiguringModules;
    private VisualElement rootElement;
    private Label screenTitleLabel;
    private VisualElement missionOneCard;
    private VisualElement informationCard;
    private VisualElement dashboardRightPanel;
    private Button missionImageButton;
    private Label missionTitleLabel;
    private Label missionInfoLabel;
    private Label urlInfoLabel;
    private Button startMissionButton;

    private ScrollView equipmentScrollView;
    private Label resourcesTitleLabel;
    private Label selectedModuleLabel;
    private Label statLabel;
    private Label detailLabel;
    private Label weightLabel;
    private Label priceLabel;
    private ProgressBar capacityProgress;
    private Button saveButton;
    private Button launchButton;
    private Button exitButton;

    private GenericModule currentSelectedModule;
    private readonly Dictionary<string, Button> selectedCardsByCategory = new Dictionary<string, Button>();
    private VisualElement currentHoveredElement;
    private readonly List<Button> moduleCardButtons = new List<Button>();
    private bool isDraggingScroll;
    private float lastScrollDragY;
    private float scrollDragDistance;
    private int lastClickFrame = -1;

    private sealed class ModuleCardData
    {
        public string Name;
        public string Category;
        public string StatTitle;
        public string DetailTitle;
        public string Stats;
        public string Price;
        public float ProgressValue;
        public float ProgressMax;
        public GenericModule Module;
    }

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
        isConfiguringModules = false;
        screenTitleLabel = root.Q<Label>("screenTitle");
        missionOneCard = root.Q<VisualElement>("misionOne");
        informationCard = root.Q<VisualElement>("information");
        dashboardRightPanel = root.Q<VisualElement>("dashboardRight");

        if (missionOneCard != null)
        {
            missionOneCard.SetEnabled(true);
            missionOneCard.RemoveFromClassList("panel-hidden");
        }
        if (informationCard != null)
        {
            informationCard.RemoveFromClassList("panel-hidden");
        }
        if (dashboardRightPanel != null)
        {
            dashboardRightPanel.AddToClassList("panel-hidden");
        }

        missionImageButton = root.Q<Button>("MissionButtonImage");
        missionTitleLabel = root.Q<Label>("MissionTitle");
        missionInfoLabel = root.Q<Label>(className: "info-mision");
        urlInfoLabel = root.Q<Label>("urlInfo");
        startMissionButton = root.Q<Button>("Button");

        equipmentScrollView = root.Q<ScrollView>("equipmentScrollView");
        resourcesTitleLabel = root.Q<Label>("resourcesTitle");
        selectedModuleLabel = root.Q<Label>("selected");
        statLabel = root.Q<Label>("statLabel");
        detailLabel = root.Q<Label>("detailLabel");
        weightLabel = root.Q<Label>("weight");
        priceLabel = root.Q<Label>("price");
        capacityProgress = root.Q<ProgressBar>("capacity");
        saveButton = root.Q<Button>("save");
        launchButton = root.Q<Button>("launchButton");
        exitButton = root.Q<Button>("exitButton");

        if (startMissionButton != null)
        {
            startMissionButton.SetEnabled(true);
            startMissionButton.RegisterCallback<ClickEvent>(OnStartMissionClicked);
        }

        if (saveButton != null)
        {
            saveButton.RegisterCallback<ClickEvent>(OnSaveModuleClicked);
        }

        if (launchButton != null)
        {
            launchButton.RegisterCallback<ClickEvent>(OnLaunchMarsClicked);
            UpdateLaunchButtonVisibility();
        }

        if (exitButton != null)
        {
            exitButton.RegisterCallback<ClickEvent>(OnExitDashboardClicked);
        }

        SetupEquipmentGrid();
        UpdateMissionUI();
    }

    private void UnbindButton()
    {
        if (startMissionButton != null)
        {
            startMissionButton.UnregisterCallback<ClickEvent>(OnStartMissionClicked);
        }
        if (saveButton != null)
        {
            saveButton.UnregisterCallback<ClickEvent>(OnSaveModuleClicked);
        }
        if (launchButton != null)
        {
            launchButton.UnregisterCallback<ClickEvent>(OnLaunchMarsClicked);
        }
        if (exitButton != null)
        {
            exitButton.UnregisterCallback<ClickEvent>(OnExitDashboardClicked);
        }
        moduleCardButtons.Clear();
        selectedCardsByCategory.Clear();
        currentHoveredElement = null;
        isDraggingScroll = false;
    }

    private void OnExitDashboardClicked(ClickEvent evt)
    {
        ExitScreenFocus();
    }

    private void OnStartMissionClicked(ClickEvent evt)
    {
        OpenModuleDashboard();
    }

    private void OpenModuleDashboard()
    {
        if (Time.frameCount == lastClickFrame) return;
        lastClickFrame = Time.frameCount;

        isConfiguringModules = true;
        SetHoveredElement(null);

        if (missionOneCard != null) missionOneCard.AddToClassList("panel-hidden");
        if (informationCard != null) informationCard.AddToClassList("panel-hidden");
        if (dashboardRightPanel != null) dashboardRightPanel.RemoveFromClassList("panel-hidden");
        if (screenTitleLabel != null) screenTitleLabel.text = "CONFIGURA TU ROVER";
    }

    private void OnSaveModuleClicked(ClickEvent evt)
    {
        ConfirmCurrentModule();
    }

    private void OnLaunchMarsClicked(ClickEvent evt)
    {
        LaunchMarsScene();
    }

    private void ConfirmCurrentModule()
    {
        RoverArmor armor = null;
        RoverBattery battery = null;
        RoverAntenna antenna = null;

        foreach (Button card in selectedCardsByCategory.Values)
        {
            if (card?.userData is ModuleCardData data)
            {
                if (data.Module is RoverArmor a) armor = a;
                else if (data.Module is RoverBattery b) battery = b;
                else if (data.Module is RoverAntenna ant) antenna = ant;
            }
        }

        if (armor == null && currentSelectedModule is RoverArmor fallbackArmor) armor = fallbackArmor;
        if (battery == null && currentSelectedModule is RoverBattery fallbackBattery) battery = fallbackBattery;
        if (antenna == null && currentSelectedModule is RoverAntenna fallbackAntenna) antenna = fallbackAntenna;

        if (armor == null && battery == null && antenna == null) return;

        RoverModuleSelection.SaveSelection(armor, battery, antenna);
        if (saveButton != null)
        {
            saveButton.text = "Guardado";
        }
        UpdateLaunchButtonVisibility();
    }

    private void UpdateLaunchButtonVisibility()
    {
        if (launchButton == null) return;

        if (RoverModuleSelection.IsConfigurationComplete)
        {
            launchButton.RemoveFromClassList("panel-hidden");
        }
        else
        {
            launchButton.AddToClassList("panel-hidden");
        }
    }

    private void LaunchMarsScene()
    {
        if (!RoverModuleSelection.IsConfigurationComplete) return;

        LoadingManegment.TargetScene = "MarsScene";
        SceneManager.LoadScene("LoadingScene");
    }

    private void SetupEquipmentGrid()
    {
        if (equipmentScrollView == null) return;

        equipmentScrollView.mode = ScrollViewMode.Vertical;
        equipmentScrollView.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
        equipmentScrollView.Clear();
        moduleCardButtons.Clear();
        selectedCardsByCategory.Clear();

        AddCategorySection("Armor", BuildArmorCards());
        AddCategorySection("Battery", BuildBatteryCards());
        AddCategorySection("Antena", BuildAntennaCards());
    }

    private void AddCategorySection(string title, List<ModuleCardData> items)
    {
        var header = new Label(title);
        header.AddToClassList("section-header");
        equipmentScrollView.Add(header);

        var grid = new VisualElement();
        grid.AddToClassList("module-grid");

        foreach (ModuleCardData data in items)
        {
            var cardButton = new Button();
            cardButton.AddToClassList("grid-card");
            cardButton.userData = data;

            var frame = new VisualElement();
            frame.AddToClassList("grid-card-frame");
            frame.pickingMode = PickingMode.Ignore;

            var icon = new VisualElement();
            icon.AddToClassList("grid-card-icon");
            icon.pickingMode = PickingMode.Ignore;
            if (data.Module != null && !string.IsNullOrEmpty(data.Module.spriteClass))
            {
                icon.AddToClassList(data.Module.spriteClass);
            }
            frame.Add(icon);

            var nameLabel = new Label(data.Name);
            nameLabel.AddToClassList("grid-card-label");
            nameLabel.pickingMode = PickingMode.Ignore;

            cardButton.Add(frame);
            cardButton.Add(nameLabel);
            cardButton.clicked += () => SelectModuleCard(cardButton, data);

            moduleCardButtons.Add(cardButton);
            grid.Add(cardButton);
        }

        equipmentScrollView.Add(grid);
    }

    private List<ModuleCardData> BuildArmorCards()
    {
        var list = new List<ModuleCardData>();
        foreach (RoverArmor armor in RoverModulesImplement.armorList)
        {
            list.Add(new ModuleCardData
            {
                Name = armor.name,
                Category = "Armor",
                StatTitle = "Resistencia:",
                DetailTitle = "Golpes:",
                Stats = $"{armor.bangs} golpes",
                Price = $"{armor.price}$",
                ProgressValue = armor.bangs,
                ProgressMax = 20f,
                Module = armor
            });
        }
        return list;
    }

    private List<ModuleCardData> BuildBatteryCards()
    {
        var list = new List<ModuleCardData>();
        foreach (RoverBattery battery in RoverModulesImplement.batteryList)
        {
            list.Add(new ModuleCardData
            {
                Name = battery.name,
                Category = "Battery",
                StatTitle = "Capacidad:",
                DetailTitle = "Carga:",
                Stats = $"{battery.ah / 1000f:0.#}kAh",
                Price = $"{battery.price}$",
                ProgressValue = battery.ah / 1000f,
                ProgressMax = 30f,
                Module = battery
            });
        }
        return list;
    }

    private List<ModuleCardData> BuildAntennaCards()
    {
        var list = new List<ModuleCardData>();
        foreach (RoverAntenna antenna in RoverModulesImplement.antennaList)
        {
            list.Add(new ModuleCardData
            {
                Name = antenna.name,
                Category = "Antena",
                StatTitle = "Alcance:",
                DetailTitle = "Señal:",
                Stats = $"{antenna.radius}cm / {antenna.speed}Hz",
                Price = "N/D",
                ProgressValue = antenna.radius,
                ProgressMax = 100f,
                Module = antenna
            });
        }
        return list;
    }

    private void SelectModuleCard(Button cardButton, ModuleCardData data)
    {
        if (data == null || data.Module == null) return;

        if (selectedCardsByCategory.TryGetValue(data.Category, out Button prevCard) && prevCard != null)
        {
            prevCard.RemoveFromClassList("is-selected");
        }
        selectedCardsByCategory[data.Category] = cardButton;
        cardButton?.AddToClassList("is-selected");

        currentSelectedModule = data.Module;
        if (saveButton != null)
        {
            saveButton.text = "Seleccionar";
        }

        if (resourcesTitleLabel != null) resourcesTitleLabel.text = data.Category.ToUpperInvariant();
        if (selectedModuleLabel != null) selectedModuleLabel.text = data.Name;
        if (statLabel != null) statLabel.text = data.StatTitle;
        if (detailLabel != null) detailLabel.text = data.DetailTitle;
        if (weightLabel != null) weightLabel.text = data.Stats;
        if (priceLabel != null) priceLabel.text = data.Price;

        if (capacityProgress != null)
        {
            capacityProgress.highValue = data.ProgressMax;
            capacityProgress.value = data.ProgressValue;
            capacityProgress.title = data.Stats;
        }
    }

    private void SetHoveredElement(VisualElement element)
    {
        if (currentHoveredElement == element) return;

        if (currentHoveredElement != null)
        {
            currentHoveredElement.RemoveFromClassList("is-hovered");
        }
        currentHoveredElement = element;
        if (currentHoveredElement != null)
        {
            currentHoveredElement.AddToClassList("is-hovered");
        }
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
        if (mouse != null)
        {
            Vector2 mousePos = mouse.position.ReadValue();
            if (TryGetPanelPointFromScreen(mousePos, true, out Vector2 panelPoint))
            {
                UpdateMouseHover(panelPoint);

                if (mouse.leftButton.wasPressedThisFrame)
                {
                    HandlePanelClick(panelPoint);
                }

                if (isConfiguringModules && equipmentScrollView != null)
                {
                    float scrollY = mouse.scroll.ReadValue().y;
                    if (!Mathf.Approximately(scrollY, 0f) && equipmentScrollView.worldBound.Contains(panelPoint))
                    {
                        float step = Mathf.Abs(scrollY) > 10f ? scrollY * 0.5f : scrollY * 50f;
                        ScrollEquipmentBy(-step);
                    }

                    if (mouse.rightButton.wasPressedThisFrame && equipmentScrollView.worldBound.Contains(panelPoint))
                    {
                        isDraggingScroll = true;
                        lastScrollDragY = panelPoint.y;
                        scrollDragDistance = 0f;
                    }
                    else if (isDraggingScroll && mouse.rightButton.isPressed)
                    {
                        float deltaY = panelPoint.y - lastScrollDragY;
                        lastScrollDragY = panelPoint.y;
                        scrollDragDistance += Mathf.Abs(deltaY);
                        if (Mathf.Abs(deltaY) > 0.01f)
                        {
                            ScrollEquipmentBy(-deltaY);
                        }
                    }
                    else if (mouse.rightButton.wasReleasedThisFrame)
                    {
                        isDraggingScroll = false;
                    }
                }
            }
            else
            {
                isDraggingScroll = false;
                SetHoveredElement(null);
            }
        }
    }

    private void ScrollEquipmentBy(float deltaY)
    {
        if (equipmentScrollView == null) return;

        float contentHeight = equipmentScrollView.contentContainer.layout.height;
        float viewportHeight = equipmentScrollView.contentViewport.layout.height;
        float maxScroll = Mathf.Max(0f, contentHeight - viewportHeight);

        Vector2 offset = equipmentScrollView.scrollOffset;
        offset.y = Mathf.Clamp(offset.y + deltaY, 0f, maxScroll);
        equipmentScrollView.scrollOffset = offset;
    }

    private void UpdateMouseHover(Vector2 panelPoint)
    {
        if (!isConfiguringModules)
        {
            if (startMissionButton != null && startMissionButton.worldBound.Contains(panelPoint))
            {
                SetHoveredElement(startMissionButton);
            }
            else
            {
                SetHoveredElement(null);
            }
            return;
        }

        if (exitButton != null && exitButton.worldBound.Contains(panelPoint))
        {
            SetHoveredElement(exitButton);
            return;
        }

        if (saveButton != null && saveButton.worldBound.Contains(panelPoint))
        {
            SetHoveredElement(saveButton);
            return;
        }

        if (launchButton != null && !launchButton.ClassListContains("panel-hidden") && launchButton.worldBound.Contains(panelPoint))
        {
            SetHoveredElement(launchButton);
            return;
        }

        Button hoveredCard = FindModuleCardAtPoint(panelPoint);
        if (hoveredCard != null)
        {
            SetHoveredElement(hoveredCard);
            return;
        }

        SetHoveredElement(null);
    }

    private Button FindModuleCardAtPoint(Vector2 panelPoint)
    {
        if (equipmentScrollView != null && !equipmentScrollView.worldBound.Contains(panelPoint))
        {
            return null;
        }

        if (rootElement != null && rootElement.panel != null)
        {
            VisualElement picked = rootElement.panel.Pick(panelPoint);
            for (VisualElement el = picked; el != null; el = el.parent)
            {
                if (el is Button btn && btn.userData is ModuleCardData)
                {
                    return btn;
                }
            }
        }

        foreach (Button card in moduleCardButtons)
        {
            if (IsPointOverCard(card, panelPoint))
            {
                return card;
            }
        }

        return null;
    }

    private static bool IsPointOverCard(Button card, Vector2 panelPoint)
    {
        if (card == null) return false;
        if (card.worldBound.Contains(panelPoint)) return true;

        VisualElement icon = card.Q<VisualElement>(className: "grid-card-icon");
        return icon != null && icon.worldBound.Contains(panelPoint);
    }

    private void HandlePanelClick(Vector2 panelPoint)
    {
        if (!isConfiguringModules)
        {
            if (startMissionButton != null && startMissionButton.worldBound.Contains(panelPoint))
            {
                OpenModuleDashboard();
            }
            return;
        }

        if (exitButton != null && exitButton.worldBound.Contains(panelPoint))
        {
            ExitScreenFocus();
            return;
        }

        if (saveButton != null && saveButton.worldBound.Contains(panelPoint))
        {
            ConfirmCurrentModule();
            return;
        }

        if (launchButton != null && !launchButton.ClassListContains("panel-hidden") && launchButton.worldBound.Contains(panelPoint))
        {
            LaunchMarsScene();
            return;
        }

        Button clickedCard = FindModuleCardAtPoint(panelPoint);
        if (clickedCard != null && clickedCard.userData is ModuleCardData data)
        {
            SelectModuleCard(clickedCard, data);
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
