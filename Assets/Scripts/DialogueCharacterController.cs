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
public class DialogueCharacterController : DialoguePresenterBase
{
    private const float CharacterHorizontalOverscan = 250f;
    private const string CharacterFolder = "Assets/Graphic/Characters";
    private const string TokaBodyResource = "TokaBodyList";
    private const string TokaBodyPrefix = "Ch_Toka_Body_";
    private const string TokaFacePrefix = "Ch_Toka_Face_";

    [SerializeField] private RectTransform characterRoot;
    [SerializeField] private List<Sprite> characterSprites = new();
    [SerializeField, Range(0.1f, 1.5f)] private float characterHeightRatio = 0.9f;
    [SerializeField] private float bottomOffset;
    [SerializeField] private Vector2 characterPivot = new(0.5f, 0f);

    [Header("Speaker Focus")]
    [SerializeField] private bool focusSpeakerOnDialogueLine = true;
    [SerializeField] private Color nonSpeakerColor = new(0.68f, 0.68f, 0.68f, 1f);
    [SerializeField] private DialogueRunner dialogueRunner;

    private static DialogueCharacterController activeController;
    private readonly Dictionary<string, CharacterView> activeCharacters = new(System.StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Sprite> spriteLookup = new(System.StringComparer.OrdinalIgnoreCase);
    private DialogueRunner registeredDialogueRunner;
    private string speakingCharacterId;
    private TokaBodyList tokaBodyList;

    private void Awake()
    {
        activeController = this;
        EnsureReferences();
        BuildSpriteLookup();
    }

    private void OnEnable()
    {
        activeController = this;
        RegisterAsDialoguePresenter();
    }

    private void OnDisable()
    {
        SetSpeakingCharacter(null);
        UnregisterAsDialoguePresenter();

        if (activeController == this)
        {
            activeController = null;
        }
    }

    private void Reset()
    {
        CacheCharacterSprites();
    }

    private void OnValidate()
    {
        characterHeightRatio = Mathf.Max(0.1f, characterHeightRatio);
        CacheCharacterSprites();
    }

    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        SetSpeakingCharacter(focusSpeakerOnDialogueLine ? line?.CharacterName : null);
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueStartedAsync()
    {
        SetSpeakingCharacter(null);
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        SetSpeakingCharacter(null);
        return YarnTask.CompletedTask;
    }

    [YarnCommand("char")]
    public static IEnumerator CharacterCommand(params string[] args)
    {
        DialogueCharacterController controller = GetActiveController();
        if (controller == null)
        {
            Debug.LogError("No active DialogueCharacterController found in the scene.");
            yield break;
        }

        yield return controller.RunCharacterCommand(args);
    }

    private static DialogueCharacterController GetActiveController()
    {
        if (activeController != null)
        {
            return activeController;
        }

        activeController = Object.FindFirstObjectByType<DialogueCharacterController>();
        return activeController;
    }

    private IEnumerator RunCharacterCommand(string[] args)
    {
        if (args == null || args.Length == 0)
        {
            Debug.LogWarning("Character command needs an action: show, face, move, flip, tint, scale, hide, or clear.");
            yield break;
        }

        string action = args[0].Trim().ToLowerInvariant();
        switch (action)
        {
            case "show":
            case "add":
                yield return ShowCharacter(args);
                break;
            case "face":
            case "sprite":
            case "variation":
                yield return ChangeCharacterFace(args);
                break;
            case "move":
            case "position":
                yield return MoveCharacter(args);
                break;
            case "flip":
                FlipCharacter(args);
                break;
            case "tint":
            case "color":
                yield return TintCharacter(args);
                break;
            case "scale":
            case "size":
                yield return ScaleCharacter(args);
                break;
            case "hide":
            case "remove":
                yield return HideCharacter(args);
                break;
            case "clear":
            case "hide_all":
            case "remove_all":
                yield return ClearCharacters(args);
                break;
            default:
                Debug.LogWarning($"Unknown character command action '{args[0]}'.");
                break;
        }
    }

    private IEnumerator ShowCharacter(string[] args)
    {
        if (!TryReadArg(args, 1, "show", "character id", out string characterId)
            || !TryReadArg(args, 2, "show", "sprite name", out string spriteName))
        {
            yield break;
        }

        Sprite sprite = FindCharacterSprite(spriteName);
        if (sprite == null)
        {
            Debug.LogWarning($"Character sprite '{spriteName}' was not found in {CharacterFolder}.");
            yield break;
        }

        float xPosition = ParseNormalizedPosition(GetArg(args, 3, "0.5"));
        float fadeTime = ParseFadeTime(GetArg(args, 4, "instant"));
        bool flipped = ParseFlip(GetArg(args, 5, "false"));

        CharacterView view = GetOrCreateCharacterView(characterId);
        if (view == null)
        {
            yield break;
        }

        view.GameObject.SetActive(true);
        view.RectTransform.SetAsLastSibling();

        float fadeInTime = fadeTime;
        if (fadeTime > 0f && view.CanvasGroup.alpha > 0f && !CharacterVisualMatches(view, characterId, sprite))
        {
            yield return FadeCharacter(view, 0f, fadeTime * 0.5f);
            fadeInTime = fadeTime * 0.5f;
        }

        SetCharacterVisual(view, characterId, sprite);
        SetCharacterPosition(view, xPosition);
        SetCharacterFlip(view, flipped);

        if (fadeTime <= 0f)
        {
            view.CanvasGroup.alpha = 1f;
            yield break;
        }

        if (view.CanvasGroup.alpha <= 0f)
        {
            view.CanvasGroup.alpha = 0f;
        }

        yield return FadeCharacter(view, 1f, fadeInTime);
    }

    private IEnumerator ChangeCharacterFace(string[] args)
    {
        if (!TryReadArg(args, 1, "face", "character id", out string characterId)
            || !TryReadArg(args, 2, "face", "sprite name", out string spriteName))
        {
            yield break;
        }

        if (!activeCharacters.TryGetValue(characterId, out CharacterView view))
        {
            Debug.LogWarning($"Character '{characterId}' is not currently shown.");
            yield break;
        }

        Sprite sprite = FindCharacterSprite(spriteName);
        if (sprite == null)
        {
            Debug.LogWarning($"Character sprite '{spriteName}' was not found in {CharacterFolder}.");
            yield break;
        }

        float fadeTime = ParseFadeTime(GetArg(args, 3, "instant"));
        if (fadeTime <= 0f)
        {
            SetCharacterVisual(view, characterId, sprite);
            yield break;
        }

        yield return FadeCharacter(view, 0f, fadeTime * 0.5f);
        SetCharacterVisual(view, characterId, sprite);
        yield return FadeCharacter(view, 1f, fadeTime * 0.5f);
    }

    private IEnumerator MoveCharacter(string[] args)
    {
        if (!TryReadArg(args, 1, "move", "character id", out string characterId)
            || !TryReadArg(args, 2, "move", "x position", out string positionValue))
        {
            yield break;
        }

        if (!activeCharacters.TryGetValue(characterId, out CharacterView view))
        {
            Debug.LogWarning($"Character '{characterId}' is not currently shown.");
            yield break;
        }

        float xPosition = ParseNormalizedPosition(positionValue);
        float fadeTime = ParseFadeTime(GetArg(args, 3, "instant"));

        if (fadeTime <= 0f)
        {
            SetCharacterPosition(view, xPosition);
            yield break;
        }

        yield return MoveCharacterTo(view, xPosition, fadeTime);
    }

    private void FlipCharacter(string[] args)
    {
        if (!TryReadArg(args, 1, "flip", "character id", out string characterId))
        {
            return;
        }

        if (!activeCharacters.TryGetValue(characterId, out CharacterView view))
        {
            Debug.LogWarning($"Character '{characterId}' is not currently shown.");
            return;
        }

        bool flipped = ParseFlip(GetArg(args, 2, "true"));
        SetCharacterFlip(view, flipped);
    }

    private IEnumerator TintCharacter(string[] args)
    {
        if (!TryReadArg(args, 1, "tint", "character id", out string characterId)
            || !TryReadArg(args, 2, "tint", "color", out string colorValue))
        {
            yield break;
        }

        if (!activeCharacters.TryGetValue(characterId, out CharacterView view))
        {
            Debug.LogWarning($"Character '{characterId}' is not currently shown.");
            yield break;
        }

        Color tintColor = ParseColor(colorValue);
        float fadeTime = ParseFadeTime(GetArg(args, 3, "instant"));

        if (fadeTime <= 0f)
        {
            SetCharacterTint(view, tintColor);
            yield break;
        }

        yield return TintCharacterTo(view, tintColor, fadeTime);
    }

    private IEnumerator ScaleCharacter(string[] args)
    {
        if (!TryReadArg(args, 1, "scale", "character id", out string characterId)
            || !TryReadArg(args, 2, "scale", "scale value", out string scaleValue))
        {
            yield break;
        }

        if (!activeCharacters.TryGetValue(characterId, out CharacterView view))
        {
            Debug.LogWarning($"Character '{characterId}' is not currently shown.");
            yield break;
        }

        float targetScale = ParseScale(scaleValue);
        float fadeTime = ParseFadeTime(GetArg(args, 3, "instant"));

        if (fadeTime <= 0f)
        {
            SetCharacterScale(view, targetScale);
            yield break;
        }

        yield return ScaleCharacterTo(view, targetScale, fadeTime);
    }

    private IEnumerator HideCharacter(string[] args)
    {
        if (!TryReadArg(args, 1, "hide", "character id", out string characterId))
        {
            yield break;
        }

        if (!activeCharacters.TryGetValue(characterId, out CharacterView view))
        {
            yield break;
        }

        activeCharacters.Remove(characterId);

        float fadeTime = ParseFadeTime(GetArg(args, 2, "instant"));
        if (fadeTime > 0f)
        {
            yield return FadeCharacter(view, 0f, fadeTime);
        }

        Destroy(view.GameObject);
    }

    private IEnumerator ClearCharacters(string[] args)
    {
        float fadeTime = ParseFadeTime(GetArg(args, 1, "instant"));
        List<CharacterView> views = new(activeCharacters.Values);
        activeCharacters.Clear();

        if (fadeTime > 0f)
        {
            float elapsed = 0f;
            List<float> startAlphas = new();
            foreach (CharacterView view in views)
            {
                startAlphas.Add(view.CanvasGroup.alpha);
            }

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / fadeTime);
                for (int i = 0; i < views.Count; i++)
                {
                    if (views[i].CanvasGroup != null)
                    {
                        views[i].CanvasGroup.alpha = Mathf.Lerp(startAlphas[i], 0f, progress);
                    }
                }

                yield return null;
            }
        }

