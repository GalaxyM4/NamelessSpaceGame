using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class UIScript : MonoBehaviour
{
    PanelRenderer panelRenderer;
    Button play;
    Button exit;

    void OnEnable()
    {
        Debug.Log("gasdasdsa");
        panelRenderer = GetComponent<PanelRenderer>();
        panelRenderer.RegisterUIReloadCallback(OnUIReload);
    }

    void OnDestroy()
    {
        panelRenderer.UnregisterUIReloadCallback(OnUIReload);
    }

    void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
    {
        play = root.Q<Button>("play");
        play.clicked += PlayEvent;

        exit = root.Q<Button>("exit");
        exit.clicked += ExitEvent;
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