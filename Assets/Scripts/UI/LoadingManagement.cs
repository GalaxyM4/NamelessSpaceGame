using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class LoadingManegment : MonoBehaviour
{
    public static string TargetScene;
    public float falseTime = 3;
    public float spinSpeed = 3;
    private PanelRenderer panelRenderer;
    private VisualElement img;
    void Start()
    {
        panelRenderer = GetComponent<PanelRenderer>();
        panelRenderer.RegisterUIReloadCallback(OnUIReload);

        StartCoroutine(LoadTargetSceneAsync());
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
        img = root.Q<VisualElement>("img-loading");
    }

    void Update()
    {
        if (img == null) return;

        float escalaX = Mathf.Cos(Time.time * spinSpeed);
        img.style.scale = new Scale(new Vector3(escalaX, 1f, 1f));
    }

    private IEnumerator LoadTargetSceneAsync()
    {
        yield return null;
        AsyncOperation operation = SceneManager.LoadSceneAsync(TargetScene, LoadSceneMode.Single);
        operation.allowSceneActivation = false;
        float transcurredTime = 0;

        while(!operation.isDone) {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            float falseProgress = Mathf.Clamp01(transcurredTime / falseTime);

            transcurredTime += Time.unscaledDeltaTime;
            if (progress >= 1f && falseProgress >= 1f) operation.allowSceneActivation = true;
    
            yield return null;
        }
    }
}
