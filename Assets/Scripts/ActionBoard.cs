using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class ActionBoard : MonoBehaviour
{
    [Header("References")]
   // [SerializeField] private Canvas canvas;             // assign parent canvas


    CanvasGroup canvasGrp;
    RectTransform boardRect;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        boardRect = GetComponent<RectTransform>();

        // hide on default
        canvasGrp = GetComponent<CanvasGroup>();
        canvasGrp.alpha = 0.0f;
        canvasGrp.blocksRaycasts = false;
        canvasGrp.interactable = false;

        // stop Update on default
        enabled = false;
    }

    // Activate from button
    public void Activate()
    {
        canvasGrp.DOFade(1.0f, 0.25f);
        canvasGrp.blocksRaycasts = true;
        canvasGrp.interactable = true;

        enabled = true;
    }

    public void Inactivate()
    {
        canvasGrp.DOFade(0.0f, 0.25f);
        canvasGrp.blocksRaycasts = false;
        canvasGrp.interactable = false;

        enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        bool mouseInside = RectTransformUtility.RectangleContainsScreenPoint(boardRect, Input.mousePosition, null);

        if (!mouseInside)
        {
            Inactivate();
        }
    }
}