        foreach (CharacterView view in views)
        {
            if (view.GameObject != null)
            {
                Destroy(view.GameObject);
            }
        }
    }

    private CharacterView GetOrCreateCharacterView(string characterId)
    {
        if (!EnsureReferences())
        {
            return null;
        }

        if (activeCharacters.TryGetValue(characterId, out CharacterView view))
        {
            return view;
        }

        GameObject characterObject = new GameObject($"Character {characterId}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        characterObject.transform.SetParent(characterRoot, false);

        RectTransform rectTransform = characterObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.pivot = characterPivot;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localScale = Vector3.one;

        Image image = characterObject.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = Color.white;

        RawImage tokaBodyImage = CreateTokaLayer(characterObject.transform, "Toka Preset Body");
        RawImage tokaFaceImage = CreateTokaLayer(characterObject.transform, "Toka Face");

        CanvasGroup canvasGroup = characterObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        view = new CharacterView(
            characterId,
            characterObject,
            rectTransform,
            image,
            tokaBodyImage,
            tokaFaceImage,
            canvasGroup
        );
        activeCharacters.Add(characterId, view);
        return view;
    }

    private static RawImage CreateTokaLayer(Transform parent, string name)
    {
        GameObject layerObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        layerObject.transform.SetParent(parent, false);

        RectTransform rect = layerObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        RawImage image = layerObject.GetComponent<RawImage>();
        image.raycastTarget = false;
        image.color = Color.white;
        image.enabled = false;
        return image;
    }

    private void RegisterAsDialoguePresenter()
    {
        if (registeredDialogueRunner != null)
        {
            return;
        }

        DialogueRunner runner = dialogueRunner != null
            ? dialogueRunner
            : Object.FindFirstObjectByType<DialogueRunner>();

        if (runner == null)
        {
            Debug.LogWarning("DialogueCharacterController could not find a DialogueRunner, so automatic speaker focus is unavailable.", this);
            return;
        }

        List<DialoguePresenterBase> presenters = new();
        bool isAlreadyRegistered = false;

        foreach (DialoguePresenterBase presenter in runner.DialoguePresenters)
        {
            if (presenter == null)
            {
                continue;
            }

            presenters.Add(presenter);
            if (presenter == this)
            {
                isAlreadyRegistered = true;
            }
        }

        if (!isAlreadyRegistered)
        {
            presenters.Add(this);
            runner.DialoguePresenters = presenters;
        }

        registeredDialogueRunner = runner;
    }

    private void UnregisterAsDialoguePresenter()
    {
        if (registeredDialogueRunner == null)
        {
            return;
        }

        List<DialoguePresenterBase> presenters = new();
        foreach (DialoguePresenterBase presenter in registeredDialogueRunner.DialoguePresenters)
        {
            if (presenter != null && presenter != this)
            {
                presenters.Add(presenter);
            }
        }

        registeredDialogueRunner.DialoguePresenters = presenters;
        registeredDialogueRunner = null;
    }

    private void SetSpeakingCharacter(string characterId)
    {
        speakingCharacterId = string.IsNullOrWhiteSpace(characterId)
            ? null
            : characterId.Trim();

        foreach (CharacterView view in activeCharacters.Values)
        {
            ApplyCharacterTint(view);
        }
    }

    private bool EnsureReferences()
    {
        if (characterRoot != null)
        {
            return true;
        }

        RectTransform parent = transform as RectTransform;
        if (parent == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            parent = canvas != null ? canvas.transform as RectTransform : null;
        }

        if (parent == null)
        {
            Debug.LogError("DialogueCharacterController needs a RectTransform or parent Canvas.");
            return false;
        }

        characterRoot = CreateCharacterRoot(parent);
        return true;
    }

    private RectTransform CreateCharacterRoot(RectTransform parent)
    {
        GameObject rootObject = new GameObject("Character Stage", typeof(RectTransform));
        rootObject.transform.SetParent(parent, false);

        RectTransform root = rootObject.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.pivot = new Vector2(0.5f, 0.5f);

        Transform background = parent.Find("Background");
        if (background != null)
        {
            root.SetSiblingIndex(background.GetSiblingIndex() + 1);
        }

        return root;
    }

    private void SetCharacterVisual(CharacterView view, string characterId, Sprite sprite)
    {
        if (ShouldUseTokaLayers(characterId, sprite))
        {
            SetTokaCharacterLayers(view, GetCurrentTokaBody(), sprite);
            return;
        }

        SetCharacterSprite(view, sprite);
    }

    private bool CharacterVisualMatches(CharacterView view, string characterId, Sprite sprite)
    {
        if (!ShouldUseTokaLayers(characterId, sprite))
        {
            return !view.UsesTokaLayers && view.Image.sprite == sprite;
        }

        Sprite body = GetCurrentTokaBody();
        return view.UsesTokaLayers
            && view.TokaBodyImage.texture == GetSpriteTexture(body)
            && view.TokaFaceImage.texture == GetSpriteTexture(sprite);
    }

    private void SetCharacterSprite(CharacterView view, Sprite sprite)
    {
        view.UsesTokaLayers = false;
        view.TokaBodyImage.enabled = false;
        view.TokaBodyImage.texture = null;
        view.TokaFaceImage.enabled = false;
        view.TokaFaceImage.texture = null;
        view.Image.sprite = sprite;
        view.Image.enabled = true;
        ApplyCharacterTint(view);
        ResizeCharacterToSprite(view, sprite);
        SetCharacterPosition(view, view.XPosition);
    }

    private void SetTokaCharacterLayers(CharacterView view, Sprite body, Sprite face)
    {
        Texture bodyTexture = GetSpriteTexture(body);
        Texture faceTexture = GetSpriteTexture(face);

        view.UsesTokaLayers = true;
        view.Image.enabled = false;
        view.Image.sprite = null;
        view.TokaBodyImage.texture = bodyTexture;
        view.TokaBodyImage.enabled = bodyTexture != null;
        view.TokaFaceImage.texture = faceTexture;
        view.TokaFaceImage.enabled = faceTexture != null;
        ApplyCharacterTint(view);

        Texture referenceTexture = bodyTexture != null ? bodyTexture : faceTexture;
        ResizeCharacterToTexture(view, referenceTexture);
        SetCharacterPosition(view, view.XPosition);
    }

    private static Texture GetSpriteTexture(Sprite sprite)
    {
        return sprite != null ? sprite.texture : null;
    }

    private void ResizeCharacterToSprite(CharacterView view, Sprite sprite)
    {
        if (sprite == null)
        {
            return;
        }

        float stageHeight = GetStageHeight();
        float targetHeight = Mathf.Max(1f, stageHeight * characterHeightRatio);
        float targetWidth = targetHeight;

        if (sprite.rect.height > 0f)
        {
            targetWidth = sprite.rect.width / sprite.rect.height * targetHeight;
        }

        view.RectTransform.sizeDelta = new Vector2(targetWidth, targetHeight);
    }

    private void ResizeCharacterToTexture(CharacterView view, Texture texture)
    {
        if (texture == null)
        {
            return;
        }

        float stageHeight = GetStageHeight();
        float targetHeight = Mathf.Max(1f, stageHeight * characterHeightRatio);
        float targetWidth = texture.height > 0
            ? (float)texture.width / texture.height * targetHeight
            : targetHeight;

        view.RectTransform.sizeDelta = new Vector2(targetWidth, targetHeight);
    }

    private bool ShouldUseTokaLayers(string characterId, Sprite sprite)
    {
        return IsTokaCharacterId(characterId) && IsTokaFaceSprite(sprite);
    }

    private bool IsTokaFaceSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            return false;
        }

        string textureName = GetSpriteTexture(sprite)?.name;
        if (!string.IsNullOrWhiteSpace(textureName)
            && textureName.StartsWith(TokaFacePrefix, System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        TokaBodyList list = GetTokaBodyList();
        if (list == null)
        {
            return false;
        }

        if (UsesSameTexture(list.defaultFace, sprite))
        {
            return true;
        }

        foreach (Sprite face in list.faceList)
        {
            if (UsesSameTexture(face, sprite))
            {
                return true;
            }
        }

        return false;
    }

    private Sprite GetCurrentTokaBody()
    {
        Sprite currentBody = PlayerProfile.TokaCurrentBody;
        if (IsLayerableTokaBody(currentBody))
        {
            return currentBody;
        }

        TokaBodyList list = GetTokaBodyList();
        if (list == null)
        {
            return currentBody;
        }

        Sprite matchingBody = FindMatchingTokaBody(list, currentBody);
        if (matchingBody != null)
        {
            return matchingBody;
        }

        if (IsLayerableTokaBody(list.defaultSprite))
        {
            return list.defaultSprite;
        }

        foreach (Sprite body in list.spriteList)
        {
            if (IsLayerableTokaBody(body))
            {
                return body;
            }
        }

        return currentBody;
    }

    private TokaBodyList GetTokaBodyList()
    {
        if (tokaBodyList == null)
        {
            tokaBodyList = Resources.Load<TokaBodyList>(TokaBodyResource);
        }

        return tokaBodyList;
    }

    private static Sprite FindMatchingTokaBody(TokaBodyList list, Sprite currentBody)
    {
        string currentName = GetSpriteTexture(currentBody)?.name;
        if (string.IsNullOrWhiteSpace(currentName))
        {
            return null;
        }

        const string tokaPrefix = "Ch_Toka_";
        int suffixStart = currentName.StartsWith(tokaPrefix, System.StringComparison.OrdinalIgnoreCase)
            ? tokaPrefix.Length
            : -1;
        int suffixEnd = suffixStart >= 0 ? currentName.IndexOf('_', suffixStart) : -1;
        if (suffixStart < 0 || suffixEnd <= suffixStart)
        {
            return null;
        }

        string appearance = currentName[suffixStart..suffixEnd];
        string expectedName = TokaBodyPrefix + appearance;
        foreach (Sprite body in list.spriteList)
        {
            if (string.Equals(GetSpriteTexture(body)?.name, expectedName, System.StringComparison.OrdinalIgnoreCase))
            {
                return body;
            }
        }

        return null;
    }

    private static bool IsLayerableTokaBody(Sprite sprite)
    {
        return GetSpriteTexture(sprite)?.name.StartsWith(TokaBodyPrefix, System.StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool UsesSameTexture(Sprite first, Sprite second)
    {
        return first != null && second != null && GetSpriteTexture(first) == GetSpriteTexture(second);
    }

    private static bool IsTokaCharacterId(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return false;
        }

        string id = characterId.Trim();
        return string.Equals(id, "toka", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(id, "momoka", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(id, "白崎桃香", System.StringComparison.Ordinal);
    }

    private void SetCharacterPosition(CharacterView view, float normalizedPosition)
    {
        normalizedPosition = Mathf.Clamp01(normalizedPosition);
        view.XPosition = normalizedPosition;

        float stageWidth = GetStageWidth();
        float characterWidth = view.RectTransform.sizeDelta.x * Mathf.Max(0.01f, view.DisplayScale);
        float left = characterWidth * view.RectTransform.pivot.x - CharacterHorizontalOverscan;
        float right = stageWidth - characterWidth * (1f - view.RectTransform.pivot.x) + CharacterHorizontalOverscan;

        float x = left <= right
            ? Mathf.Lerp(left, right, normalizedPosition)
            : Mathf.Lerp(-CharacterHorizontalOverscan, stageWidth + CharacterHorizontalOverscan, normalizedPosition);

        view.RectTransform.anchoredPosition = new Vector2(x, bottomOffset);
    }

    private IEnumerator MoveCharacterTo(CharacterView view, float normalizedPosition, float duration)
    {
        Vector2 startPosition = view.RectTransform.anchoredPosition;
        SetCharacterPosition(view, normalizedPosition);
        Vector2 endPosition = view.RectTransform.anchoredPosition;
        view.RectTransform.anchoredPosition = startPosition;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            view.RectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        view.RectTransform.anchoredPosition = endPosition;
    }

    private void SetCharacterFlip(CharacterView view, bool flipped)
    {
        view.Flipped = flipped;
        ApplyCharacterScale(view);
        SetCharacterPosition(view, view.XPosition);
    }

    private void SetCharacterScale(CharacterView view, float displayScale)
    {
        view.DisplayScale = Mathf.Max(0.01f, displayScale);
        ApplyCharacterScale(view);
        SetCharacterPosition(view, view.XPosition);
    }

    private void ApplyCharacterScale(CharacterView view)
    {
        float displayScale = Mathf.Max(0.01f, view.DisplayScale);
        float xScale = view.Flipped ? -displayScale : displayScale;
        view.RectTransform.localScale = new Vector3(xScale, displayScale, 1f);
    }

    private IEnumerator ScaleCharacterTo(CharacterView view, float targetScale, float duration)
    {
        float startScale = view.DisplayScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            view.DisplayScale = Mathf.Lerp(startScale, targetScale, progress);
            ApplyCharacterScale(view);
            SetCharacterPosition(view, view.XPosition);
            yield return null;
        }

        SetCharacterScale(view, targetScale);
    }

    private void SetCharacterTint(CharacterView view, Color tintColor)
    {
        view.TintColor = tintColor;
        ApplyCharacterTint(view);
    }

    private void ApplyCharacterTint(CharacterView view)
    {
        bool hasNamedSpeaker = !string.IsNullOrWhiteSpace(speakingCharacterId);
        bool isSpeaking = hasNamedSpeaker
            && (string.Equals(view.CharacterId, speakingCharacterId, System.StringComparison.OrdinalIgnoreCase)
                || IsTokaCharacterId(view.CharacterId) && IsTokaCharacterId(speakingCharacterId));
        Color focusColor = !hasNamedSpeaker || isSpeaking ? Color.white : nonSpeakerColor;
        Color baseColor = view.TintColor;

        Color displayColor = new Color(
            baseColor.r * focusColor.r,
            baseColor.g * focusColor.g,
            baseColor.b * focusColor.b,
            baseColor.a * focusColor.a
        );

        view.Image.color = displayColor;
        view.TokaBodyImage.color = displayColor;
        view.TokaFaceImage.color = displayColor;
    }

    private IEnumerator TintCharacterTo(CharacterView view, Color targetColor, float duration)
    {
        Color startColor = view.TintColor;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Color color = Color.Lerp(startColor, targetColor, Mathf.Clamp01(elapsed / duration));
            SetCharacterTint(view, color);
            yield return null;
        }

        SetCharacterTint(view, targetColor);
    }

    private IEnumerator FadeCharacter(CharacterView view, float targetAlpha, float duration)
    {
        if (duration <= 0f)
        {
            view.CanvasGroup.alpha = targetAlpha;
            yield break;
        }

        float startAlpha = view.CanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            view.CanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        view.CanvasGroup.alpha = targetAlpha;
    }

    private Sprite FindCharacterSprite(string spriteName)
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

    private float ParseNormalizedPosition(string value)
    {
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float position))
        {
            return Mathf.Clamp01(position);
        }

        Debug.LogWarning($"Invalid character position '{value}'. Using center.");
        return 0.5f;
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

        Debug.LogWarning($"Invalid character fade time '{value}'. Using instant transition.");
        return 0f;
    }

    private bool ParseFlip(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "true":
            case "flip":
            case "flipped":
            case "left":
                return true;
            case "false":
            case "normal":
            case "none":
            case "right":
                return false;
            default:
                Debug.LogWarning($"Invalid character flip value '{value}'. Using normal direction.");
                return false;
        }
    }

    private float ParseScale(string value)
    {
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float scale))
        {
            return Mathf.Max(0.01f, scale);
        }

        Debug.LogWarning($"Invalid character scale value '{value}'. Using 1.");
        return 1f;
    }

    private Color ParseColor(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Color.white;
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

                Debug.LogWarning($"Invalid character tint color '{value}'. Using white.");
                return Color.white;
        }
    }

    private string GetArg(string[] args, int index, string fallback)
    {
        return args != null && index >= 0 && index < args.Length
            ? args[index]
            : fallback;
    }

    private bool TryReadArg(string[] args, int index, string action, string label, out string value)
    {
        value = GetArg(args, index, string.Empty);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        Debug.LogWarning($"Character '{action}' command needs a {label}.");
        return false;
    }

    private float GetStageWidth()
    {
        float width = characterRoot != null ? characterRoot.rect.width : 0f;
        if (width > 0f)
        {
            return width;
        }

        return Screen.width > 0 ? Screen.width : 1920f;
    }

    private float GetStageHeight()
    {
        float height = characterRoot != null ? characterRoot.rect.height : 0f;
        if (height > 0f)
        {
            return height;
        }

        return Screen.height > 0 ? Screen.height : 1080f;
    }

    private void BuildSpriteLookup()
    {
        spriteLookup.Clear();

        foreach (Sprite sprite in characterSprites)
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
        if (sprite == null || characterSprites.Contains(sprite))
        {
            return;
        }

        characterSprites.Add(sprite);
    }

    [ContextMenu("Refresh Character Sprite Cache")]
    private void CacheCharacterSprites()
    {
#if UNITY_EDITOR
        string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { CharacterFolder });
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

        characterSprites = sprites;
#endif
    }

#if UNITY_EDITOR
    private static Sprite FindSpriteInAssetDatabase(string spriteName)
    {
        string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { CharacterFolder });
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

    private class CharacterView
    {
        public CharacterView(
            string characterId,
            GameObject gameObject,
            RectTransform rectTransform,
            Image image,
            RawImage tokaBodyImage,
            RawImage tokaFaceImage,
            CanvasGroup canvasGroup
        )
        {
            CharacterId = characterId;
            GameObject = gameObject;
            RectTransform = rectTransform;
            Image = image;
            TokaBodyImage = tokaBodyImage;
            TokaFaceImage = tokaFaceImage;
            CanvasGroup = canvasGroup;
        }

        public string CharacterId { get; }
        public GameObject GameObject { get; }
        public RectTransform RectTransform { get; }
        public Image Image { get; }
        public RawImage TokaBodyImage { get; }
        public RawImage TokaFaceImage { get; }
        public CanvasGroup CanvasGroup { get; }
        public float XPosition { get; set; }
        public bool Flipped { get; set; }
        public float DisplayScale { get; set; } = 1f;
        public Color TintColor { get; set; } = Color.white;
        public bool UsesTokaLayers { get; set; }
    }
}
