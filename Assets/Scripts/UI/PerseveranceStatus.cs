using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(PanelRenderer))]
public class PerseveranceStatus : MonoBehaviour
{
    [Header("Battery")]
    [Range(0f, 100f)]
    [SerializeField] private float batteryPercent = 100f;
    [SerializeField] private float idleDrainAhPerSecond = 8f;
    [SerializeField] private float movingDrainAhPerSecond = 65f;

    [Header("Signal & Antenna Range")]
    [SerializeField] private Vector2 originXZ = Vector2.zero;
    [SerializeField] private float antennaRangeMultiplier = 1f;
    [Range(0.5f, 0.95f)]
    [SerializeField] private float lowSignalWarningThreshold = 0.75f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip cameraEndClip;

    private PanelRenderer panelRenderer;
    private int m_Version = 0;

    private Image batteryImage;
    private Label percentLabel;
    private VisualElement alertsElement;
    private VisualElement signalDistortionOverlay;
    private VisualElement lowSignalBanner;
    private Label lowSignalTitle;
    private Label lowSignalDetail;
    private VisualElement marsPauseMenu;
    private Button continueButton;
    private Button retryButton;
    private Button exitLobbyButton;
    private final_scanlines_holder scanlinesHolder;

    private VectorImage batteryCompleteSvg;
    private VectorImage batteryYellowSvg;
    private VectorImage batteryRedSvg;
    private VectorImage batteryVeryLowSvg;
    private VectorImage noBatterySvg;

    private float maxBatteryAh = 5000f;
    private float currentBatteryAh = 5000f;
    private float lastPercent = -1f;
    private bool wasAlarmActive = false;
    private bool isBatteryAlarmActive = false;
    private bool isSignalAlarmActive = false;
    private bool wasSignalWarningPlayed = false;
    private bool isPausedMenuOpen = false;

    private Transform roverTransform;
    private Rigidbody roverRigidbody;
    private RoverController roverControllerRef;
    private Camera roverCamera;
    private Volume signalVolume;
    private LensDistortion lensDistortion;
    private ChromaticAberration chromaticAberration;
    private FilmGrain filmGrain;
    private Vignette vignette;

    private sealed class final_scanlines_holder
    {
        public VisualElement[] bars;
    }

    public float BatteryPercent
    {
        get => batteryPercent;
        set => SetBatteryPercent(value);
    }

    private void Awake()
    {
        panelRenderer = GetComponent<PanelRenderer>();

        batteryCompleteSvg = Resources.Load<VectorImage>("battery");
        batteryYellowSvg = Resources.Load<VectorImage>("battery_medium");
        batteryRedSvg = Resources.Load<VectorImage>("battery_low");
        batteryVeryLowSvg = Resources.Load<VectorImage>("battery_very_low");
        noBatterySvg = Resources.Load<VectorImage>("no_battery");

        if (audioSource == null)
        {
            audioSource = GetComponentInParent<AudioSource>();
            if (audioSource == null && transform.root != null)
            {
                audioSource = transform.root.GetComponentInChildren<AudioSource>();
            }
        }

        if (cameraEndClip == null)
        {
            var roverController = GetComponentInParent<RoverController>();
            if (roverController != null)
            {
                cameraEndClip = roverController.endSound;
                roverTransform = roverController.transform;
            }
        }

        if (roverTransform == null)
        {
            var roverController = GetComponentInParent<RoverController>() ?? FindAnyObjectByType<RoverController>();
            if (roverController != null)
            {
                roverControllerRef = roverController;
                roverTransform = roverController.transform;
            }
            else if (transform.root != null)
            {
                roverTransform = transform.root;
            }
        }
        else if (roverControllerRef == null)
        {
            roverControllerRef = roverTransform.GetComponent<RoverController>();
        }

        if (roverTransform != null)
        {
            roverRigidbody = roverTransform.GetComponent<Rigidbody>();
        }

        InitializeBatteryFromStatic();
        SetupSignalPostProcessing();
    }

