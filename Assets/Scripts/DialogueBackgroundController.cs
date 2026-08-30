using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class DialogueBackgroundController : MonoBehaviour
{
    private const string BackgroundFolder = "Assets/Graphic/Backgrounds";

    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image fadeImage;
    [SerializeField] private List<Sprite> backgroundSprites = new();
    [SerializeField] private bool preserveAspect;

    private static DialogueBackgroundController activeController;
    private readonly Dictionary<string, Sprite> spriteLookup = new(System.StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        activeController = this;
        EnsureReferences();
        BuildSpriteLookup();
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

    private void Reset()
    {
        backgroundImage = GetComponent<Image>();
        preserveAspect = backgroundImage != null && backgroundImage.preserveAspect;
        CacheBackgroundSprites();
    }

    private void OnValidate()
    {
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        if (backgroundImage != null)
        {
            backgroundImage.preserveAspect = preserveAspect;
        }

        CacheBackgroundSprites();
    }

    [YarnCommand("background")]
    public static IEnumerator SetBackground(string spriteName, string fadeTimeOrInstant = "instant", string fadeColor = "black")
    {
        yield return RunBackgroundCommand(spriteName, fadeTimeOrInstant, fadeColor);
    }

    [YarnCommand("bg")]
    public static IEnumerator SetBackgroundShort(string spriteName, string fadeTimeOrInstant = "instant", string fadeColor = "black")
    {
        yield return RunBackgroundCommand(spriteName, fadeTimeOrInstant, fadeColor);
    }

    private static IEnumerator RunBackgroundCommand(string spriteName, string fadeTimeOrInstant, string fadeColor)
    {
        if (activeController == null)
        {
            Debug.LogError("No active DialogueBackgroundController found in the scene.");
            yield break;
        }

        yield return activeController.ChangeBackground(spriteName, fadeTimeOrInstant, fadeColor);
    }

    private IEnumerator ChangeBackground(string spriteName, string fadeTimeOrInstant, string fadeColorName)
    {
        if (!EnsureReferences())
        {
            yield break;
        }

        Sprite nextSprite = FindBackgroundSprite(spriteName);
        if (nextSprite == null)
        {
            Debug.LogWarning($"Dialogue background sprite '{spriteName}' was not found in {BackgroundFolder}.");
            yield break;
        }

        float fadeTime = ParseFadeTime(fadeTimeOrInstant);
        Color fadeColor = ParseFadeColor(fadeColorName);

        if (fadeTime <= 0f)
        {
            ApplyBackgroundSprite(nextSprite);
            SetFadeAlpha(0f, fadeColor);
            yield break;
        }

        float halfFadeTime = fadeTime * 0.5f;
        yield return FadeOverlay(fadeColor.a, halfFadeTime, fadeColor);
        ApplyBackgroundSprite(nextSprite);
        yield return FadeOverlay(0f, halfFadeTime, fadeColor);
    }

    private bool EnsureReferences()
    {
        if (backgroundImage == null && !TryGetComponent(out backgroundImage))
        {
            Debug.LogError("DialogueBackgroundController needs a UI Image target.");
            return false;
        }

        backgroundImage.preserveAspect = preserveAspect;

        if (fadeImage == null)
        {
            fadeImage = CreateFadeImage();
        }

        return fadeImage != null;
    }

    private Image CreateFadeImage()
    {
        GameObject fadeObject = new GameObject("Background Fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fadeObject.transform.SetParent(transform, false);
        fadeObject.transform.SetAsLastSibling();

        RectTransform fadeRect = fadeObject.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;

        Image image = fadeObject.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = false;
        return image;
    }

    private Sprite FindBackgroundSprite(string spriteName)
    {
        if (string.IsNullOrWhiteSpace(spriteName))
        {
            return null;
        }

        BuildSpriteLookup();

        string normalizedName = NormalizeSpriteKey(spriteName);
        if (spriteLookup.TryGetValue(normalizedName, out Sprite sprite))
        {
            return sprite;
        }

#if UNITY_EDITOR
        Sprite editorSprite = FindSpriteInAssetDatabase(normalizedName);
        if (editorSprite != null)
        {
            AddSpriteReference(editorSprite);
            BuildSpriteLookup();
            return editorSprite;
        }
#endif

        return null;
    }

    private void ApplyBackgroundSprite(Sprite sprite)
    {
        backgroundImage.sprite = sprite;
        backgroundImage.color = Color.white;
        backgroundImage.enabled = true;
    }

    private IEnumerator FadeOverlay(float targetAlpha, float duration, Color fadeColor)
    {
        if (duration <= 0f)
        {
            SetFadeAlpha(targetAlpha, fadeColor);
            yield break;
        }

        Color startColor = fadeImage.color;
        Color targetColor = fadeColor;
        targetColor.a = targetAlpha;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeImage.color = Color.Lerp(startColor, targetColor, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        fadeImage.color = targetColor;
    }

    private void SetFadeAlpha(float alpha, Color fadeColor)
    {
        fadeColor.a = alpha;
        fadeImage.color = fadeColor;
    }

    private float ParseFadeTime(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0f;
        }

        string normalized = value.Trim().ToLowerInvariant();
        if (normalized == "instant" || normalized == "none")
        {
            return 0f;
        }

        if (float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out float fadeTime))
        {
            return Mathf.Max(0f, fadeTime);
        }

        Debug.LogWarning($"Invalid background fade time '{value}'. Using instant transition.");
        return 0f;
    }

    private Color ParseFadeColor(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Color.black;
        }

        string normalized = value.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "black":
                return Color.black;
            case "white":
                return Color.white;
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
            case "gray":
            case "grey":
                return Color.gray;
            case "clear":
                return Color.clear;
        }

        string htmlColor = normalized;
        if (!htmlColor.StartsWith("#") && (htmlColor.Length == 6 || htmlColor.Length == 8))
        {
            htmlColor = "#" + htmlColor;
        }

        if (ColorUtility.TryParseHtmlString(htmlColor, out Color parsedColor))
        {
            return parsedColor;
        }

        Debug.LogWarning($"Invalid background fade color '{value}'. Using black.");
        return Color.black;
    }

    private void BuildSpriteLookup()
    {
        spriteLookup.Clear();

        foreach (Sprite sprite in backgroundSprites)
        {
            AddSpriteLookup(sprite);
        }
    }

    private void AddSpriteLookup(Sprite sprite)
    {
        if (sprite == null)
        {
            return;
        }

        RegisterSpriteKey(sprite.name, sprite);

        if (sprite.texture != null)
        {
            RegisterSpriteKey(sprite.texture.name, sprite);
        }

        RegisterSpriteKey(RemoveTrailingNumericSuffix(sprite.name), sprite);
    }

    private void RegisterSpriteKey(string key, Sprite sprite)
    {
        key = NormalizeSpriteKey(key);
        if (string.IsNullOrEmpty(key) || spriteLookup.ContainsKey(key))
        {
            return;
        }

        spriteLookup.Add(key, sprite);
    }

    private string NormalizeSpriteKey(string key)
    {
        return string.IsNullOrWhiteSpace(key)
            ? string.Empty
            : key.Trim().Replace("\\", "/");
    }

    private string RemoveTrailingNumericSuffix(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        int separatorIndex = key.LastIndexOf('_');
        if (separatorIndex <= 0 || separatorIndex >= key.Length - 1)
        {
            return key;
        }

        return int.TryParse(key[(separatorIndex + 1)..], out _)
            ? key[..separatorIndex]
            : key;
    }

    private void AddSpriteReference(Sprite sprite)
    {
        if (sprite == null || backgroundSprites.Contains(sprite))
        {
            return;
        }

        backgroundSprites.Add(sprite);
    }

    [ContextMenu("Refresh Background Sprite Cache")]
    private void CacheBackgroundSprites()
    {
#if UNITY_EDITOR
        string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { BackgroundFolder });
        List<Sprite> sprites = new();

        foreach (string guid in spriteGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Sprite mainSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            AddUniqueSprite(sprites, mainSprite);

            Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
            foreach (Object asset in assets)
            {
                AddUniqueSprite(sprites, asset as Sprite);
            }
        }

        backgroundSprites = sprites;
