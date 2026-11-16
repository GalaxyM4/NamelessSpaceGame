using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class UIScript : MonoBehaviour
{
    public float durationFade = 2f;
    public AudioSource menuAudioSource;

    PanelRenderer panelRenderer;
    VisualElement rootElement;
    VisualElement fadeOverlay;
    Button play;
    Button config;
    Button credits;
    Button exit;

    VisualElement settingsPanel;
    VisualElement creditsPanel;
    ListView creditsList;
    bool isFading;

    private readonly List<string> creditsEntries = new List<string>
    {
        "Nameless Space Game",
        "Development and Programming - AI + Kiwi Brotherhood",
        "Interface Design and Art - AI + Kiwi Brotherhood",
        "Game Engine - Unity",
        "Blender and related tools",
        "® Nasa Space Apps"
    };

    void OnEnable()
    {
        isFading = false;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        if (menuAudioSource == null)
        {
            menuAudioSource = GetComponentInParent<AudioSource>();
            if (menuAudioSource == null && transform.parent != null)
            {
                menuAudioSource = transform.parent.GetComponentInChildren<AudioSource>();
            }
            if (menuAudioSource == null)
            {
                menuAudioSource = FindAnyObjectByType<AudioSource>();
            }
        }

        panelRenderer = GetComponent<PanelRenderer>();
        panelRenderer.RegisterUIReloadCallback(OnUIReload);
    }

    void OnDestroy()
    {
        if (panelRenderer != null)
        {
            panelRenderer.UnregisterUIReloadCallback(OnUIReload);
        }
    }

    void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
    {
        rootElement = root;

        play = root.Q<Button>("play");
        if (play != null) play.clicked += PlayEvent;

        config = root.Q<Button>("config");
        if (config != null) config.clicked += ShowSettings;

        credits = root.Q<Button>("credits");
        if (credits != null) credits.clicked += ShowCredits;

        exit = root.Q<Button>("exit");
        if (exit != null) exit.clicked += ExitEvent;

        settingsPanel = root.Q<VisualElement>("settingsPanel");
        creditsPanel = root.Q<VisualElement>("creditsPanel");
        creditsList = root.Q<ListView>("creditsList");

        if (creditsList != null)
        {
            creditsList.makeItem = () =>
            {
                var label = new Label();
                label.AddToClassList("credits-item");
                return label;
            };
            creditsList.bindItem = (element, index) =>
            {
                if (element is Label label && index >= 0 && index < creditsEntries.Count)
                {
                    label.text = creditsEntries[index];
                }
            };
            creditsList.itemsSource = creditsEntries;
            creditsList.Rebuild();
        }

        fadeOverlay = root.Q<VisualElement>("fadeOverlay");
        if (fadeOverlay == null)
        {
            fadeOverlay = new VisualElement { name = "fadeOverlay" };
            fadeOverlay.style.position = Position.Absolute;
            fadeOverlay.style.left = 0;
            fadeOverlay.style.top = 0;
            fadeOverlay.style.right = 0;
            fadeOverlay.style.bottom = 0;
            fadeOverlay.style.width = Length.Percent(100);
            fadeOverlay.style.height = Length.Percent(100);
            root.Add(fadeOverlay);
        }
        fadeOverlay.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0f));
        fadeOverlay.pickingMode = PickingMode.Ignore;
        fadeOverlay.BringToFront();
    }

    private void ShowSettings()
    {
        if (isFading) return;
        if (settingsPanel != null) settingsPanel.RemoveFromClassList("panel-hidden");
        if (creditsPanel != null) creditsPanel.AddToClassList("panel-hidden");
    }

    private void ShowCredits()
    {
        if (isFading) return;
        if (creditsPanel != null) creditsPanel.RemoveFromClassList("panel-hidden");
        if (settingsPanel != null) settingsPanel.AddToClassList("panel-hidden");
    }

    private void PlayEvent()
    {
        if (isFading) return;
        StartCoroutine(FadeOutAndPlay());
    }

    private IEnumerator FadeOutAndPlay()
    {
        isFading = true;

        if (fadeOverlay != null)
        {
            fadeOverlay.pickingMode = PickingMode.Position;
            fadeOverlay.BringToFront();
        }

        float startVolume = menuAudioSource != null ? menuAudioSource.volume : 1f;
        float elapsed = 0f;

        if (durationFade > 0f)
        {
            while (elapsed < durationFade)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / durationFade);

                if (menuAudioSource != null)
                {
                    menuAudioSource.volume = Mathf.Lerp(startVolume, 0f, t);
                }

                if (fadeOverlay != null)
                {
                    fadeOverlay.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, t));
                }

                yield return null;
            }
        }

        if (menuAudioSource != null)
        {
            menuAudioSource.volume = 0f;
        }

        if (fadeOverlay != null)
        {
            fadeOverlay.style.backgroundColor = new StyleColor(Color.black);
        }

        LoadingManegment.TargetScene = "LobbyScene";
        SceneManager.LoadScene("LoadingScene");
    }

    private void ExitEvent()
    {
        if (isFading) return;
        Application.Quit();
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
