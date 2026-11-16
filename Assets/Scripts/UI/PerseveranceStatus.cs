using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(PanelRenderer))]
public class PerseveranceStatus : MonoBehaviour
{
    [Header("Battery")]
    [Range(0f, 100f)]
    [SerializeField] private float batteryPercent = 100f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip cameraEndClip;

    private PanelRenderer panelRenderer;
    private int m_Version = 0;

    private Image batteryImage;
    private Label percentLabel;
    private VisualElement alertsElement;

    private VectorImage batteryCompleteSvg;
    private VectorImage batteryYellowSvg;
    private VectorImage batteryRedSvg;
    private VectorImage batteryVeryLowSvg;
    private VectorImage noBatterySvg;

    private float lastPercent = -1f;
    private bool wasAlarmActive = false;

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
            }
        }
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

        batteryImage = root.Q<Image>(className: "image-battery");
        percentLabel = root.Q<Label>(className: "percent-battery");
        alertsElement = root.Q<VisualElement>(className: "alerts");

        lastPercent = -1f;
        UpdateBatteryUI();
    }

    private void Update()
    {
        if (!Mathf.Approximately(batteryPercent, lastPercent))
        {
            UpdateBatteryUI();
        }
    }

    public void SetBatteryPercent(float percent)
    {
        batteryPercent = Mathf.Clamp(percent, 0f, 100f);
        UpdateBatteryUI();
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

        if (alertsElement != null)
        {
            if (isAlarmActive)
            {
                alertsElement.RemoveFromClassList("alerts-hidden");
            }
            else
            {
                alertsElement.AddToClassList("alerts-hidden");
            }
        }

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