#endif
    }

#if UNITY_EDITOR
    private static Sprite FindSpriteInAssetDatabase(string spriteName)
    {
        string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { BackgroundFolder });
        foreach (string guid in spriteGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            if (string.Equals(fileName, spriteName, System.StringComparison.OrdinalIgnoreCase))
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
            foreach (Object asset in assets)
            {
                if (asset is not Sprite sprite)
                {
                    continue;
                }

                if (string.Equals(sprite.name, spriteName, System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(RemoveTrailingNumericSuffixStatic(sprite.name), spriteName, System.StringComparison.OrdinalIgnoreCase)
                    || sprite.texture != null && string.Equals(sprite.texture.name, spriteName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return sprite;
                }
            }
        }

        return null;
    }

    private static void AddUniqueSprite(List<Sprite> sprites, Sprite sprite)
    {
        if (sprite != null && !sprites.Contains(sprite))
        {
            sprites.Add(sprite);
        }
    }

    private static string RemoveTrailingNumericSuffixStatic(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        int separatorIndex = key.LastIndexOf('_');
        if (separatorIndex <= 0 || separatorIndex >= key.Length - 1)
        {
            return key;
        }

        return int.TryParse(key[(separatorIndex + 1)..], out _)
            ? key[..separatorIndex]
            : key;
    }
#endif
}
