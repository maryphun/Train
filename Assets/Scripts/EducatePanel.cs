using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using Assets.SimpleLocalization.Scripts;

public class EducatePanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Image[] arrows;
    [SerializeField] CanvasGroup[] buttons;
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

    const float FadeDuration = 0.5f;
    CanvasGroup canvasGroup;
    EducationCategoryButton currentSelectedCategory;


    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        bool visible = canvasGroup.alpha > 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    public void DisplayUI(bool display)
    {
        if (display) Init();

        canvasGroup.DOKill();
        canvasGroup.interactable = display;
        canvasGroup.blocksRaycasts = display;
        canvasGroup.DOFade(display ? 1f : 0f, FadeDuration);
    }

    private void Init()
    {
        StartCoroutine(OpenAnimation());
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
    }

    public void ButtonPressed(EducationCategoryButton data)
    {
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
        if (PlayerProfile.Energy >= currentSelectedCategory.education[index].energyCost)
        {
            PlayerProfile.Energy -= currentSelectedCategory.education[index].energyCost;
            DisplayCG(currentSelectedCategory.education[index]);
        }
        else
        {
            // not enough energy. show UI
            notEnoughEnergy.DOKill();
            notEnoughEnergy.interactable = true;
            notEnoughEnergy.blocksRaycasts = true;
            notEnoughEnergy.DOFade(1f, FadeDuration);
        }
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
        StartCoroutine(DisplayCGAnimate(data));
    }


    IEnumerator DisplayCGAnimate(EducateOptionData data)
    {
        float CGDisplayTime = 3.0f;
        float textDisplayTime = 1.0f;

        CGDisplayer_CG.sprite = data.image;
        CGDisplayer.DOKill();
        CGDisplayer.interactable = true;
        CGDisplayer.blocksRaycasts = true;
        CGDisplayer.DOFade(1f, FadeDuration);

        CGDisplayer_Dialog.text = string.Empty;
        CGDisplayer_Dialog.DOText("<indent=1em><line-indent=-1em>" + LocalizationManager.Localize(data.dialogueID), textDisplayTime).SetEase(Ease.Linear);

        yield return new WaitForSeconds(CGDisplayTime);

        CGDisplayer.DOKill();
        CGDisplayer.interactable = false;
        CGDisplayer.blocksRaycasts = false;
        CGDisplayer.DOFade(0f, FadeDuration);
    }
}
