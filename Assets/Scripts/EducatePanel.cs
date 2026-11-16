using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Assets.SimpleLocalization.Scripts;

public class EducatePanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Image[] arrows;
    [SerializeField] CanvasGroup[] buttons;
    [SerializeField] Button[] actionSlots;
    [SerializeField] Sprite filledActionSlotSprite;
    [SerializeField] Sprite emptyActionSlotSprite;
    [SerializeField] CanvasGroup categoryPanel;
    [SerializeField] TMP_Text categoryNameLabel;
    [SerializeField] Button educateBtn1;
    [SerializeField] Button educateBtn2;
    [SerializeField] Button educateBtn3;
    [SerializeField] TMP_Text educateBtn1_Label;
    [SerializeField] TMP_Text educateBtn2_Label;
    [SerializeField] TMP_Text educateBtn3_Label;
    [SerializeField] CanvasGroup notEnoughEnergy;
    [SerializeField] CanvasGroup CGDisplayer;
    [SerializeField] Image CGDisplayer_CG;
    [SerializeField] TMP_Text CGDisplayer_Dialog;
    [SerializeField] Button executeButton;
    [SerializeField] EducateTutorial educateTutorial;
    [SerializeField] GoingOutTutorial goingOutTutorial;

    [Header("CG presentation")]
    [SerializeField, Range(0f, 1f)] float cgBackgroundDarkness = 0.65f;
    [SerializeField, Min(0.01f)] float cgBackgroundFadeDuration = 0.75f;
    CanvasGroup cgBackgroundDimmer;
    float cgBackgroundTargetAlpha = -1f;

    const float FadeDuration = 0.5f;
    const int RequiredActionCount = 4;
    CanvasGroup canvasGroup;
    EducationCategoryButton currentSelectedCategory;
    readonly List<EducateOptionData> queuedActions = new List<EducateOptionData>(RequiredActionCount);
    readonly List<string> queuedActionLabels = new List<string>(RequiredActionCount);
    Image[] actionSlotImages;
    TMP_Text[] actionSlotLabels;
    string[] originalActionSlotTexts;
    Sprite[] originalActionSlotSprites;
    Coroutine openingRoutine;
    Coroutine executionRoutine;
    Tween panelFade;
    bool isOpen;
    bool isExecuting;
    public bool IsBusy => isOpen || isExecuting || IsTutorialRunning;
    bool IsTutorialRunning => (educateTutorial != null && educateTutorial.IsRunning)
        || (goingOutTutorial != null && goingOutTutorial.IsRunning);


    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        bool visible = canvasGroup.alpha > 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
        isOpen = visible;
        CacheActionSlotPresentation();
        if (executeButton != null)
            executeButton.onClick.AddListener(OnClickExecuteButton);
        RefreshActionSlots();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    public void DisplayUI(bool display)
    {
        DisplayUI(display, false);
    }

    public void DisplayUI(bool display, bool displayTutorial = false)
    {
        if (isExecuting || (display && IsTutorialRunning)) return;
        if (!display && educateTutorial != null) educateTutorial.gameObject.SetActive(false);
        isOpen = display;
        StopOpeningAnimation();
        queuedActions.Clear();
        queuedActionLabels.Clear();
        RefreshActionSlots();
        CloseCategory();
        HideCG();
        OnPressBackOnNotEnoughEnergyBtn();
        if (display) Init();

        canvasGroup.DOKill();
        canvasGroup.interactable = display;
        canvasGroup.blocksRaycasts = display;
        panelFade = canvasGroup.DOFade(display ? 1f : 0f, FadeDuration);

        // All opening paths show the tutorial once; the legacy displayTutorial argument stays compatible.
    }

    private void Init()
    {
        openingRoutine = StartCoroutine(OpenAnimation());
    }

    IEnumerator OpenAnimation()
    {
        float animationTime = 0.5f;
        foreach (Image arrow in arrows)
        {
            float originalHeight = arrow.rectTransform.rect.height;
            arrow.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0.0f);
            arrow.rectTransform.DOSizeDelta(new Vector2(arrow.rectTransform.rect.width, originalHeight), animationTime, false);
        }
        foreach (CanvasGroup button in buttons)
        {
            button.alpha = 0.0f;
            button.blocksRaycasts = false;
            button.interactable = false;
        }

        yield return new WaitForSeconds(animationTime);

        foreach (CanvasGroup button in buttons)
        {
            button.DOFade(1.0f, animationTime);
        }

        yield return new WaitForSeconds(animationTime);

        foreach (CanvasGroup button in buttons)
        {
            button.alpha = 1.0f;
            button.blocksRaycasts = true;
            button.interactable = true;
        }
        openingRoutine = null;
        if (isOpen && !PlayerProfile.GetTutorialTriggered(Tutorials.Educate))
        {
            if (educateTutorial != null)
            {
                educateTutorial.transform.SetAsLastSibling();
                educateTutorial.StartTutorial();
            }
            else Debug.LogWarning("Assign Educate Tutorial to show the first-opening education tutorial.", this);
        }
    }

    public void ButtonPressed(EducationCategoryButton data)
    {
        if (!isOpen || isExecuting || IsTutorialRunning || data == null) return;
        OpenCategory(LocalizationManager.Localize(data.categoryNameID));

        currentSelectedCategory = data;
        if (PlayerProfile.HasAvailableEducateOption(data.education[0]))
        {
            educateBtn1.interactable = true;
            educateBtn1_Label.text = LocalizationManager.Localize(data.education[0].nameID);
        }
        else
        {
            educateBtn1.interactable = false;
            educateBtn1_Label.text = LocalizationManager.Localize("Educate.Unknown");
        }

        if (false/*PlayerProfile.HasAvailableEducateOption(data.education[1])*/)
        {
            educateBtn2.interactable = true;
            educateBtn2_Label.text = LocalizationManager.Localize(data.education[1].nameID);
        }
        else
        {
            educateBtn2.interactable = false;
            educateBtn2_Label.text = LocalizationManager.Localize("Educate.Unknown");
        }

        if (false/*PlayerProfile.HasAvailableEducateOption(data.education[2]) */)
        {
            educateBtn3.interactable = true;
            educateBtn3_Label.text = LocalizationManager.Localize(data.education[2].nameID);
        }
        else
        {
            educateBtn3.interactable = false;
            educateBtn3_Label.text = LocalizationManager.Localize("Educate.Unknown");
        }
    }

    public void OpenCategory(string name)
    {
        if (!isOpen || isExecuting || IsTutorialRunning) return;
        categoryNameLabel.text = name;
        categoryPanel.DOKill();
        categoryPanel.interactable = true;
        categoryPanel.blocksRaycasts = true;
        categoryPanel.DOFade(true ? 1f : 0f, FadeDuration);
    }

    public void CloseCategory()
    {
        currentSelectedCategory = null;

        categoryPanel.DOKill();
        categoryPanel.interactable = false;
        categoryPanel.blocksRaycasts = false;
        categoryPanel.DOFade(false ? 1f : 0f, FadeDuration);
    }

    public void OnPressEducationOptionButton(int index)
    {
        if (!isOpen || isExecuting || IsTutorialRunning || queuedActions.Count >= RequiredActionCount
            || currentSelectedCategory == null || currentSelectedCategory.education == null
            || index < 0 || index >= currentSelectedCategory.education.Length)
            return;

        Button optionButton = index == 0 ? educateBtn1 : index == 1 ? educateBtn2 : index == 2 ? educateBtn3 : null;
        EducateOptionData option = currentSelectedCategory.education[index];
        if (optionButton == null || !optionButton.interactable || option == null
            || !PlayerProfile.HasAvailableEducateOption(option))
            return;

        queuedActions.Add(option);
        TMP_Text optionLabel = index == 0 ? educateBtn1_Label : index == 1 ? educateBtn2_Label : educateBtn3_Label;
        queuedActionLabels.Add(optionLabel != null ? optionLabel.text : LocalizationManager.Localize(option.nameID));
        RefreshActionSlots();
    }

    public void OnPressBackOnNotEnoughEnergyBtn()
    {
        notEnoughEnergy.DOKill();
        notEnoughEnergy.interactable = false;
        notEnoughEnergy.blocksRaycasts = false;
        notEnoughEnergy.DOFade(0f, FadeDuration);
    }

    public void DisplayCG(EducateOptionData data)
    {
        if (!isOpen || isExecuting || IsTutorialRunning || data == null) return;
        isExecuting = true;
        StopOpeningAnimation();
        canvasGroup.interactable = false;
        RefreshActionSlots();
        executionRoutine = StartCoroutine(DisplaySingleCG(data));
    }

    IEnumerator DisplaySingleCG(EducateOptionData data)
    {
        yield return DisplayCGAnimate(data);
        executionRoutine = null;
        isExecuting = false;
        SetCGBackgroundDimmed(false);
        canvasGroup.interactable = isOpen;
        RefreshActionSlots();
    }

    IEnumerator DisplayCGAnimate(EducateOptionData data)
    {
        float CGDisplayTime = 3.0f;
        float textDisplayTime = 1.0f;

        // MainMenu authors this under categoryPanel. Closing that group would also hide every CG.
        // Keep presentation under the main panel instead, with its existing screen-space placement.
        if (CGDisplayer.transform.parent != transform)
            CGDisplayer.transform.SetParent(transform, true);
        SetCGBackgroundDimmed(true);
        CGDisplayer.transform.SetAsLastSibling();

        CGDisplayer_CG.sprite = data.image;
        CGDisplayer.DOKill();
        CGDisplayer.interactable = true;
        CGDisplayer.blocksRaycasts = true;
        CGDisplayer.DOFade(1f, FadeDuration);

        CGDisplayer_Dialog.text = string.Empty;
        CGDisplayer_Dialog.DOKill();
        CGDisplayer_Dialog.DOText("<indent=1em><line-indent=-1em>" + LocalizationManager.Localize(data.dialogueID), textDisplayTime).SetEase(Ease.Linear);

        yield return new WaitForSeconds(CGDisplayTime);

        CGDisplayer.DOKill();
        yield return CGDisplayer.DOFade(0f, FadeDuration).WaitForCompletion();
        CGDisplayer.interactable = false;
        CGDisplayer.blocksRaycasts = false;
    }

    public void OnClickActionSlot (int slotIndex)
    {
        // Existing Inspector callbacks use 1 for the first slot through 4 for the last.
        int index = slotIndex - 1;
        if (!isOpen || isExecuting || IsTutorialRunning || index < 0 || index >= queuedActions.Count) return;
        queuedActions.RemoveAt(index);
        queuedActionLabels.RemoveAt(index);
        RefreshActionSlots();
    }

    public void OnClickExecuteButton()
    {
        if (!isOpen || isExecuting || IsTutorialRunning || queuedActions.Count != RequiredActionCount) return;
        if (CGDisplayer == null || CGDisplayer_CG == null || CGDisplayer_Dialog == null)
        {
            Debug.LogWarning("Assign the CG Displayer, image and dialog before executing education actions.", this);
            return;
        }

        EducateOptionData[] actions = queuedActions.ToArray();
        int[] costs = new int[RequiredActionCount];
        long totalCost = 0;
        for (int i = 0; i < actions.Length; i++)
        {
            if (actions[i] == null || actions[i].energyCost < 0 || !PlayerProfile.HasAvailableEducateOption(actions[i]))
            {
                Debug.LogWarning("The education queue contains an invalid or unavailable option. Re-select its slot.", this);
                return;
            }
            costs[i] = actions[i].energyCost;
            totalCost += costs[i];
        }
        if (totalCost > PlayerProfile.Energy)
        {
            ShowNotEnoughEnergy();
            return;
        }

        isExecuting = true;
        StopOpeningAnimation();
        CloseCategory();
        OnPressBackOnNotEnoughEnergyBtn();
        canvasGroup.interactable = false;
        RefreshActionSlots();
        executionRoutine = StartCoroutine(ExecuteActions(actions, costs));
    }

    IEnumerator ExecuteActions(EducateOptionData[] actions, int[] costs)
    {
        for (int i = 0; i < actions.Length; i++)
        {
            // Recheck in case another system/debugger changes energy during playback.
            if (PlayerProfile.Energy < costs[i])
            {
                queuedActions.RemoveRange(0, i);
                queuedActionLabels.RemoveRange(0, i);
                executionRoutine = null;
                isExecuting = false;
                SetCGBackgroundDimmed(false);
                canvasGroup.interactable = true;
                RefreshActionSlots();
                ShowNotEnoughEnergy();
                yield break;
            }
            PlayerProfile.Energy -= costs[i];
            yield return DisplayCGAnimate(actions[i]);
        }

        isExecuting = false;
        DisplayUI(false);
        // Keep the batch guarded and background input blocked until the panel is fully closed.
        isExecuting = true;
        canvasGroup.blocksRaycasts = true;
        yield return panelFade.WaitForCompletion();
        canvasGroup.blocksRaycasts = false;
        isExecuting = false;
        executionRoutine = null;

        if (!PlayerProfile.GetTutorialTriggered(Tutorials.GoingOut))
        {
            if (goingOutTutorial != null)
            {
                goingOutTutorial.transform.SetAsLastSibling();
                goingOutTutorial.StartTutorial();
            }
            else Debug.LogWarning("Assign Going Out Tutorial to show the post-education tutorial.", this);
        }
    }

    void ShowNotEnoughEnergy()
    {
        notEnoughEnergy.DOKill();
        notEnoughEnergy.interactable = true;
        notEnoughEnergy.blocksRaycasts = true;
        notEnoughEnergy.DOFade(1f, FadeDuration);
    }

    void CacheActionSlotPresentation()
    {
        actionSlotImages = new Image[actionSlots.Length];
        actionSlotLabels = new TMP_Text[actionSlots.Length];
        originalActionSlotTexts = new string[actionSlots.Length];
        originalActionSlotSprites = new Sprite[actionSlots.Length];
        for (int i = 0; i < actionSlots.Length; i++)
        {
            if (actionSlots[i] == null) continue;
            Image image = actionSlots[i].targetGraphic as Image;
            if (image == null) image = actionSlots[i].GetComponent<Image>();
            actionSlotImages[i] = image;
            originalActionSlotSprites[i] = image != null ? image.sprite : null;
            TMP_Text label = actionSlots[i].GetComponentInChildren<TMP_Text>(true);
            actionSlotLabels[i] = label;
            originalActionSlotTexts[i] = label != null ? label.text : string.Empty;
        }
    }

    void RefreshActionSlots()
    {
        for (int i = 0; i < actionSlots.Length; i++)
        {
            bool filled = i < queuedActions.Count;
            if (actionSlots[i] != null)
                actionSlots[i].interactable = isOpen && !isExecuting && filled;
            if (actionSlotImages != null && actionSlotImages[i] != null)
            {
                Sprite sprite = filled ? filledActionSlotSprite : emptyActionSlotSprite;
                if (sprite == null) sprite = originalActionSlotSprites[i];
                // Keep disabled playback slots filled even if the Button uses Sprite Swap.
                SpriteState spriteState = actionSlots[i].spriteState;
                spriteState.disabledSprite = sprite;
                actionSlots[i].spriteState = spriteState;
                actionSlotImages[i].overrideSprite = null;
                actionSlotImages[i].sprite = sprite;
            }
            if (actionSlotLabels != null && actionSlotLabels[i] != null)
                actionSlotLabels[i].text = filled ? queuedActionLabels[i] : originalActionSlotTexts[i];
        }
        if (executeButton != null)
            executeButton.interactable = isOpen && !isExecuting && queuedActions.Count == RequiredActionCount;
    }

    void StopOpeningAnimation()
    {
        if (openingRoutine != null) StopCoroutine(openingRoutine);
        openingRoutine = null;
        foreach (Image arrow in arrows) arrow.rectTransform.DOKill();
        foreach (CanvasGroup button in buttons) button.DOKill();
    }

    void SetCGBackgroundDimmed(bool dimmed, bool instant = false)
    {
        if (cgBackgroundDimmer == null && dimmed)
        {
            var overlay = new GameObject("CG Background Dimmer", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            overlay.layer = gameObject.layer;
            overlay.transform.SetParent(transform, false);
            Image image = overlay.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = true;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            cgBackgroundDimmer = overlay.GetComponent<CanvasGroup>();
            cgBackgroundDimmer.alpha = 0f;
            cgBackgroundDimmer.interactable = false;
            cgBackgroundTargetAlpha = -1f;
        }
        if (cgBackgroundDimmer == null) return;

        if (dimmed) cgBackgroundDimmer.transform.SetAsLastSibling();
        cgBackgroundDimmer.blocksRaycasts = dimmed;
        float alpha = dimmed ? Mathf.Clamp01(cgBackgroundDarkness) : 0f;
        // Repeated CGs keep the current fade/opacity instead of restarting the transition.
        if (!instant && Mathf.Approximately(cgBackgroundTargetAlpha, alpha)) return;
        cgBackgroundTargetAlpha = alpha;
        cgBackgroundDimmer.DOKill();
        if (instant) cgBackgroundDimmer.alpha = alpha;
        else cgBackgroundDimmer.DOFade(alpha, Mathf.Max(0.01f, cgBackgroundFadeDuration))
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
    }

    void HideCG(bool instant = false)
    {
        SetCGBackgroundDimmed(false, instant);
        if (CGDisplayer != null)
        {
            CGDisplayer.DOKill();
            CGDisplayer.alpha = 0f;
            CGDisplayer.interactable = false;
            CGDisplayer.blocksRaycasts = false;
        }
        if (CGDisplayer_Dialog != null) CGDisplayer_Dialog.DOKill();
    }

    void OnDisable()
    {
        if (educateTutorial != null) educateTutorial.gameObject.SetActive(false);
        StopOpeningAnimation();
        if (executionRoutine != null) StopCoroutine(executionRoutine);
        executionRoutine = null;
        isExecuting = false;
        isOpen = false;
        queuedActions.Clear();
        queuedActionLabels.Clear();
        HideCG(true);
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        RefreshActionSlots();
    }

    void OnDestroy()
    {
        if (executeButton != null) executeButton.onClick.RemoveListener(OnClickExecuteButton);
    }
}
