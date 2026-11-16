using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using Assets.SimpleLocalization.Scripts;

public class MainMenuTurorial : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform tutorialtextRect;
    [SerializeField] private Image tutorialDialogueNextArrow;
    [SerializeField] private TMP_Text tutorialText;
    [SerializeField] private CanvasGroup canvasGrp;
    [SerializeField] private Image exampleGraphic;
    [SerializeField] private Image tetora;
    [SerializeField] private GameObject toka;

    [Header("Centered text layout")]
    [SerializeField, Range(0.1f, 1f)] private float maximumWidthRatio = 0.8f;
    [SerializeField] private Vector2 textBoxPadding = new Vector2(80f, 50f);
    [SerializeField, Min(0f)] private float nextArrowTextGap = 20f;
    [SerializeField] private Vector2 nextArrowCornerInset = new Vector2(12f, 12f);
    [SerializeField, Min(0f)] private float minimumTextBoxHeight = 100f;
    [SerializeField] private bool highlightProtagonistThroughout;

    [Header("For Debug")]
    [SerializeField] private int currentStep;
    [SerializeField] private bool isCurrentStepSkippable;
    [SerializeField] private GameObject copiedObject;

    protected virtual int LastTextStep => 8;
    protected virtual string LocalizationPrefix => "Tutorial.MainMenu-";
    protected virtual Tutorials CompletionTutorial => Tutorials.MainMenu;
    protected virtual bool AllowProtagonistHighlight => true;
    protected virtual GameObject GetHighlightTarget(int step) =>
        AllowProtagonistHighlight && (step == 2 || highlightProtagonistThroughout) ? toka : null;
    public bool IsRunning => isRunning;
    private Sequence stepSequence;
    private bool isRunning;
    private bool isEnding;
    private int lastAdvanceFrame = -1;
    private bool isRevealingText;
    private string currentFullText;
    private Vector2 currentTextBoxSize;

    public void StartTutorial()
    {
        StopPresentation();
        ClearProtagonistHighlight();
        gameObject.SetActive(true);
        isRunning = true;
        isEnding = false;
        isRevealingText = false;
        currentStep = -1;
        lastAdvanceFrame = Time.frameCount;
        canvasGrp.alpha = 0f;
        canvasGrp.interactable = true;
        canvasGrp.blocksRaycasts = true;
        tutorialText.alpha = 1f;
        tutorialText.text = string.Empty;
        if (exampleGraphic != null) exampleGraphic.color = Color.clear;
        CenterTextBox();
        GoNextStep();
    }

    private void EndTutorial()
    {
        if (!isRunning) return;
        isRunning = false;
        isEnding = false;
        isCurrentStepSkippable = false;
        StopPresentation();
        ClearProtagonistHighlight();
        PlayerProfile.SetTutorialTriggered(CompletionTutorial, true);
        gameObject.SetActive(false);
        OnTutorialCompleted();
    }

    protected virtual void OnTutorialCompleted()
    {
        MainMenuManager manager = FindFirstObjectByType<MainMenuManager>();
        if (manager != null) manager.StartNewDay(false);
        else Debug.LogWarning("MainMenu tutorial finished without an active MainMenuManager.", this);
    }

    private void GoNextStep()
    {
        if (!isRunning) return;
        StopPresentation();
        currentStep++;
        isRevealingText = false;
        isCurrentStepSkippable = true;
        CenterTextBox();

        if (currentStep == 0)
        {
            tutorialDialogueNextArrow.color = Color.clear;
            stepSequence = DOTween.Sequence().SetUpdate(true);
            stepSequence.Append(canvasGrp.DOFade(1f, 1f))
                .AppendCallback(GoNextStep);
            return;
        }

        if (currentStep > LastTextStep)
        {
            isEnding = true;
            tutorialDialogueNextArrow.color = Color.clear;
            ClearProtagonistHighlight();
            stepSequence = DOTween.Sequence().SetUpdate(true);
            stepSequence.Append(canvasGrp.DOFade(0f, 1f))
                .AppendCallback(EndTutorial);
            return;
        }

        string text = LocalizationManager.Localize(LocalizationPrefix + currentStep);
        Vector2 size = MeasureTextBox(text);
        currentFullText = text;
        currentTextBoxSize = size;
        isRevealingText = true;
        UpdateProtagonistHighlight();
        tutorialText.alpha = 1f;
        tutorialText.text = string.Empty;
        tutorialDialogueNextArrow.color = Color.white;
        if (tetora != null) tetora.color = Color.white;
        stepSequence = DOTween.Sequence().SetUpdate(true);
        stepSequence.Append(canvasGrp.DOFade(1f, 0.25f))
            .Join(tutorialtextRect.DOSizeDelta(size, 0.25f))
            .Append(tutorialText.DOText(text, 2f).SetEase(Ease.Linear))
            .AppendCallback(() => isRevealingText = false);
    }

    private void CenterTextBox()
    {
        tutorialtextRect.anchorMin = new Vector2(0.5f, 0.5f);
        tutorialtextRect.anchorMax = tutorialtextRect.anchorMin;
        tutorialtextRect.pivot = tutorialtextRect.anchorMin;
        tutorialtextRect.anchoredPosition = Vector2.zero;
    }

    private Vector2 MeasureTextBox(string text)
    {
        RectTransform parent = tutorialtextRect.parent as RectTransform;
        float availableWidth = parent != null ? parent.rect.width : tutorialtextRect.rect.width;
        float paddingX = Mathf.Max(0f, textBoxPadding.x);
        float paddingY = Mathf.Max(0f, textBoxPadding.y);
        RectTransform arrowRect = tutorialDialogueNextArrow.rectTransform;
        float arrowSpace = arrowRect.rect.width + Mathf.Max(0f, nextArrowTextGap);
        float horizontalSpace = paddingX + arrowSpace;
        float maxWidth = Mathf.Max(horizontalSpace + 1f, availableWidth * Mathf.Clamp01(maximumWidthRatio));
        float width = Mathf.Clamp(tutorialText.GetPreferredValues(text).x + horizontalSpace, horizontalSpace + 1f, maxWidth);
        Vector2 preferred = tutorialText.GetPreferredValues(text, Mathf.Max(1f, width - horizontalSpace), Mathf.Infinity);
        float height = Mathf.Max(minimumTextBoxHeight, Mathf.Max(preferred.y, arrowRect.rect.height) + paddingY);

        // Give the triangle its own right-hand gutter, with balanced padding above/below the text.
        RectTransform textRect = tutorialText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(paddingX * 0.5f, paddingY * 0.5f);
        textRect.offsetMax = new Vector2(-paddingX * 0.5f - arrowSpace, -paddingY * 0.5f);
        arrowRect.anchorMin = new Vector2(1f, 0f);
        arrowRect.anchorMax = arrowRect.anchorMin;
        // Preserve the authored pivot (including Y = 0.5); inset the visible edges, not the pivot point.
        arrowRect.anchoredPosition = new Vector2(
            -Mathf.Max(0f, nextArrowCornerInset.x) - (1f - arrowRect.pivot.x) * arrowRect.rect.width,
            Mathf.Max(0f, nextArrowCornerInset.y) + arrowRect.pivot.y * arrowRect.rect.height);
        return new Vector2(width, height);
    }

    private void UpdateProtagonistHighlight()
    {
        GameObject target = GetHighlightTarget(currentStep);
        if (target == null)
        {
            ClearProtagonistHighlight();
            return;
        }
        if (copiedObject != null) return;
        copiedObject = CopyObjectWithoutButtons(target, transform);
        CanvasGroup group = copiedObject.GetComponent<CanvasGroup>();
        if (group == null) group = copiedObject.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void ClearProtagonistHighlight()
    {
        if (copiedObject == null) return;
        CanvasGroup group = copiedObject.GetComponent<CanvasGroup>();
        if (group != null) group.DOKill();
        copiedObject.SetActive(false);
        Destroy(copiedObject);
        copiedObject = null;
    }

    private void StopPresentation()
    {
        // A click kills old callbacks too, so a skipped step cannot advance later by itself.
        Sequence previous = stepSequence;
        stepSequence = null;
        previous?.Kill();
    }

    protected virtual void OnDisable()
    {
        StopPresentation();
        ClearProtagonistHighlight();
        isRunning = false;
        isEnding = false;
        isCurrentStepSkippable = false;
        isRevealingText = false;
        if (canvasGrp != null)
        {
            canvasGrp.alpha = 0f;
            canvasGrp.interactable = false;
            canvasGrp.blocksRaycasts = false;
        }
    }

    protected virtual void Update()
    {
        if (Input.GetMouseButtonDown(0)) OnClickNext();
    }

    public void OnClickNext()
    {
        if (!isRunning || !isCurrentStepSkippable || lastAdvanceFrame == Time.frameCount) return;
        lastAdvanceFrame = Time.frameCount;
        if (isRevealingText)
        {
            StopPresentation();
            tutorialText.text = currentFullText;
            tutorialtextRect.sizeDelta = currentTextBoxSize;
            canvasGrp.alpha = 1f;
            isRevealingText = false;
            return;
        }
        if (isEnding) EndTutorial();
        else GoNextStep();
    }

    public void CopyRectTransform(RectTransform target, RectTransform source)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.offsetMin = source.offsetMin;
        target.offsetMax = source.offsetMax;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
    }

    public GameObject CopyObjectWithoutButtons(GameObject source, Transform parent = null)
    {
        GameObject copy = Instantiate(source, parent);
        copy.transform.SetAsFirstSibling();
        foreach (Button button in copy.GetComponentsInChildren<Button>(true)) Destroy(button);
        return copy;
    }

    public void DestroyObjectAfterFade(GameObject obj, float time)
    {
        if (obj == null) return;
        CanvasGroup group = obj.GetComponent<CanvasGroup>();
        if (group == null) { Destroy(obj); return; }
        group.DOFade(0f, time).SetUpdate(true).OnComplete(() => Destroy(obj));
    }
}
