using System;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(PanelRenderer))]
public class UiInterface : MonoBehaviour
{
    private PanelRenderer panelRenderer;
    private VisualElement root;
    private int m_Version = 0;

    private VisualElement resources;
    private Button resourceButton;
    private Button[] jetpackButtons = new Button[6];
    private Button saveButton;
    private Label selectedLabel;
    private Label costLabel;
    private Label weightLabel;
    private ProgressBar litersProgress;

    [Serializable]
    public struct jetpackData
    {
        public string name;
        public string cost;
        public string weight;
        public float liters;
    }

    [SerializeField]
    private jetpackData[] jetpacks = new jetpackData[]
    {
        new jetpackData {name="Bob Semple", cost ="100$", weight="4kg", liters=5},
        new jetpackData {name="Plastimax", cost ="200$", weight="45kg", liters=20},
        new jetpackData {name="Plastimax II", cost ="300$", weight="15kg", liters=15},
        new jetpackData {name="Aluminium", cost ="500$", weight="8kg", liters=20},
        new jetpackData {name="Fibrividrio", cost ="700$", weight="4kg", liters=35}
    };

    private void Awake()
    {
        panelRenderer = GetComponent<PanelRenderer>();
    }

    private void OnEnable() => GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
    private void OnDisable() => GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);

    private void OnUIReload(PanelRenderer panelRenderer, VisualElement root, int version)
    {
        if (version == m_Version) return;
        m_Version = version;

        resources = root.Q<VisualElement>("resources-right");
        resourceButton = root.Q<Button>("credits");

        jetpackButtons[0] = root.Q<Button>("bobsemple");
        jetpackButtons[1] = root.Q<Button>("superacerin");
        jetpackButtons[2] = root.Q<Button>("plastimax");
        jetpackButtons[3] = root.Q<Button>("plastimaxII");
        jetpackButtons[4] = root.Q<Button>("aluminium");
        jetpackButtons[5] = root.Q<Button>("fibrividrio");

        selectedLabel = root.Q<Label>("selected");
        costLabel = root.Q<Label>("cost");
        weightLabel = root.Q<Label>("weight");
        litersProgress = root.Q<ProgressBar>("capacity");
        saveButton = root.Q<Button>("save");

        resourceButton.RegisterCallback<ClickEvent>(OpenResources);
        saveButton.RegisterCallback<ClickEvent>(CloseNavigation);

        for (int i = 0; i < jetpackButtons.Length; i++)
        {
            if (jetpackButtons[i] != null)
            {
                // Pasamos el índice directamente
                jetpackButtons[i].RegisterCallback<ClickEvent, int>(SelectJetpack, i);
            }
        }
    }
    private void CloseNavigation(ClickEvent evt)
    {
        resources.RemoveFromClassList("right-active");
    }

    private void OpenResources(ClickEvent evt)
    {
        Debug.Log("No cargó" + evt);
        resources.AddToClassList("right-active");
    }

    private void SelectJetpack(ClickEvent evt, int index)
    {
        if (index >= 0 && index < jetpacks.Length)
        {
            selectedLabel.text = jetpacks[index].name;
            costLabel.text = jetpacks[index].cost;
            weightLabel.text = jetpacks[index].weight;
            litersProgress.lowValue = jetpacks[index].liters;
        }
        else
        {
            selectedLabel.text = "Selecciona uno";
            costLabel.text = "0.00$";
            weightLabel.text = "0Kg";
            litersProgress.lowValue = 0;
        }
    }
}
