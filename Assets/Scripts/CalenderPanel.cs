using UnityEngine;
using DG.Tweening;


public class CalenderPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGrp;
    [SerializeField] private RectTransform calenderBtn;

    private Vector3 calenderBtnOriginalPosition;

    private void Start()
    {
        calenderBtnOriginalPosition = calenderBtn.anchoredPosition;
    }

    public void DisplayUI(bool display)
    {
        canvasGrp.DOKill();
        canvasGrp.interactable = display;
        canvasGrp.blocksRaycasts = display;
        canvasGrp.DOFade(display ? 1f : 0f, 0.5f);

        PlayCalenderAnimation(display);
    }

    private void PlayCalenderAnimation(bool isOpen)
    {
        if (isOpen)
        {
            calenderBtn.DOAnchorPos(Vector2.zero, 0.5f);
            calenderBtn.DOScale(new Vector3(2, 2, 1), 0.5f);
        }
        else
        {
            calenderBtn.DOAnchorPos(calenderBtnOriginalPosition, 0.5f, false);
            calenderBtn.DOScale(new Vector3(1, 1, 1), 0.5f);
        }
    }
}
