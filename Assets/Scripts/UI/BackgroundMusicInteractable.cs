using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class BackgroundMusicInteractable : MonoBehaviour, IInteractable
{
    [Header("Interacción y Cámara")]
    public string promptText = "Panel de Música";
    public float transitionDuration = 0.4f;
    public float screenDistanceMultiplier = 1.25f;

    [Header("Audio de Fondo")]
    public AudioSource musicAudioSource;
    public List<AudioClip> backgroundTracks = new List<AudioClip>();
    [Range(0f, 1f)]
    public float initialVolume = 0.7f;

    [Header("UI del Panel")]
    public PanelRenderer touchPanelRenderer;

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

    private int currentTrackIndex = -1;
    private bool isPaused;
    private bool hasStartedPlayback;
    private float currentVolume = 0.7f;
    private int lastActionFrame = -1;

    private VisualElement rootElement;
    private Label statusLabel;
    private Label trackTitleLabel;
    private Button prevButton;
    private Button playPauseButton;
    private Button nextButton;
    private Button volDownButton;
    private Button volUpButton;
    private VisualElement volumeTrack;
    private VisualElement volumeFill;
    private Label volumeLabel;

#if UNITY_EDITOR
    private void OnValidate()
    {
        LoadTracksFromEditorAssetDatabase();
    }
#endif

    private void Awake()
    {
        currentVolume = Mathf.Clamp01(initialVolume);
        EnsureColliderThickness();
        EnsureAudioSource();
        LoadTracksIfNeeded();
        FindTouchPanelRenderer();
    }

    private void EnsureColliderThickness()
    {
        if (gameObject.CompareTag("Untagged"))
        {
            gameObject.tag = "Interactible";
        }

        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null && GetComponent<Collider>() == null)
        {
            box = gameObject.AddComponent<BoxCollider>();
        }

        if (box != null && box.size.z < 0.001f)
        {
            box.size = new Vector3(
                Mathf.Max(box.size.x, 0.0042f),
                Mathf.Max(box.size.y, 0.0024f),
                0.0015f
            );
        }
    }

    private void Start()
    {
        PlayRandomTrack();
    }

    private void OnEnable()
    {
        FindTouchPanelRenderer();
        if (touchPanelRenderer != null)
        {
            touchPanelRenderer.RegisterUIReloadCallback(OnScreenUIReload);
            SetupScreenToPanelSpace();
        }
    }

    private void OnDisable()
    {
        if (touchPanelRenderer != null)
        {
            touchPanelRenderer.UnregisterUIReloadCallback(OnScreenUIReload);
            if (touchPanelRenderer.panelSettings != null)
            {
                touchPanelRenderer.panelSettings.SetScreenToPanelSpaceFunction(null);
            }
        }
        UnbindUI();
    }

    private void EnsureAudioSource()
    {
        if (musicAudioSource == null)
        {
            musicAudioSource = GetComponent<AudioSource>();
            if (musicAudioSource == null)
            {
                musicAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        musicAudioSource.playOnAwake = false;
        musicAudioSource.loop = false;
        musicAudioSource.spatialBlend = 0f;
        musicAudioSource.volume = currentVolume;
    }

    private void LoadTracksIfNeeded()
    {
        backgroundTracks.RemoveAll(c => c == null);
        if (backgroundTracks.Count > 0) return;

#if UNITY_EDITOR
        LoadTracksFromEditorAssetDatabase();
#endif

        if (backgroundTracks.Count == 0)
        {
            AudioClip[] loaded = Resources.LoadAll<AudioClip>("Audio/Background");
            if (loaded != null && loaded.Length > 0)
            {
                backgroundTracks.AddRange(loaded);
            }
        }
    }

#if UNITY_EDITOR
    private void LoadTracksFromEditorAssetDatabase()
    {
        backgroundTracks.RemoveAll(c => c == null);
        if (backgroundTracks.Count > 0) return;

        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Background" });
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null && !backgroundTracks.Contains(clip))
            {
                backgroundTracks.Add(clip);
            }
        }
    }
