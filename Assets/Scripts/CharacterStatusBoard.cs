using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HoverActivator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform hoverImageRect; 
    [SerializeField] private RectTransform characterStatusBoard;
    [SerializeField] private float hoverSeconds = 0.5f;
    [SerializeField] private float initialMoveDistance = 100f;


    private CanvasGroup canvasGrp;

    private float timer;
    private bool activated;

    private void Start()
    {
        canvasGrp = characterStatusBoard.GetComponent<CanvasGroup>();
    }

    void Update()
    {
        bool mouseInside = RectTransformUtility.RectangleContainsScreenPoint(
            hoverImageRect,
            Input.mousePosition,
            null
        );

        if (mouseInside)
        {
            timer += Time.deltaTime;

            if (!activated && timer >= hoverSeconds)
            {
                this.Activate();
                activated = true;
            }
        }
        else
        {
            timer = 0f;

            if (activated)
            {
                this.Inactivate();
                activated = false;
            }
        }
    }

    void Activate()
    {
        canvasGrp.DOFade(1.0f, 0.25f);
        canvasGrp.blocksRaycasts = true;
        canvasGrp.interactable = true;

        characterStatusBoard.anchoredPosition = new Vector2(-initialMoveDistance, 0.0f);
        characterStatusBoard.DOAnchorPos(Vector2.zero, 0.25f);
    }

    void Inactivate()
    {
        canvasGrp.DOFade(0.0f, 0.25f);
        canvasGrp.blocksRaycasts = false;
        canvasGrp.interactable = false;

        characterStatusBoard.anchoredPosition = Vector2.zero;
        characterStatusBoard.DOAnchorPos(new Vector2(-initialMoveDistance, 0.0f), 0.25f);
    }
}