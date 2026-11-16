using System.Collections.Generic;
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
    [SerializeField] private float cycleInterval = 3f;
    [SerializeField] private int slotOffset = 0;

    private PanelRenderer panelRenderer;
    private Button itemButton;
    private Label resourcesTitleLabel;
    private Label selectedLabel;
    private ProgressBar capacityProgress;
    private Label detailTitleLabel;
    private Label weightLabel;
    private Label priceLabel;

    private string currentSpriteClass = "sprite-bobsemple";
    private int currentCycleIndex;
    private float cycleTimer;

    public void SetSlotOffset(int offset)
    {
        slotOffset = Mathf.Max(0, offset);
        currentCycleIndex = slotOffset;
        RefreshDisplay();
    }

    private void Awake()
    {
        panelRenderer = GetComponent<PanelRenderer>();
    }

    private void OnEnable()
    {
        if (panelRenderer == null) panelRenderer = GetComponent<PanelRenderer>();
        panelRenderer.RegisterUIReloadCallback(OnUIReload);
        RoverModuleSelection.OnSelectionChanged += OnSelectionChanged;
        currentCycleIndex = slotOffset;
        cycleTimer = 0f;
    }

    private void OnDisable()
    {
        RoverModuleSelection.OnSelectionChanged -= OnSelectionChanged;
        if (panelRenderer != null)
        {
            panelRenderer.UnregisterUIReloadCallback(OnUIReload);
        }
    }

    private void Update()
    {
        IReadOnlyList<GenericModule> modules = RoverModuleSelection.SelectedModules;
        if (modules == null || modules.Count <= 1) return;

        cycleTimer += Time.deltaTime;
        if (cycleTimer >= Mathf.Max(0.5f, cycleInterval))
        {
            cycleTimer = 0f;
            currentCycleIndex = (currentCycleIndex + 1) % modules.Count;
            RefreshDisplay();
        }
    }

    private void OnSelectionChanged()
    {
        currentCycleIndex = slotOffset;
        cycleTimer = 0f;
        RefreshDisplay();
    }

    private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
    {
        itemButton = root.Q<Button>("item");
        resourcesTitleLabel = root.Q<Label>(className: "resources-title");
        selectedLabel = root.Q<Label>("selected");
        capacityProgress = root.Q<ProgressBar>("capacity");
        detailTitleLabel = root.Q<Label>(className: "label-stadistic");
        weightLabel = root.Q<Label>("weight");
        priceLabel = root.Q<Label>("price");

        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        IReadOnlyList<GenericModule> modules = RoverModuleSelection.SelectedModules;
        if (modules != null && modules.Count > 0)
        {
            int index = ((currentCycleIndex % modules.Count) + modules.Count) % modules.Count;
            GenericModule module = modules[index];

            if (module is RoverArmor armor)
            {
                ApplyValues("ARMOR", armor.name, armor.spriteClass, "Resistance:", armor.bangs, 20f, $"{armor.bangs} hits", $"{armor.price}$");
                return;
            }
            if (module is RoverBattery battery)
            {
                float kAh = battery.ah / 1000f;
                ApplyValues("BATTERY", battery.name, battery.spriteClass, "Capacity:", kAh, 30f, $"{kAh:0.#}kAh", $"{battery.price}$");
                return;
            }
            if (module is RoverAntenna antenna)
            {
                ApplyValues("ANTENNA", antenna.name, antenna.spriteClass, "Range:", antenna.radius, 100f, $"{antenna.radius}cm / {antenna.speed}Hz", $"{antenna.price}$");
                return;
            }
        }

        switch (slot)
        {
            case RoverModuleSlot.Armor:
                ApplyValues("ARMOR", "Armor", "aluminium-armor", "Resistance:", 0f, 20f, "-", "0.00$");
                break;

            case RoverModuleSlot.Battery:
                ApplyValues("BATTERY", "Battery", "niquelin-material", "Capacity:", 0f, 30f, "-", "0.00$");
                break;

            case RoverModuleSlot.Antenna:
                ApplyValues("ANTENNA", "Antenna", "tp-radin", "Range:", 0f, 100f, "-", "0.00$");
                break;
        }
    }

    private void ApplyValues(string categoryTitle, string title, string spriteClass, string statTitle, float progressVal, float progressMax, string detailText, string priceText)
    {
        if (resourcesTitleLabel != null) resourcesTitleLabel.text = categoryTitle;
        if (selectedLabel != null) selectedLabel.text = title;
        if (detailTitleLabel != null) detailTitleLabel.text = statTitle;
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
