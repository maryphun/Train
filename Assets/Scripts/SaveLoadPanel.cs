using UnityEngine;
using DG.Tweening;

public class SaveLoadPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGrp;
    [SerializeField] private SaveSlot[] saveSlots;
    [SerializeField] private bool isSavePanel; // false means this is load panel.

    public void DisplayUI(bool display)
    {
        canvasGrp.DOKill();
        canvasGrp.interactable = display;
        canvasGrp.blocksRaycasts = display;
        canvasGrp.DOFade(display ? 1f : 0f, 0.5f);

        if (display)
        {
            InitiateUI();
        }
    }

    public void InitiateUI()
    {
        for (int i = 0; i < saveSlots.Length; i++)
        {
            saveSlots[i].Init(i, isSavePanel);
        }
    }
}
