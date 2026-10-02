using DG.Tweening;
using UnityEngine;

public class OutdoorMapPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGrp;

    public void DisplayUI(bool display)
    {
        canvasGrp.DOKill();
        canvasGrp.interactable = display;
        canvasGrp.blocksRaycasts = display;
        canvasGrp.DOFade(display ? 1f : 0f, 0.5f);
    }
}
