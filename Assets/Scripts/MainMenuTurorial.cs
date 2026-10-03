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

    [Header("For Debug")]
    [SerializeField] private int currentStep = 0;   
    [SerializeField] private bool isCurrentStepSkippable = false;   

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
        tutorialDialogueNextArrow.color = new Color(1, 1, 1, 0);

        switch (currentStep)
        {
            case 0:
            default:
                {
                    isCurrentStepSkippable = false;
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
}
