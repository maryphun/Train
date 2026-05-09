using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : SingletonMonoBehaviour<SceneTransitionManager>
{
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    private bool isTransitioning;
    private bool isCanvasCreated = false;

    public void LoadScene(string sceneName, float fadeTime)
    {
        if (isTransitioning) return;
        if (!isCanvasCreated) CreateFadeCanvas();

        if (fadeTime == 0.0f)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        StartCoroutine(LoadSceneRoutine(sceneName, fadeTime));
    }

    private IEnumerator LoadSceneRoutine(string sceneName, float fadeTime)
    {
        isTransitioning = true;

        fadeCanvasGroup.blocksRaycasts = true;

        // preload scene
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        yield return Fade(1.0f, fadeTime);

        // wait until scene is loaded correctly
        while (operation.progress < 0.9f)
        {
            yield return null;
        }

        // actually change scene
        operation.allowSceneActivation = true;

        yield return null;

        yield return Fade(0.0f, fadeTime);

        fadeCanvasGroup.blocksRaycasts = false;

        isTransitioning = false;
    }

    private IEnumerator Fade(float targetAlpha, float time)
    {
        float startAlpha = fadeCanvasGroup.alpha;
        float timer = 0f;

        while (timer < time)
        {
            timer += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / time);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }

    private void CreateFadeCanvas()
    {
        GameObject canvasObj = new GameObject("Scene Transition Canvas");
        canvasObj.transform.SetParent(transform);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject fadeObj = new GameObject("Fade Image");
        fadeObj.transform.SetParent(canvasObj.transform, false);

        Image fadeImage = fadeObj.AddComponent<Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = true;

        RectTransform rect = fadeObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        fadeCanvasGroup = fadeObj.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.interactable = false;
        fadeCanvasGroup.blocksRaycasts = false;

        isCanvasCreated = true;
    }
}