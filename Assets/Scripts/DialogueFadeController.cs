using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

[DisallowMultipleComponent]
public class DialogueFadeController : MonoBehaviour
{
    [SerializeField] private Canvas fadeCanvas;
    [SerializeField] private Image fadeImage;
    [SerializeField] private int sortingOrder = 5000;

    private static DialogueFadeController activeController;

    private void Awake()
    {
        activeController = this;
        EnsureReferences();
    }

    private void OnEnable()
    {
        activeController = this;
    }

    private void OnDisable()
    {
        if (activeController == this)
        {
            activeController = null;
        }
    }

    [YarnCommand("fade")]
    public static IEnumerator FadeCommand(params string[] args)
    {
        DialogueFadeController controller = GetOrCreateController();
        if (controller == null)
        {
            Debug.LogError("Could not create DialogueFadeController.");
            yield break;
        }

        yield return controller.RunFadeCommand(args);
    }

    private static DialogueFadeController GetOrCreateController()
    {
        if (activeController != null)
        {
            return activeController;
        }

        activeController = Object.FindFirstObjectByType<DialogueFadeController>();
        if (activeController != null)
        {
            return activeController;
        }

        GameObject controllerObject = new("Dialogue Fade Controller", typeof(DialogueFadeController));
        activeController = controllerObject.GetComponent<DialogueFadeController>();
        return activeController;
    }

    private IEnumerator RunFadeCommand(string[] args)
    {
        if (args == null || args.Length == 0)
        {
            Debug.LogWarning("Fade command needs an action: out, in, to, or clear.");
            yield break;
        }

        if (!EnsureReferences())
        {
            yield break;
        }

        string action = args[0].Trim().ToLowerInvariant();
        switch (action)
        {
            case "out":
                Color outColor = ParseColor(GetArg(args, 1, "black"), Color.black);
                yield return FadeOut(outColor, ParseDuration(GetArg(args, 2, "0.5"), 0.5f));
                break;

            case "in":
                Color inColor = ParseColor(GetArg(args, 1, "black"), Color.black);
                yield return FadeIn(inColor, ParseDuration(GetArg(args, 2, "0.5"), 0.5f));
                break;

            case "to":
                Color toColor = ParseColor(GetArg(args, 1, "black"), Color.black);
                float targetAlpha = ParseAlpha(GetArg(args, 2, "1"));
                yield return FadeOverlay(targetAlpha, ParseDuration(GetArg(args, 3, "0.5"), 0.5f), toColor);
                break;

            case "clear":
                yield return FadeOverlay(0f, ParseDuration(GetArg(args, 1, "instant"), 0f), fadeImage.color);
                break;

            default:
                Debug.LogWarning($"Unknown fade command action '{args[0]}'.");
                break;
        }
    }

    private bool EnsureReferences()
    {
        if (fadeImage != null)
        {
            return true;
        }

        if (fadeCanvas == null)
        {
            GameObject canvasObject = new("Dialogue Fade Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            fadeCanvas = canvasObject.GetComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        GameObject imageObject = new("Dialogue Fade Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(fadeCanvas.transform, false);

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        fadeImage = imageObject.GetComponent<Image>();
        fadeImage.color = Color.clear;
        SetBlocksRaycasts(false);
        return true;
    }

    private IEnumerator FadeOut(Color color, float duration)
    {
        yield return FadeOverlay(color.a, duration, color);
    }

    private IEnumerator FadeIn(Color color, float duration)
    {
        if (fadeImage.color.a <= 0.001f)
        {
            SetFadeAlpha(color.a, color);
        }

        yield return FadeOverlay(0f, duration, color);
    }

    private IEnumerator FadeOverlay(float targetAlpha, float duration, Color color)
    {
        if (!EnsureReferences())
        {
            yield break;
        }

        Color startColor = fadeImage.color;
        if (startColor.a <= 0.001f)
        {
            startColor = color;
            startColor.a = 0f;
        }

        Color targetColor = color;
        targetColor.a = Mathf.Clamp01(targetAlpha);
        SetBlocksRaycasts(true);

        if (duration <= 0f)
        {
            fadeImage.color = targetColor;
            SetBlocksRaycasts(targetColor.a > 0.001f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeImage.color = Color.Lerp(startColor, targetColor, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        fadeImage.color = targetColor;
        SetBlocksRaycasts(targetColor.a > 0.001f);
    }

    private void SetFadeAlpha(float alpha, Color color)
    {
        color.a = Mathf.Clamp01(alpha);
        fadeImage.color = color;
        SetBlocksRaycasts(color.a > 0.001f);
    }

    private void SetBlocksRaycasts(bool blocks)
    {
        if (fadeImage != null)
        {
            fadeImage.raycastTarget = blocks;
        }
    }

    private static float ParseDuration(string value, float fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        string normalized = value.Trim().ToLowerInvariant();
        if (normalized == "instant" || normalized == "none")
        {
            return 0f;
        }

        if (float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out float duration))
        {
            return Mathf.Max(0f, duration);
        }

        Debug.LogWarning($"Invalid fade duration '{value}'. Using {fallback}.");
        return fallback;
    }

    private static float ParseAlpha(string value)
    {
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float alpha))
        {
            return Mathf.Clamp01(alpha);
        }

        Debug.LogWarning($"Invalid fade alpha '{value}'. Using 1.");
        return 1f;
    }

    private static Color ParseColor(string value, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        string normalized = value.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "black":
                return Color.black;
            case "white":
                return Color.white;
            case "gray":
            case "grey":
                return Color.gray;
            case "clear":
            case "transparent":
                return Color.clear;
            case "red":
                return Color.red;
            case "green":
                return Color.green;
            case "blue":
                return Color.blue;
            case "yellow":
                return Color.yellow;
            case "cyan":
                return Color.cyan;
            case "magenta":
                return Color.magenta;
            default:
                if (ColorUtility.TryParseHtmlString(value.Trim(), out Color htmlColor))
                {
                    return htmlColor;
                }

                Debug.LogWarning($"Invalid fade color '{value}'. Using fallback color.");
                return fallback;
        }
    }

    private static string GetArg(string[] args, int index, string fallback)
    {
        return args != null && index >= 0 && index < args.Length
            ? args[index]
            : fallback;
    }
}
