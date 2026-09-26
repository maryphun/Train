using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class HoverActivator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform hoverImageRect; 
    [SerializeField] private RectTransform characterStatusBoard;
    [SerializeField] private float hoverSeconds = 0.5f;
    [SerializeField] private float initialMoveDistance = 100f;


    private CanvasGroup canvasGrp;
    private EventSystem pointerEventSystem;
    private PointerEventData pointerEventData;
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

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
        ) && IsUnblockedByUI();

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

    private bool IsUnblockedByUI()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || hoverImageRect == null)
            return false;

        if (pointerEventData == null || pointerEventSystem != eventSystem)
        {
            pointerEventSystem = eventSystem;
            pointerEventData = new PointerEventData(eventSystem);
        }

        pointerEventData.position = Input.mousePosition;
        raycastResults.Clear();
        eventSystem.RaycastAll(pointerEventData, raycastResults);

        if (raycastResults.Count == 0 || raycastResults[0].gameObject == null)
            return false;

        Transform topHit = raycastResults[0].gameObject.transform;
        if (topHit != hoverImageRect && !topHit.IsChildOf(hoverImageRect))
            return false;

        for (int i = 1; i < raycastResults.Count; i++)
        {
            GameObject hitObject = raycastResults[i].gameObject;
            if (hitObject != null && IsOtherUIBlocker(hitObject.transform))
                return false;
        }

        return true;
    }

    private static bool IsOtherUIBlocker(Transform hitTransform)
    {
        if (hitTransform.GetComponentInParent<Selectable>() != null)
            return true;

        for (Transform current = hitTransform; current != null; current = current.parent)
        {
            CanvasGroup group = current.GetComponent<CanvasGroup>();
            if (group != null && group.isActiveAndEnabled && group.blocksRaycasts)
                return true;
        }

        return false;
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
