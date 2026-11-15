using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class UIScript : MonoBehaviour
{
    PanelRenderer panelRenderer;
    Button play;
    Button config;
    Button credits;
    Button exit;

    VisualElement settingsPanel;
    VisualElement creditsPanel;
    ListView creditsList;

    private readonly List<string> creditsEntries = new List<string>
    {
        "Nameless Space Game",
        "Desarrollo y Programación - IA + Kiwi Brotherhood",
        "Diseño de Interfaz y Arte - IA + Kiwi Brotherhood",
        "Motor Gráfico - Unity",
        "Uso de Blender y herramientas a fines",
        "® Nasa Space Apps"
    };

    void OnEnable()
    {
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
    }

    private void ShowSettings()
    {
        if (settingsPanel != null) settingsPanel.RemoveFromClassList("panel-hidden");
        if (creditsPanel != null) creditsPanel.AddToClassList("panel-hidden");
    }

    private void ShowCredits()
    {
        if (creditsPanel != null) creditsPanel.RemoveFromClassList("panel-hidden");
        if (settingsPanel != null) settingsPanel.AddToClassList("panel-hidden");
    }

    private void PlayEvent()
    {
        SceneManager.LoadScene("LobbyScene");
    }

    private void ExitEvent()
    {
        Application.Quit();
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