    private void InitializeBatteryFromStatic()
    {
        RoverBattery selectedBattery = RoverModuleSelection.SelectedBattery;
        if (selectedBattery == null && RoverModulesImplement.batteryList != null && RoverModulesImplement.batteryList.Count > 0)
        {
            selectedBattery = RoverModulesImplement.batteryList[0];
        }

        maxBatteryAh = selectedBattery != null && selectedBattery.ah > 0f ? selectedBattery.ah : 5000f;
        currentBatteryAh = maxBatteryAh * Mathf.Clamp01(batteryPercent / 100f);
        batteryPercent = (currentBatteryAh / maxBatteryAh) * 100f;
    }

    private void SetupSignalPostProcessing()
    {
        roverCamera = GetComponentInParent<Camera>();
        if (roverCamera == null) roverCamera = Camera.main;

        if (roverCamera != null)
        {
            var camData = roverCamera.GetComponent<UniversalAdditionalCameraData>();
            if (camData != null)
            {
                camData.renderPostProcessing = true;
            }
        }

        GameObject volumeObj = new GameObject("SignalDistortionVolume");
        volumeObj.transform.SetParent(transform, false);
        signalVolume = volumeObj.AddComponent<Volume>();
        signalVolume.isGlobal = true;
        signalVolume.priority = 50f;
        signalVolume.weight = 1f;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        signalVolume.sharedProfile = profile;

        lensDistortion = profile.Add<LensDistortion>(true);
        lensDistortion.intensity.Override(0f);
        lensDistortion.scale.Override(1f);

        chromaticAberration = profile.Add<ChromaticAberration>(true);
        chromaticAberration.intensity.Override(0f);

        filmGrain = profile.Add<FilmGrain>(true);
        filmGrain.type.Override(FilmGrainLookup.Large01);
        filmGrain.intensity.Override(0f);
        filmGrain.response.Override(0.8f);

        vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0f);
        vignette.smoothness.Override(0.45f);
    }

    private void OnEnable()
    {
        if (panelRenderer == null) panelRenderer = GetComponent<PanelRenderer>();
        panelRenderer.RegisterUIReloadCallback(OnUIReload);
    }

    private void OnDisable()
    {
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
        UnbindPauseButtons();
        if (panelRenderer != null)
        {
            panelRenderer.UnregisterUIReloadCallback(OnUIReload);
        }
    }

    private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
    {
        if (version == m_Version) return;
        m_Version = version;

        UnbindPauseButtons();

        batteryImage = root.Q<Image>(className: "image-battery");
        percentLabel = root.Q<Label>(className: "percent-battery");
        alertsElement = root.Q<VisualElement>(className: "alerts");
        signalDistortionOverlay = root.Q<VisualElement>("signalDistortionOverlay");
        lowSignalBanner = root.Q<VisualElement>("lowSignalBanner");
        lowSignalTitle = root.Q<Label>("lowSignalTitle");
        lowSignalDetail = root.Q<Label>("lowSignalDetail");
        marsPauseMenu = root.Q<VisualElement>("marsPauseMenu");
        continueButton = root.Q<Button>("continueButton");
        retryButton = root.Q<Button>("retryButton");
        exitLobbyButton = root.Q<Button>("exitLobbyButton");

        EnsureSignalUIElements(root);
        EnsurePauseMenuUIElements(root);
        BindPauseButtons();
        UpdatePauseMenuState();

        lastPercent = -1f;
        UpdateBatteryUI();
        UpdateSignalAndDistortion();
    }

    private void EnsureSignalUIElements(VisualElement root)
    {
        VisualElement host = root.Q<VisualElement>(className: "frame-minor") ?? root;

        if (signalDistortionOverlay == null)
        {
            signalDistortionOverlay = new VisualElement { name = "signalDistortionOverlay", pickingMode = PickingMode.Ignore };
            signalDistortionOverlay.AddToClassList("signal-distortion-overlay");
            signalDistortionOverlay.style.position = Position.Absolute;
            signalDistortionOverlay.style.left = 0;
            signalDistortionOverlay.style.right = 0;
            signalDistortionOverlay.style.top = 0;
            signalDistortionOverlay.style.bottom = 0;
            host.Add(signalDistortionOverlay);
        }

        signalDistortionOverlay.Clear();
        scanlinesHolder = new final_scanlines_holder { bars = new VisualElement[6] };
        for (int i = 0; i < scanlinesHolder.bars.Length; i++)
        {
            var bar = new VisualElement { pickingMode = PickingMode.Ignore };
            bar.style.position = Position.Absolute;
            bar.style.left = 0;
            bar.style.right = 0;
            bar.style.height = Length.Percent(4f);
            bar.style.top = Length.Percent(i * 16f);
            bar.style.backgroundColor = new Color(0.6f, 0.9f, 1f, 0f);
            signalDistortionOverlay.Add(bar);
            scanlinesHolder.bars[i] = bar;
        }

        if (lowSignalBanner == null)
        {
            lowSignalBanner = new VisualElement { name = "lowSignalBanner", pickingMode = PickingMode.Ignore };
            lowSignalBanner.AddToClassList("low-signal-banner");
            lowSignalBanner.AddToClassList("alerts-hidden");
            lowSignalBanner.style.position = Position.Absolute;
            lowSignalBanner.style.top = 40;
            lowSignalBanner.style.alignSelf = Align.Center;
            lowSignalBanner.style.paddingTop = 14;
            lowSignalBanner.style.paddingBottom = 14;
            lowSignalBanner.style.paddingLeft = 32;
            lowSignalBanner.style.paddingRight = 32;
            lowSignalBanner.style.backgroundColor = new Color(0.14f, 0.04f, 0.04f, 0.86f);
            lowSignalBanner.style.borderTopWidth = 3;
            lowSignalBanner.style.borderBottomWidth = 3;
            lowSignalBanner.style.borderLeftWidth = 3;
            lowSignalBanner.style.borderRightWidth = 3;
            Color warnRed = new Color(1f, 0.25f, 0.18f, 1f);
            lowSignalBanner.style.borderTopColor = warnRed;
            lowSignalBanner.style.borderBottomColor = warnRed;
            lowSignalBanner.style.borderLeftColor = warnRed;
            lowSignalBanner.style.borderRightColor = warnRed;
            lowSignalBanner.style.borderTopLeftRadius = 12;
            lowSignalBanner.style.borderTopRightRadius = 12;
            lowSignalBanner.style.borderBottomLeftRadius = 12;
            lowSignalBanner.style.borderBottomRightRadius = 12;
            lowSignalBanner.style.alignItems = Align.Center;

            lowSignalTitle = new Label("WARNING: LOW SIGNAL") { name = "lowSignalTitle", pickingMode = PickingMode.Ignore };
            lowSignalTitle.style.fontSize = 38;
            lowSignalTitle.style.color = warnRed;
            lowSignalTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            lowSignalTitle.style.unityTextAlign = TextAnchor.MiddleCenter;

            lowSignalDetail = new Label("Approaching the antenna's range limit") { name = "lowSignalDetail", pickingMode = PickingMode.Ignore };
            lowSignalDetail.style.fontSize = 24;
            lowSignalDetail.style.color = new Color(1f, 0.88f, 0.86f, 1f);
            lowSignalDetail.style.unityTextAlign = TextAnchor.MiddleCenter;

            lowSignalBanner.Add(lowSignalTitle);
            lowSignalBanner.Add(lowSignalDetail);
            host.Add(lowSignalBanner);
        }
    }

    private void EnsurePauseMenuUIElements(VisualElement root)
    {
        if (marsPauseMenu != null) return;

        VisualElement frameRoot = root.Q<VisualElement>(className: "frame") ?? root;

        marsPauseMenu = new VisualElement { name = "marsPauseMenu" };
        marsPauseMenu.AddToClassList("pause-container");
        marsPauseMenu.AddToClassList("alerts-hidden");
        marsPauseMenu.style.position = Position.Absolute;
        marsPauseMenu.style.left = 0;
        marsPauseMenu.style.top = 0;
        marsPauseMenu.style.right = 0;
        marsPauseMenu.style.bottom = 0;
        marsPauseMenu.style.flexDirection = FlexDirection.Row;

        var leftPanel = new VisualElement { name = "PauseMenuLeft" };
        leftPanel.AddToClassList("pause-left");
        leftPanel.style.minWidth = 800;
        leftPanel.style.maxWidth = 700;
        leftPanel.style.height = Length.Percent(100);
        leftPanel.style.backgroundColor = new Color(28f / 255f, 28f / 255f, 28f / 255f, 1f);
        leftPanel.style.justifyContent = Justify.SpaceBetween;
        leftPanel.style.borderRightWidth = 15;
        leftPanel.style.borderRightColor = new Color(55f / 255f, 200f / 255f, 171f / 255f, 1f);

        var title = new Label("Nameless Space Game") { name = "pauseTitle" };
        title.AddToClassList("pause-title");
        title.style.fontSize = 140;
        title.style.color = Color.white;
        title.style.whiteSpace = WhiteSpace.Normal;
        title.style.marginTop = 25;
        title.style.marginLeft = 25;
        title.style.marginRight = 25;
        title.style.marginBottom = 25;

        var buttonsContainer = new VisualElement { name = "pauseButtons" };
        continueButton = CreatePauseButton("continueButton", "CONTINUE");
        retryButton = CreatePauseButton("retryButton", "RETRY");
        exitLobbyButton = CreatePauseButton("exitLobbyButton", "EXIT TO LOBBY");

        buttonsContainer.Add(continueButton);
        buttonsContainer.Add(retryButton);
        buttonsContainer.Add(exitLobbyButton);

        var footer = new VisualElement { name = "pauseFooter" };
        leftPanel.Add(title);
        leftPanel.Add(buttonsContainer);
        leftPanel.Add(footer);

        var rightPanel = new VisualElement { name = "PauseMenuRight" };
        rightPanel.AddToClassList("pause-right");
        rightPanel.style.flexGrow = 1;
        rightPanel.style.height = Length.Percent(100);
        rightPanel.style.backgroundColor = new Color(9f / 255f, 10f / 255f, 20f / 255f, 0.522f);

        marsPauseMenu.Add(leftPanel);
        marsPauseMenu.Add(rightPanel);
        frameRoot.Add(marsPauseMenu);
    }

    private Button CreatePauseButton(string btnName, string text)
    {
        var btn = new Button { name = btnName, text = text };
        btn.AddToClassList("pause-style-button");
        btn.style.backgroundColor = new Color(203f / 255f, 203f / 255f, 203f / 255f, 1f);
        btn.style.fontSize = 72;
        btn.style.marginTop = 25;
        btn.style.marginBottom = 25;
        btn.style.marginLeft = 25;
        btn.style.marginRight = 25;
        btn.style.paddingTop = 10;
        btn.style.paddingBottom = 10;
        btn.style.paddingLeft = 10;
        btn.style.paddingRight = 10;
        btn.style.borderTopLeftRadius = 15;
        btn.style.borderTopRightRadius = 15;
        btn.style.borderBottomLeftRadius = 15;
        btn.style.borderBottomRightRadius = 15;
        return btn;
    }

    private void BindPauseButtons()
    {
        if (continueButton != null) continueButton.clicked += OnContinueClicked;
        if (retryButton != null) retryButton.clicked += OnRetryOrMissionClicked;
        if (exitLobbyButton != null) exitLobbyButton.clicked += OnExitToLobbyClicked;
    }

    private void UnbindPauseButtons()
    {
        if (continueButton != null) continueButton.clicked -= OnContinueClicked;
        if (retryButton != null) retryButton.clicked -= OnRetryOrMissionClicked;
        if (exitLobbyButton != null) exitLobbyButton.clicked -= OnExitToLobbyClicked;
    }

    public void TogglePauseMenu()
    {
        SetPauseMenuOpen(!isPausedMenuOpen);
    }

    public void SetPauseMenuOpen(bool open)
    {
        isPausedMenuOpen = open;
        Time.timeScale = isPausedMenuOpen ? 0f : 1f;

        if (isPausedMenuOpen)
        {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }
        else
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }

        UpdatePauseMenuState();
    }

    private void UpdatePauseMenuState()
    {
        if (marsPauseMenu == null) return;

        if (retryButton != null)
        {
            bool inMission = MissionsImplement.ActiveMission != null;
            retryButton.text = inMission ? "RETRY" : "MISSION";
        }

        if (isPausedMenuOpen)
        {
            marsPauseMenu.RemoveFromClassList("alerts-hidden");
            marsPauseMenu.BringToFront();
        }
        else
        {
            marsPauseMenu.AddToClassList("alerts-hidden");
        }
    }

    private void OnContinueClicked()
    {
        SetPauseMenuOpen(false);
    }

    private void OnRetryOrMissionClicked()
    {
        Time.timeScale = 1f;
        isPausedMenuOpen = false;

        if (MissionsImplement.ActiveMission == null &&
            MissionsImplement.GetMissions != null &&
            MissionsImplement.GetMissions.Count > 0)
        {
            MissionsImplement.ActiveMission = MissionsImplement.GetMissions[0];
        }

        LoadingManegment.TargetScene = "MarsScene";
        SceneManager.LoadScene("LoadingScene");
    }

    private void OnExitToLobbyClicked()
    {
        Time.timeScale = 1f;
        isPausedMenuOpen = false;
        MissionsImplement.ActiveMission = null;

        LoadingManegment.TargetScene = "LobbyScene";
        SceneManager.LoadScene("LoadingScene");
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            TogglePauseMenu();
        }

        if (isPausedMenuOpen) return;

        UpdateBatteryConsumption(kb);

        if (!Mathf.Approximately(batteryPercent, lastPercent))
        {
            UpdateBatteryUI();
        }

        UpdateSignalAndDistortion();
    }

    private void UpdateBatteryConsumption(Keyboard kb)
    {
        if (maxBatteryAh <= 0f)
        {
            InitializeBatteryFromStatic();
        }

        bool isDrivingOrRotatingCam = false;
        if (kb != null)
        {
            isDrivingOrRotatingCam = kb.wKey.isPressed || kb.sKey.isPressed ||
                                     kb.aKey.isPressed || kb.dKey.isPressed ||
                                     kb.qKey.isPressed || kb.eKey.isPressed ||
                                     kb.leftCtrlKey.isPressed || kb.leftShiftKey.isPressed;
        }

        if (!isDrivingOrRotatingCam && roverRigidbody != null && roverRigidbody.linearVelocity.magnitude > 0.2f)
        {
            isDrivingOrRotatingCam = true;
        }

        float drainAh = (isDrivingOrRotatingCam ? movingDrainAhPerSecond : idleDrainAhPerSecond) * Time.deltaTime;
        currentBatteryAh = Mathf.Clamp(currentBatteryAh - drainAh, 0f, maxBatteryAh);
        batteryPercent = (currentBatteryAh / maxBatteryAh) * 100f;
    }

    public void SetBatteryPercent(float percent)
    {
        batteryPercent = Mathf.Clamp(percent, 0f, 100f);
        if (maxBatteryAh > 0f)
        {
            currentBatteryAh = maxBatteryAh * (batteryPercent / 100f);
        }
        UpdateBatteryUI();
    }

    private float GetMaxAntennaRange()
    {
        RoverAntenna antenna = RoverModuleSelection.SelectedAntenna;
        if (antenna == null && RoverModulesImplement.antennaList != null && RoverModulesImplement.antennaList.Count > 0)
        {
            antenna = RoverModulesImplement.antennaList[0];
        }

        float baseRange = antenna != null ? antenna.radius : 5f;
        return Mathf.Max(0.1f, baseRange * Mathf.Max(0.1f, antennaRangeMultiplier));
    }

    private void UpdateSignalAndDistortion()
    {
        Vector3 worldPos = roverTransform != null ? roverTransform.position : transform.position;
        float distanceFromOrigin = Vector2.Distance(new Vector2(worldPos.x, worldPos.z), originXZ);
        float maxRange = GetMaxAntennaRange();
        float normDist = Mathf.Clamp01(distanceFromOrigin / maxRange);

        // Progressive camera distortion as rover moves away from (0,0)
        float distortionCurve = normDist * normDist;
        float wobble = Mathf.Sin(Time.time * (4f + normDist * 12f)) * 0.14f * distortionCurve;

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.value = Mathf.Clamp01(normDist * 1.15f);
        }

        if (lensDistortion != null)
        {
            lensDistortion.intensity.value = Mathf.Clamp(-0.62f * distortionCurve + wobble, -0.85f, 0.85f);
            lensDistortion.scale.value = Mathf.Lerp(1f, 0.92f, distortionCurve);
        }

        if (filmGrain != null)
        {
            filmGrain.intensity.value = Mathf.Clamp01(normDist * 1.1f);
        }

        if (vignette != null)
        {
            vignette.intensity.value = Mathf.Lerp(0f, 0.55f, distortionCurve);
        }

        if (signalDistortionOverlay != null)
        {
            float flicker = normDist > 0.15f
                ? (Mathf.PerlinNoise(Time.time * 18f, 0f) * 0.18f * distortionCurve)
                : 0f;
            signalDistortionOverlay.style.backgroundColor = new Color(0.08f, 0.12f, 0.16f, Mathf.Clamp(distortionCurve * 0.42f + flicker, 0f, 0.72f));

            if (scanlinesHolder?.bars != null)
            {
                for (int i = 0; i < scanlinesHolder.bars.Length; i++)
                {
                    VisualElement bar = scanlinesHolder.bars[i];
                    if (bar == null) continue;
                    float yPct = ((Time.time * (18f + i * 7f) + i * 19f) % 100f);
                    bar.style.top = Length.Percent(yPct);
                    float barAlpha = normDist > 0.2f
                        ? Mathf.Clamp01((normDist - 0.2f) / 0.8f) * 0.22f * Mathf.PerlinNoise(Time.time * 25f, i * 3.1f)
                        : 0f;
                    bar.style.backgroundColor = new Color(0.55f, 0.9f, 1f, barAlpha);
                }
            }
        }

        // Low signal warning when approaching antenna limit
        isSignalAlarmActive = normDist >= lowSignalWarningThreshold;

        if (lowSignalBanner != null)
        {
            if (isSignalAlarmActive)
            {
                lowSignalBanner.RemoveFromClassList("alerts-hidden");
                int signalPct = Mathf.Clamp(Mathf.RoundToInt((1f - normDist) * 100f), 0, 100);
                float pulse = 0.65f + 0.35f * Mathf.Sin(Time.time * 8f);
                lowSignalBanner.style.opacity = pulse;

                if (lowSignalTitle != null)
                {
                    lowSignalTitle.text = normDist >= 0.98f
                        ? "CRITICAL SIGNAL - RANGE LIMIT REACHED"
                        : "WARNING: LOW SIGNAL";
                }
                if (lowSignalDetail != null)
                {
                    lowSignalDetail.text = $"Signal: {signalPct}%  |  Distance: {distanceFromOrigin:0}m / {maxRange:0}m";
                }
            }
            else
            {
                lowSignalBanner.AddToClassList("alerts-hidden");
            }
        }

        if (isSignalAlarmActive && !wasSignalWarningPlayed)
        {
            if (audioSource != null && cameraEndClip != null)
            {
                audioSource.PlayOneShot(cameraEndClip);
            }
        }
        wasSignalWarningPlayed = isSignalAlarmActive;

        UpdateCombinedAlertIcon();
    }

    private void UpdateCombinedAlertIcon()
    {
        if (alertsElement == null) return;

        if (isBatteryAlarmActive || isSignalAlarmActive)
        {
            alertsElement.RemoveFromClassList("alerts-hidden");
        }
        else
        {
            alertsElement.AddToClassList("alerts-hidden");
        }
    }

    private void UpdateBatteryUI()
    {
        batteryPercent = Mathf.Clamp(batteryPercent, 0f, 100f);
        lastPercent = batteryPercent;

        int roundedPercent = Mathf.RoundToInt(batteryPercent);

        if (percentLabel != null)
        {
            percentLabel.text = $"{roundedPercent}%";
        }

        VectorImage targetSvg;
        bool isAlarmActive;

        if (roundedPercent >= 90)
        {
            targetSvg = batteryCompleteSvg;
            isAlarmActive = false;
        }
        else if (roundedPercent >= 50)
        {
            targetSvg = batteryYellowSvg;
            isAlarmActive = false;
        }
        else if (roundedPercent >= 16)
        {
            targetSvg = batteryRedSvg;
            isAlarmActive = true;
        }
        else if (roundedPercent >= 1)
        {
            targetSvg = batteryVeryLowSvg;
            isAlarmActive = true;
        }
        else
        {
            targetSvg = noBatterySvg;
            isAlarmActive = true;
        }

        if (batteryImage != null && targetSvg != null)
        {
            batteryImage.vectorImage = targetSvg;
        }

        isBatteryAlarmActive = isAlarmActive;
        UpdateCombinedAlertIcon();

        if (isAlarmActive && !wasAlarmActive)
        {
            if (audioSource != null && cameraEndClip != null)
            {
                audioSource.PlayOneShot(cameraEndClip);
            }
        }

        wasAlarmActive = isAlarmActive;
    }
}
