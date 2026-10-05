using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using Assets.SimpleLocalization.Scripts;
using Unity.VisualScripting;

public class MainMenuTurorial : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform tutorialtextRect;
    [SerializeField] private Image tutorialDialogueNextArrow;
    [SerializeField] private TMP_Text tutorialText;
    [SerializeField] private CanvasGroup canvasGrp;
    [SerializeField] private Image exampleGraphic;


    [Header("References for copy")]
    [SerializeField] private Image tohka;
    [SerializeField] private GameObject playerProfile;
    [SerializeField] private GameObject actionBtn;
    [SerializeField] private GameObject actionPanel;

    [Header("For Debug")]
    [SerializeField] private int currentStep = 0;   
    [SerializeField] private bool isCurrentStepSkippable = false;   
    [SerializeField] private GameObject copiedObject;   

    public void StartTutorial()
    {
        gameObject.SetActive(true);
        currentStep = 0;

        GoNextStep();
    }

    private void EndTutorial()
    {
        PlayerProfile.SetTutorialTriggered(Tutorials.MainMenu, true);
        gameObject.SetActive(false);
    }

    private void GoNextStep()
    {
        isCurrentStepSkippable = false;
        tutorialDialogueNextArrow.color = new Color(1, 1, 1, 0);
        tutorialText.text = string.Empty;

        switch (currentStep)
        {
            case 0:
            default:
                {
                    DOTween.Sequence()
                           .Append(canvasGrp.DOFade(1.0f, 1.0f))
                           .AppendCallback(() => GoNextStep());
                }
                break;
            case 1:
                {
                    DOTween.Sequence()
                           .Append(tutorialtextRect.DOAnchorPos(new Vector2(21.2455f, 0.0f), 0.25f))
                           .Join(tutorialtextRect.DOSizeDelta(new Vector2(1538.019f, 100.0f), 0.25f))
                           .Append(tutorialText.DOText(LocalizationManager.Localize("Tutorial.MainMenu-1"), 2.0f))
                           .Append(tutorialDialogueNextArrow.rectTransform.DOAnchorPos(new Vector2(-40f, 0.0f), 0.0f))
                           .Append(tutorialDialogueNextArrow.DOFade(1.0f, 0.15f))
                           .AppendCallback(() => isCurrentStepSkippable = true);
                }
                break;
            case 2:
                {
                    DOTween.Sequence()
                           .Append(tutorialtextRect.DOAnchorPos(new Vector2(21.2455f, 0.0f), 0.25f))
                           .Join(tutorialtextRect.DOSizeDelta(new Vector2(0.0f, 100.0f), 0.25f))
                           .JoinCallback(() => exampleGraphic.enabled = true)
                           .JoinCallback(() => CopyImage(exampleGraphic, tohka))
                           .JoinCallback(() => exampleGraphic.color = new Color(0.75f, 0.75f, 0.75f, 1.0f))
                           .Append(exampleGraphic.DOColor(Color.white, 1.0f))
                           .Append(tutorialText.DOText(LocalizationManager.Localize("Tutorial.MainMenu-2"), 2.0f))
                           .Join(tutorialtextRect.DOSizeDelta(new Vector2(1125.0f, 100.0f), 0.25f))
                           .Append(tutorialDialogueNextArrow.rectTransform.DOAnchorPos(new Vector2(-40f, 0.0f), 0.0f))
                           .Append(tutorialDialogueNextArrow.DOFade(1.0f, 0.15f))
                           .AppendCallback(() => isCurrentStepSkippable = true);
                }
                break;
            case 3:
                {
                    exampleGraphic.DOFade(0.0f, 0.50f);
                    copiedObject = CopyObjectWithoutButtons(playerProfile, transform);
                    var canvasGroup = copiedObject.AddComponent<CanvasGroup>();
                    canvasGroup.alpha = 0.0f;
                    canvasGroup.blocksRaycasts = false;
                    
                    DOTween.Sequence()
                           .Append(tutorialtextRect.DOAnchorPos(new Vector2(-530.54f, 189.51f), 0.25f))
                           .Join(tutorialtextRect.DOSizeDelta(new Vector2(763f, 200.0f), 0.25f))
                           .Append(canvasGroup.DOFade(1.0f, 1.0f))
                           .Append(tutorialText.DOText(LocalizationManager.Localize("Tutorial.MainMenu-3"), 2.0f))
                           .Append(tutorialDialogueNextArrow.rectTransform.DOAnchorPos(new Vector2(-40f, 0.0f), 0.0f))
                           .Append(tutorialDialogueNextArrow.DOFade(1.0f, 0.15f))
                           .AppendCallback(() => isCurrentStepSkippable = true);
                }
                break;
            case 4:
                {
                    DOTween.Sequence()
                           .AppendCallback(() => DestroyObjectAfterFade(copiedObject, 1.0f))
                           .AppendCallback(() => copiedObject = CopyObjectWithoutButtons(actionBtn, transform))
                           .AppendInterval(1.0f)
                           .Append(tutorialtextRect.DOAnchorPos(new Vector2(-489.3f, -234.3f), 0.25f))                         // text box
                           .Join(tutorialtextRect.DOSizeDelta(new Vector2(763f, 200.0f), 0.25f))                                // text box
                           .Append(tutorialText.DOText(LocalizationManager.Localize("Tutorial.MainMenu-4"), 2.0f))              // text
                           .Append(tutorialDialogueNextArrow.rectTransform.DOAnchorPos(new Vector2(-40f, 0.0f), 0.0f))          // arrow animation
                           .Append(tutorialDialogueNextArrow.DOFade(1.0f, 0.15f))                                               // arrow animation
                           .AppendCallback(() => copiedObject.AddComponent<Button>().onClick.AddListener(OnClickNext))
                           .AppendCallback(() => isCurrentStepSkippable = false);                                               // is skippable
                }
                break;
            case 5:
                {
                    Destroy(copiedObject);
                    exampleGraphic.DOFade(0.0f, 1.0f);
                    copiedObject = CopyObjectWithoutButtons(actionPanel, transform);
                    copiedObject.GetComponent<ActionBoard>().enabled = false;

                    DOTween.Sequence()
                           .Append(copiedObject.GetComponent<CanvasGroup>().DOFade(1.0f, 1.0f))
                           .Append(tutorialtextRect.DOAnchorPos(new Vector2(0.0f, -260.5f), 0.25f))                         // text box
                           .Join(tutorialtextRect.DOSizeDelta(new Vector2(763f, 200.0f), 0.25f))                                // text box
                           .Append(tutorialText.DOText(LocalizationManager.Localize("Tutorial.MainMenu-6"), 2.0f))              // text
                           .Append(tutorialDialogueNextArrow.rectTransform.DOAnchorPos(new Vector2(-40f, 0.0f), 0.0f))          // arrow animation
                           .Append(tutorialDialogueNextArrow.DOFade(1.0f, 0.15f))                                               // arrow animation
                           .AppendCallback(() => isCurrentStepSkippable = true);                                               // is skippable
                }
                break;
            case 6:
                {
                    DOTween.Sequence()
                           .Append(tutorialtextRect.DOSizeDelta(new Vector2(810f, 200.0f), 0.25f))                                // text box
                           .Append(tutorialText.DOText(LocalizationManager.Localize("Tutorial.MainMenu-7"), 2.0f))              // text
                           .Append(tutorialDialogueNextArrow.rectTransform.DOAnchorPos(new Vector2(-40f, 0.0f), 0.0f))          // arrow animation
                           .Append(tutorialDialogueNextArrow.DOFade(1.0f, 0.15f))                                               // arrow animation
                           .AppendCallback(() => isCurrentStepSkippable = true);                                                // is skippable
                }
                break;
            case 7:
                {
                    DOTween.Sequence()
                           .AppendCallback(() => DestroyObjectAfterFade(copiedObject, 1.0f))
                           .AppendInterval(1.0f)
                           .Append(tutorialtextRect.DOAnchorPos(new Vector2(21.2455f, 0.0f), 0.25f))
                           .Join(tutorialtextRect.DOSizeDelta(new Vector2(1538.019f, 100.0f), 0.25f))
                           .Append(tutorialText.DOText(LocalizationManager.Localize("Tutorial.MainMenu-8"), 2.0f))              // text
                           .Append(tutorialDialogueNextArrow.rectTransform.DOAnchorPos(new Vector2(-40f, 0.0f), 0.0f))          // arrow animation
                           .Append(tutorialDialogueNextArrow.DOFade(1.0f, 0.15f))                                               // arrow animation
                           .AppendCallback(() => isCurrentStepSkippable = true);                                                // is skippable
                }
                break;
            case 8:
                {
                    // end of tutorial
                    isCurrentStepSkippable = false;
                    DOTween.Sequence()
                           .Append(tutorialText.DOFade(0.0f, 0.5f))
                           .Append(tutorialtextRect.DOSizeDelta(new Vector2(0.0f, 100.0f), 0.5f))
                           .Append(canvasGrp.DOFade(0.0f, 1.0f))
                           .AppendCallback(() => EndTutorial());
                }
                break;
        }

        currentStep++;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) 
        {
            if (isCurrentStepSkippable)
            {
                GoNextStep();
            }
        }
    }

    private void CopyImage(Image target, Image source)
    {
        target.sprite = source.sprite;
        CopyRectTransform(exampleGraphic.rectTransform, source.rectTransform);
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

        Button[] buttons = copy.GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            Destroy(button);
        }

        return copy;
    }

    public void DestroyObjectAfterFade(GameObject obj, float time)
    {
        var canvasGroup = obj.GetComponent<CanvasGroup>();
        DOTween.Sequence()
                           .Append(canvasGroup.DOFade(0.0f, time))
                           .AppendCallback(() => Destroy(obj));
    }

    public void OnClickNext()
    {
        GoNextStep();
    }
}
