using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-32000)]
public class SceneTransitionManager : SingletonMonoBehaviour<SceneTransitionManager>
{
    public const float DefaultFadeTime = 0.75f;

    private CanvasGroup fadeCanvasGroup;
    private Canvas fadeCanvas;
    private bool isTransitioning;
    private readonly HashSet<Behaviour> disabledInput = new HashSet<Behaviour>();

    /// <summary>Also use this guard in custom scripts that poll raw mouse/touch input.</summary>
    public static bool IsTransitioning => IsAlive() && Instance.isTransitioning;

    /// <summary>Returns true if accepted, not when finished. Call on Unity's main thread.</summary>
    public static bool Load(string sceneName, float fadeTime = DefaultFadeTime)
    {
        if (!Application.isPlaying)
            return false;
        return Instance.TryLoadScene(sceneName, fadeTime);
    }

    /// <summary>Reloads the active scene, not PlayerProfile or a saved game.</summary>
    public static bool Restart(float fadeTime = DefaultFadeTime)
    {
        if (!Application.isPlaying)
            return false;
        Scene active = SceneManager.GetActiveScene();
        // Use the full path so identically named scenes cannot restart the wrong one.
        return Load(string.IsNullOrEmpty(active.path) ? active.name : active.path, fadeTime);
    }

    // Preserve existing calls such as SceneTransitionManager.Instance.LoadScene("MainMenu", 0.75f).
    public void LoadScene(string sceneName, float fadeTime = DefaultFadeTime) => TryLoadScene(sceneName, fadeTime);
    public void RestartScene(float fadeTime = DefaultFadeTime) => Restart(fadeTime);

    protected override void Awake()
    {
        // A scene transition cannot survive activation unless its owner persists.
        _DontDestroyOnLoad = true;
        base.Awake();
    }

    public bool TryLoadScene(string sceneName, float fadeTime = DefaultFadeTime)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || Instance != this || isTransitioning)
            return false;
        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"Cannot transition to '{sceneName}'. Add the scene to the enabled Build Profiles scene list.", this);
            return false;
        }
        if (float.IsNaN(fadeTime) || float.IsInfinity(fadeTime) || fadeTime < 0f)
        {
            Debug.LogWarning("Fade time must be a finite value greater than or equal to zero.", this);
            return false;
        }

        // Even a manually placed manager must be a persistent root object.
        if (transform.parent != null)
            transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);
        if (fadeCanvasGroup == null)
            CreateFadeCanvas();

        isTransitioning = true;
        fadeCanvas.gameObject.SetActive(true);
        fadeCanvasGroup.blocksRaycasts = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        BlockSceneInput(); // Immediate, before another UI event can request a second transition.
        StartCoroutine(LoadSceneRoutine(sceneName, fadeTime));
        return true;
    }

    private IEnumerator LoadSceneRoutine(string sceneName, float fadeTime)
    {
        try
        {
            yield return Fade(1f, fadeTime);

            // Begin loading only after the old scene is completely covered.
            AsyncOperation operation = BeginLoading(sceneName);
            if (operation != null)
            {
                while (!operation.isDone)
                    yield return null;

                // isDone includes activation. Give the new scene's Start methods a frame
                // under the opaque overlay before revealing it.
                yield return null;
                BlockSceneInput();
            }

            // On a synchronous load error, reveal the original scene and restore its input.
            yield return Fade(0f, fadeTime);
            yield return null;
        }
        finally
        {
            FinishTransition();
        }
    }

    private AsyncOperation BeginLoading(string sceneName)
    {
        try
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
                Debug.LogError($"Unity could not start loading '{sceneName}'.", this);
            return operation;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            return null;
        }
    }

    private IEnumerator Fade(float targetAlpha, float duration)
    {
        float startAlpha = fadeCanvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Works even when Time.timeScale == 0.
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            yield return null;
        }
        fadeCanvasGroup.alpha = targetAlpha;
    }

    private void Update()
    {
        if (isTransitioning)
            BlockSceneInput();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => BlockSceneInput();

    private void BlockSceneInput()
    {
        // The overlay blocks pointer raycasts; disabling input modules also blocks a selected
        // button's keyboard/controller submit and clears old pointer/drag state on disable.
        foreach (EventSystem system in FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
        {
            foreach (BaseInputModule module in system.GetComponents<BaseInputModule>())
                DisableInput(module);
            DisableInput(system);
        }
    }

    private void DisableInput(Behaviour component)
    {
        if (!component.enabled)
            return;
        disabledInput.Add(component);
        component.enabled = false;
    }

    private void FinishTransition()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
        if (fadeCanvas != null)
            fadeCanvas.gameObject.SetActive(false);
        foreach (Behaviour component in disabledInput)
            if (component != null)
                component.enabled = true;
        disabledInput.Clear();
        isTransitioning = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        FinishTransition();
    }

    private void OnDestroy() => FinishTransition();

    private void CreateFadeCanvas()
    {
        GameObject canvasObj = new GameObject("Scene Transition Canvas", typeof(RectTransform));
        canvasObj.transform.SetParent(transform, false);

        fadeCanvas = canvasObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = short.MaxValue;

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject fadeObj = new GameObject("Fade Image", typeof(RectTransform));
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
        fadeCanvasGroup.ignoreParentGroups = true;
    }
}