#endif

    private void FindTouchPanelRenderer()
    {
        if (touchPanelRenderer != null) return;

        touchPanelRenderer = GetComponent<PanelRenderer>();
        if (touchPanelRenderer != null) return;

        PanelRenderer[] renderers = FindObjectsByType<PanelRenderer>();
        foreach (PanelRenderer r in renderers)
        {
            if (r.gameObject.name.Contains("TouchPanel_01", System.StringComparison.OrdinalIgnoreCase) ||
                r.gameObject.name.Contains("TouchPanel_1", System.StringComparison.OrdinalIgnoreCase) ||
                (r.panelSettings != null && r.panelSettings.name.Contains("TouchPanel1", System.StringComparison.OrdinalIgnoreCase)))
            {
                touchPanelRenderer = r;
                break;
            }
        }
    }

    private void OnScreenUIReload(PanelRenderer renderer, VisualElement root, int version)
    {
        UnbindUI();
        rootElement = root;

        VisualElement musicRoot = root.Q<VisualElement>("musicPlayerRoot");
        if (musicRoot == null)
        {
            BuildMinimalMusicUI(root);
        }

        statusLabel = root.Q<Label>("statusLabel");
        trackTitleLabel = root.Q<Label>("trackTitleLabel");
        prevButton = root.Q<Button>("prevButton");
        playPauseButton = root.Q<Button>("playPauseButton");
        nextButton = root.Q<Button>("nextButton");
        volDownButton = root.Q<Button>("volDownButton");
        volUpButton = root.Q<Button>("volUpButton");
        volumeTrack = root.Q<VisualElement>("volumeTrack");
        volumeFill = root.Q<VisualElement>("volumeFill");
        volumeLabel = root.Q<Label>("volumeLabel");

        if (prevButton != null) prevButton.RegisterCallback<ClickEvent>(OnPrevClicked);
        if (playPauseButton != null) playPauseButton.RegisterCallback<ClickEvent>(OnPlayPauseClicked);
        if (nextButton != null) nextButton.RegisterCallback<ClickEvent>(OnNextClicked);
        if (volDownButton != null) volDownButton.RegisterCallback<ClickEvent>(OnVolDownClicked);
        if (volUpButton != null) volUpButton.RegisterCallback<ClickEvent>(OnVolUpClicked);

        UpdateMusicUI();
    }

    private void BuildMinimalMusicUI(VisualElement root)
    {
        root.Clear();

        var container = new VisualElement { name = "musicPlayerRoot" };
        container.style.width = Length.Percent(100);
        container.style.height = Length.Percent(100);
        container.style.backgroundColor = new Color(0.04f, 0.055f, 0.086f, 1f);
        container.style.paddingTop = 48;
        container.style.paddingBottom = 48;
        container.style.paddingLeft = 48;
        container.style.paddingRight = 48;
        container.style.justifyContent = Justify.SpaceBetween;
        container.style.alignItems = Align.Center;
        container.style.borderTopWidth = 6;
        container.style.borderBottomWidth = 6;
        container.style.borderLeftWidth = 6;
        container.style.borderRightWidth = 6;
        container.style.borderTopColor = new Color(0.35f, 0.78f, 0.98f, 0.35f);
        container.style.borderBottomColor = new Color(0.35f, 0.78f, 0.98f, 0.35f);
        container.style.borderLeftColor = new Color(0.35f, 0.78f, 0.98f, 0.35f);
        container.style.borderRightColor = new Color(0.35f, 0.78f, 0.98f, 0.35f);

        var header = new Label("AUDIO // SISTEMA DE MÚSICA") { name = "headerLabel" };
        header.style.fontSize = 28;
        header.style.color = new Color(0.51f, 0.82f, 1f, 0.8f);
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.unityTextAlign = TextAnchor.MiddleCenter;
        container.Add(header);

        var trackBox = new VisualElement { name = "trackBox" };
        trackBox.style.width = Length.Percent(90);
        trackBox.style.paddingTop = 32;
        trackBox.style.paddingBottom = 32;
        trackBox.style.paddingLeft = 24;
        trackBox.style.paddingRight = 24;
        trackBox.style.backgroundColor = new Color(1f, 1f, 1f, 0.04f);
        trackBox.style.borderTopLeftRadius = 16;
        trackBox.style.borderTopRightRadius = 16;
        trackBox.style.borderBottomLeftRadius = 16;
        trackBox.style.borderBottomRightRadius = 16;
        trackBox.style.borderTopWidth = 2;
        trackBox.style.borderBottomWidth = 2;
        trackBox.style.borderLeftWidth = 2;
        trackBox.style.borderRightWidth = 2;
        trackBox.style.borderTopColor = new Color(1f, 1f, 1f, 0.12f);
        trackBox.style.borderBottomColor = new Color(1f, 1f, 1f, 0.12f);
        trackBox.style.borderLeftColor = new Color(1f, 1f, 1f, 0.12f);
        trackBox.style.borderRightColor = new Color(1f, 1f, 1f, 0.12f);
        trackBox.style.alignItems = Align.Center;
        trackBox.style.justifyContent = Justify.Center;

        var status = new Label("REPRODUCIENDO") { name = "statusLabel" };
        status.style.fontSize = 24;
        status.style.color = new Color(0.35f, 0.86f, 0.63f, 1f);
        status.style.marginBottom = 10;
        status.style.unityFontStyleAndWeight = FontStyle.Bold;
        status.style.unityTextAlign = TextAnchor.MiddleCenter;
        trackBox.Add(status);

        var title = new Label("Cargando pista...") { name = "trackTitleLabel" };
        title.style.fontSize = 54;
        title.style.color = new Color(0.96f, 0.97f, 1f, 1f);
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.unityTextAlign = TextAnchor.MiddleCenter;
        title.style.whiteSpace = WhiteSpace.Normal;
        trackBox.Add(title);

        container.Add(trackBox);

        var controlsRow = new VisualElement { name = "controlsRow" };
        controlsRow.style.flexDirection = FlexDirection.Row;
        controlsRow.style.alignItems = Align.Center;
        controlsRow.style.justifyContent = Justify.Center;
        controlsRow.style.width = Length.Percent(90);

        var prev = CreateStyledButton("prevButton", "ANT", 180, 96, false);
        var playPause = CreateStyledButton("playPauseButton", "PAUSAR", 280, 96, true);
        var next = CreateStyledButton("nextButton", "SIG", 180, 96, false);

        controlsRow.Add(prev);
        controlsRow.Add(playPause);
        controlsRow.Add(next);
        container.Add(controlsRow);

        var volumeRow = new VisualElement { name = "volumeRow" };
        volumeRow.style.flexDirection = FlexDirection.Row;
        volumeRow.style.alignItems = Align.Center;
        volumeRow.style.justifyContent = Justify.Center;
        volumeRow.style.width = Length.Percent(85);

        var volDown = CreateStyledButton("volDownButton", "-", 84, 72, false);
        var volTrack = new VisualElement { name = "volumeTrack" };
        volTrack.style.flexGrow = 1;
        volTrack.style.height = 28;
        volTrack.style.marginLeft = 20;
        volTrack.style.marginRight = 20;
        volTrack.style.backgroundColor = new Color(1f, 1f, 1f, 0.12f);
        volTrack.style.borderTopLeftRadius = 14;
        volTrack.style.borderTopRightRadius = 14;
        volTrack.style.borderBottomLeftRadius = 14;
        volTrack.style.borderBottomRightRadius = 14;
        volTrack.style.overflow = Overflow.Hidden;

        var volFill = new VisualElement { name = "volumeFill" };
        volFill.style.height = Length.Percent(100);
        volFill.style.width = Length.Percent(70);
        volFill.style.backgroundColor = new Color(0.35f, 0.78f, 0.98f, 1f);
        volTrack.Add(volFill);

        var volUp = CreateStyledButton("volUpButton", "+", 84, 72, false);
        var volLbl = new Label("70%") { name = "volumeLabel" };
        volLbl.style.width = 110;
        volLbl.style.fontSize = 32;
        volLbl.style.color = new Color(0.94f, 0.97f, 1f, 0.9f);
        volLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        volLbl.style.unityTextAlign = TextAnchor.MiddleRight;

        volumeRow.Add(volDown);
        volumeRow.Add(volTrack);
        volumeRow.Add(volUp);
        volumeRow.Add(volLbl);
        container.Add(volumeRow);

        var footer = new Label("[A / D] Cambiar pista   [Espacio] Play/Pausa   [W / S] Volumen   [ESC / F] Salir")
        {
            name = "footerLabel"
        };
        footer.style.fontSize = 22;
        footer.style.color = new Color(1f, 1f, 1f, 0.45f);
        footer.style.unityTextAlign = TextAnchor.MiddleCenter;
        container.Add(footer);

        root.Add(container);
    }

    private Button CreateStyledButton(string btnName, string text, float minWidth, float height, bool primary)
    {
        var btn = new Button { name = btnName, text = text };
        btn.style.minWidth = minWidth;
        btn.style.height = height;
        btn.style.marginLeft = 12;
        btn.style.marginRight = 12;
        btn.style.fontSize = 34;
        btn.style.unityFontStyleAndWeight = FontStyle.Bold;
        btn.style.color = new Color(0.94f, 0.97f, 1f, 1f);
        btn.style.backgroundColor = primary
            ? new Color(0.35f, 0.78f, 0.98f, 0.22f)
            : new Color(1f, 1f, 1f, 0.08f);
        btn.style.borderTopWidth = 3;
        btn.style.borderBottomWidth = 3;
        btn.style.borderLeftWidth = 3;
        btn.style.borderRightWidth = 3;
        Color borderColor = primary
            ? new Color(0.35f, 0.78f, 0.98f, 0.9f)
            : new Color(0.51f, 0.82f, 1f, 0.45f);
        btn.style.borderTopColor = borderColor;
        btn.style.borderBottomColor = borderColor;
        btn.style.borderLeftColor = borderColor;
        btn.style.borderRightColor = borderColor;
        btn.style.borderTopLeftRadius = 14;
        btn.style.borderTopRightRadius = 14;
        btn.style.borderBottomLeftRadius = 14;
        btn.style.borderBottomRightRadius = 14;
        return btn;
    }

    private void UnbindUI()
    {
        if (prevButton != null) prevButton.UnregisterCallback<ClickEvent>(OnPrevClicked);
        if (playPauseButton != null) playPauseButton.UnregisterCallback<ClickEvent>(OnPlayPauseClicked);
        if (nextButton != null) nextButton.UnregisterCallback<ClickEvent>(OnNextClicked);
        if (volDownButton != null) volDownButton.UnregisterCallback<ClickEvent>(OnVolDownClicked);
        if (volUpButton != null) volUpButton.UnregisterCallback<ClickEvent>(OnVolUpClicked);
    }

    private void OnPrevClicked(ClickEvent evt) => TriggerAction(PreviousTrack);
    private void OnPlayPauseClicked(ClickEvent evt) => TriggerAction(TogglePlayPause);
    private void OnNextClicked(ClickEvent evt) => TriggerAction(NextTrack);
    private void OnVolDownClicked(ClickEvent evt) => TriggerAction(() => ChangeVolume(-0.1f));
    private void OnVolUpClicked(ClickEvent evt) => TriggerAction(() => ChangeVolume(0.1f));

    private void TriggerAction(System.Action action)
    {
        if (Time.frameCount == lastActionFrame) return;
        lastActionFrame = Time.frameCount;
        action?.Invoke();
    }

    public void PlayRandomTrack()
    {
        LoadTracksIfNeeded();
        if (backgroundTracks.Count == 0)
        {
            UpdateMusicUI();
            return;
        }

        int newIndex = Random.Range(0, backgroundTracks.Count);
        if (backgroundTracks.Count > 1 && newIndex == currentTrackIndex)
        {
            newIndex = (newIndex + 1) % backgroundTracks.Count;
        }

        PlayTrackAtIndex(newIndex);
    }

    public void PlayTrackAtIndex(int index)
    {
        LoadTracksIfNeeded();
        if (backgroundTracks.Count == 0) return;

        currentTrackIndex = ((index % backgroundTracks.Count) + backgroundTracks.Count) % backgroundTracks.Count;
        AudioClip clip = backgroundTracks[currentTrackIndex];
        if (clip == null) return;

        EnsureAudioSource();
        musicAudioSource.clip = clip;
        musicAudioSource.volume = currentVolume;
        musicAudioSource.Play();
        isPaused = false;
        hasStartedPlayback = true;

        UpdateMusicUI();
    }

    public void NextTrack()
    {
        if (backgroundTracks.Count == 0) return;
        PlayTrackAtIndex(currentTrackIndex + 1);
    }

    public void PreviousTrack()
    {
        if (backgroundTracks.Count == 0) return;
        PlayTrackAtIndex(currentTrackIndex - 1);
    }

    public void TogglePlayPause()
    {
        if (musicAudioSource == null) return;

        if (musicAudioSource.clip == null)
        {
            PlayRandomTrack();
            return;
        }

        if (isPaused)
        {
            musicAudioSource.UnPause();
            if (!musicAudioSource.isPlaying)
            {
                musicAudioSource.Play();
            }
            isPaused = false;
        }
        else
        {
            musicAudioSource.Pause();
            isPaused = true;
        }

        UpdateMusicUI();
    }

    public void SetVolume(float volume)
    {
        currentVolume = Mathf.Clamp01(volume);
        if (musicAudioSource != null)
        {
            musicAudioSource.volume = currentVolume;
        }
        UpdateMusicUI();
    }

    public void ChangeVolume(float delta)
    {
        SetVolume(currentVolume + delta);
    }

    private void UpdateMusicUI()
    {
        if (trackTitleLabel != null)
        {
            if (currentTrackIndex >= 0 && currentTrackIndex < backgroundTracks.Count && backgroundTracks[currentTrackIndex] != null)
            {
                trackTitleLabel.text = FormatTrackName(backgroundTracks[currentTrackIndex].name);
            }
            else
            {
                trackTitleLabel.text = "Sin pistas";
            }
        }

        if (statusLabel != null)
        {
            statusLabel.text = isPaused ? "EN PAUSA" : "REPRODUCIENDO";
            statusLabel.style.color = isPaused
                ? new Color(1f, 0.75f, 0.3f, 1f)
                : new Color(0.35f, 0.86f, 0.63f, 1f);
        }

        if (playPauseButton != null)
        {
            playPauseButton.text = isPaused ? "REPRODUCIR" : "PAUSAR";
        }

        int pct = Mathf.RoundToInt(currentVolume * 100f);
        if (volumeFill != null)
        {
            volumeFill.style.width = Length.Percent(pct);
        }

        if (volumeLabel != null)
        {
            volumeLabel.text = $"{pct}%";
        }
    }

    private string FormatTrackName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName)) return "Pista Desconocida";
        return rawName.Replace("-", " ").Replace("_", " ");
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
        UpdateMusicUI();

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
        if (hasStartedPlayback && !isPaused && musicAudioSource != null && !musicAudioSource.isPlaying && backgroundTracks.Count > 0)
        {
            if (Application.isFocused)
            {
                PlayRandomTrack();
            }
        }

        if (!isFocused || isTransitioning) return;

        FixPlayerAndCamera(currentTargetPos, currentTargetRot);

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            {
                TriggerAction(PreviousTrack);
            }
            else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                TriggerAction(NextTrack);
            }
            else if (keyboard.spaceKey.wasPressedThisFrame)
            {
                TriggerAction(TogglePlayPause);
            }
            else if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
            {
                TriggerAction(() => ChangeVolume(0.1f));
            }
            else if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
            {
                TriggerAction(() => ChangeVolume(-0.1f));
            }
            else if (keyboard.escapeKey.wasPressedThisFrame || keyboard.fKey.wasPressedThisFrame)
            {
                ExitScreenFocus();
                return;
            }
        }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = mouse.position.ReadValue();
            if (TryGetPanelPointFromScreen(mousePos, true, out Vector2 panelPoint))
            {
                if (prevButton != null && prevButton.worldBound.Contains(panelPoint))
                {
                    TriggerAction(PreviousTrack);
                }
                else if (playPauseButton != null && playPauseButton.worldBound.Contains(panelPoint))
                {
                    TriggerAction(TogglePlayPause);
                }
                else if (nextButton != null && nextButton.worldBound.Contains(panelPoint))
                {
                    TriggerAction(NextTrack);
                }
                else if (volDownButton != null && volDownButton.worldBound.Contains(panelPoint))
                {
                    TriggerAction(() => ChangeVolume(-0.1f));
                }
                else if (volUpButton != null && volUpButton.worldBound.Contains(panelPoint))
                {
                    TriggerAction(() => ChangeVolume(0.1f));
                }
                else if (volumeTrack != null && volumeTrack.worldBound.Contains(panelPoint))
                {
                    Rect bound = volumeTrack.worldBound;
                    if (bound.width > 1f)
                    {
                        float norm = Mathf.Clamp01((panelPoint.x - bound.xMin) / bound.width);
                        TriggerAction(() => SetVolume(norm));
                    }
                }
            }
        }
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
        Vector3 localSize = new Vector3(0.00587f, 0.0112f, 0.001f);

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
        if (touchPanelRenderer == null || touchPanelRenderer.panelSettings == null) return;

        touchPanelRenderer.panelSettings.SetScreenToPanelSpaceFunction((Vector2 screenPosition) =>
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
        else if (touchPanelRenderer != null && touchPanelRenderer.panelSettings != null)
        {
            Vector2Int refRes = touchPanelRenderer.panelSettings.referenceResolution;
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
