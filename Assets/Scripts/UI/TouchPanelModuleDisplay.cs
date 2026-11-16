using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(PanelRenderer))]
public class TouchPanelModuleDisplay : MonoBehaviour
{
    public enum RoverModuleSlot
    {
        Armor,
        Battery,
        Antenna
    }

    [SerializeField] private RoverModuleSlot slot = RoverModuleSlot.Battery;

    private PanelRenderer panelRenderer;
    private Button itemButton;
    private Label selectedLabel;
    private ProgressBar capacityProgress;
    private Label weightLabel;
    private Label priceLabel;

    private string currentSpriteClass = "sprite-bobsemple";

    private void Awake()
    {
        panelRenderer = GetComponent<PanelRenderer>();
    }

    private void OnEnable()
    {
        if (panelRenderer == null) panelRenderer = GetComponent<PanelRenderer>();
        panelRenderer.RegisterUIReloadCallback(OnUIReload);
        RoverModuleSelection.OnSelectionChanged += RefreshDisplay;
    }

    private void OnDisable()
    {
        RoverModuleSelection.OnSelectionChanged -= RefreshDisplay;
        if (panelRenderer != null)
        {
            panelRenderer.UnregisterUIReloadCallback(OnUIReload);
        }
    }

    private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
    {
        itemButton = root.Q<Button>("item");
        selectedLabel = root.Q<Label>("selected");
        capacityProgress = root.Q<ProgressBar>("capacity");
        weightLabel = root.Q<Label>("weight");
        priceLabel = root.Q<Label>("price");

        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        switch (slot)
        {
            case RoverModuleSlot.Armor:
                RoverArmor armor = RoverModuleSelection.SelectedArmor;
                if (armor != null)
                {
                    ApplyValues(armor.name, armor.spriteClass, armor.bangs, 20f, $"{armor.bangs} golpes", $"{armor.price}$");
                }
                else
                {
                    ApplyValues("Armor", "sprite-aluminium", 0f, 20f, "-", "0.00$");
                }
                break;

            case RoverModuleSlot.Battery:
                RoverBattery battery = RoverModuleSelection.SelectedBattery;
                if (battery != null)
                {
                    float kAh = battery.ah / 1000f;
                    ApplyValues(battery.name, battery.spriteClass, kAh, 30f, $"{kAh:0.#}kAh", $"{battery.price}$");
                }
                else
                {
                    ApplyValues("Battery", "sprite-superacerin", 0f, 30f, "-", "0.00$");
                }
                break;

            case RoverModuleSlot.Antenna:
                RoverAntenna antenna = RoverModuleSelection.SelectedAntenna;
                if (antenna != null)
                {
                    ApplyValues(antenna.name, antenna.spriteClass, antenna.radius, 100f, $"{antenna.radius}cm / {antenna.speed}Hz", "N/D");
                }
                else
                {
                    ApplyValues("Antena", "sprite-fibrividrio", 0f, 100f, "-", "N/D");
                }
                break;
        }
    }

    private void ApplyValues(string title, string spriteClass, float progressVal, float progressMax, string detailText, string priceText)
    {
        if (selectedLabel != null) selectedLabel.text = title;
        if (weightLabel != null) weightLabel.text = detailText;
        if (priceLabel != null) priceLabel.text = priceText;

        if (capacityProgress != null)
        {
            capacityProgress.highValue = progressMax;
            capacityProgress.value = progressVal;
            capacityProgress.title = detailText;
        }

        if (itemButton != null && !string.IsNullOrEmpty(spriteClass))
        {
            itemButton.RemoveFromClassList(currentSpriteClass);
            currentSpriteClass = spriteClass;
            itemButton.AddToClassList(currentSpriteClass);
        }
    }
}
