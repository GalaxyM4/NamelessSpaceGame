using System;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(PanelRenderer))]
public class ComputerScript : MonoBehaviour
{
    private PanelRenderer panelRenderer;
    private int m_Version = 0;

    private Button[] jetpackButtons = new Button[6];
    private Button saveButton;
    private Label selectedLabel;
    private Label priceLabel;
    private Label weightLabel;
    private ProgressBar litersProgress;

    [Serializable]
    public struct JetpackData
    {
        public string name;
        public string cost;
        public string weight;
        public float liters;
    }

    [SerializeField]
    private JetpackData[] jetpacks = new JetpackData[]
    {
        new JetpackData { name = "Bob Semple", cost = "100$", weight = "4kg", liters = 5f },
        new JetpackData { name = "Súper Acerin", cost = "200$", weight = "45kg", liters = 8f },
        new JetpackData { name = "Plastimax", cost = "350$", weight = "15kg", liters = 15f },
        new JetpackData { name = "Plastimax II", cost = "400$", weight = "12kg", liters = 18f },
        new JetpackData { name = "Aluminium", cost = "500$", weight = "10kg", liters = 20f },
        new JetpackData { name = "Fibrividrio", cost = "700$", weight = "8kg", liters = 25f }
    };

    private static readonly string[] ButtonNames =
    {
        "bobsemple",
        "superacerin",
        "plastimax",
        "plastimaxII",
        "aluminium",
        "fibrividrio"
    };

    private void Awake()
    {
        panelRenderer = GetComponent<PanelRenderer>();
    }

    private void OnEnable()
    {
        if (panelRenderer == null) panelRenderer = GetComponent<PanelRenderer>();
        panelRenderer.RegisterUIReloadCallback(OnUIReload);
    }

    private void OnDisable()
    {
        if (panelRenderer != null)
        {
            panelRenderer.UnregisterUIReloadCallback(OnUIReload);
        }
    }

    private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
    {
        if (version == m_Version) return;
        m_Version = version;

        selectedLabel = root.Q<Label>("selected");
        priceLabel = root.Q<Label>("price");
        weightLabel = root.Q<Label>("weight");
        litersProgress = root.Q<ProgressBar>("capacity");
        saveButton = root.Q<Button>("save");

        for (int i = 0; i < ButtonNames.Length; i++)
        {
            jetpackButtons[i] = root.Q<Button>(ButtonNames[i]);
            if (jetpackButtons[i] != null)
            {
                jetpackButtons[i].RegisterCallback<ClickEvent, int>(SelectJetpack, i);
            }
        }

        if (saveButton != null)
        {
            saveButton.RegisterCallback<ClickEvent>(OnSaveClicked);
        }
    }

    private void OnSaveClicked(ClickEvent evt)
    {
        if (selectedLabel != null)
        {
            Debug.Log($"Seleccionado en computadora: {selectedLabel.text}");
        }
    }

    private void SelectJetpack(ClickEvent evt, int index)
    {
        if (index >= 0 && index < jetpacks.Length)
        {
            if (selectedLabel != null) selectedLabel.text = jetpacks[index].name;
            if (priceLabel != null) priceLabel.text = jetpacks[index].cost;
            if (weightLabel != null) weightLabel.text = jetpacks[index].weight;
            if (litersProgress != null)
            {
                litersProgress.value = jetpacks[index].liters;
                litersProgress.title = $"{jetpacks[index].liters}L";
            }
        }
        else
        {
            if (selectedLabel != null) selectedLabel.text = "Selecciona uno";
            if (priceLabel != null) priceLabel.text = "0.00$";
            if (weightLabel != null) weightLabel.text = "0Kg";
            if (litersProgress != null)
            {
                litersProgress.value = 0f;
                litersProgress.title = string.Empty;
            }
        }
    }
}
