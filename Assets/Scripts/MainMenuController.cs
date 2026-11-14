using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    const string VolumeKey = "audio.masterVolume";
    const string MuteKey = "audio.muted";
    [SerializeField] GameObject menuPanel, settingsPanel, creditsPanel;
    [SerializeField] Button playButton, settingsButton, creditsButton, settingsBack, creditsBack, muteButton;
    [SerializeField] Slider volumeSlider;
    [SerializeField] Text muteLabel, volumeLabel;
    float volume;
    bool muted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RestoreAudio()
    {
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        AudioListener.pause = PlayerPrefs.GetInt(MuteKey, 0) != 0;
    }

    void Start()
    {
        volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        muted = PlayerPrefs.GetInt(MuteKey, 0) != 0;
        volumeSlider.SetValueWithoutNotify(volume);
        volumeSlider.onValueChanged.AddListener(SetVolume);
        playButton.onClick.AddListener(() => SceneManager.LoadScene("LobbyScene"));
        settingsButton.onClick.AddListener(OpenSettings);
        creditsButton.onClick.AddListener(OpenCredits);
        settingsBack.onClick.AddListener(BackFromSettings);
        creditsBack.onClick.AddListener(BackFromCredits);
        muteButton.onClick.AddListener(ToggleMute);
        ApplyAudio();
        Show(menuPanel, playButton);
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.tabKey.wasPressedThisFrame && EventSystem.current != null)
        {
            var activePanel = settingsPanel.activeSelf ? settingsPanel : creditsPanel.activeSelf ? creditsPanel : menuPanel;
            var controls = new System.Collections.Generic.List<Selectable>(activePanel.GetComponentsInChildren<Selectable>());
            controls.Add(muteButton);
            var current = EventSystem.current.currentSelectedGameObject;
            int index = controls.FindIndex(control => control.gameObject == current);
            int direction = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed ? -1 : 1;
            index = index < 0 ? 0 : (index + direction + controls.Count) % controls.Count;
            EventSystem.current.SetSelectedGameObject(controls[index].gameObject);
        }
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            if (settingsPanel.activeSelf) BackFromSettings();
            else if (creditsPanel.activeSelf) BackFromCredits();
        }
    }

    public void Quit() => Application.Quit();
    void OpenSettings() => Show(settingsPanel, volumeSlider);
    void OpenCredits() => Show(creditsPanel, creditsBack);
    void BackFromSettings() => Show(menuPanel, settingsButton);
    void BackFromCredits() => Show(menuPanel, creditsButton);

    void Show(GameObject panel, Selectable selected)
    {
        menuPanel.SetActive(panel == menuPanel);
        settingsPanel.SetActive(panel == settingsPanel);
        creditsPanel.SetActive(panel == creditsPanel);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(selected.gameObject);
    }

    void SetVolume(float value)
    {
        volume = Mathf.Clamp01(value);
        muted = false;
        ApplyAudio();
    }

    void ToggleMute()
    {
        muted = !muted;
        ApplyAudio();
    }

    void ApplyAudio()
    {
        AudioListener.volume = volume;
        AudioListener.pause = muted;
        PlayerPrefs.SetFloat(VolumeKey, volume);
        PlayerPrefs.SetInt(MuteKey, muted ? 1 : 0);
        PlayerPrefs.Save();
        muteLabel.text = muted ? "×  ACTIVAR SONIDO" : "◖  SILENCIAR";
        volumeLabel.text = $"VOLUMEN GENERAL  {Mathf.RoundToInt(volume * 100)}%";
    }
}
