using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class BattlerHirePanel : MonoBehaviour
{
    const float FadeDuration = 0.5f;
    const float ModalSpacing = 24f;

    [Header("References")]
    [SerializeField] BattlerSelectionModal originModal;
    [SerializeField] GameObject battlerUnavailableLabel;
    [SerializeField] TMP_Text battlepoint_display;

    readonly List<BattlerSelectionModal> spawnedModals = new List<BattlerSelectionModal>();
    CanvasGroup canvasGroup;
    RectTransform viewport;
    RectTransform content;
    ScrollRect scrollRect;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        Init();

        bool visible = canvasGroup.alpha > 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;

        battlerUnavailableLabel.SetActive(false);
    }

    public void DisplayUI(bool display)
    {
        if (display)
            RebuildModals();

        canvasGroup.DOKill();
        canvasGroup.interactable = display;
        canvasGroup.blocksRaycasts = display;
        canvasGroup.DOFade(display ? 1f : 0f, FadeDuration);
    }

    void Init()
    {
        if (originModal == null)
            originModal = GetComponentInChildren<BattlerSelectionModal>(true);

        if (originModal == null)
        {
            Debug.LogError("BattlerHirePanel needs an origin BattlerSelectionModal.", this);
            return;
        }

        viewport = originModal.transform.parent as RectTransform;
        scrollRect = viewport != null ? viewport.GetComponentInParent<ScrollRect>() : null;
        content = scrollRect != null ? scrollRect.content : null;

        if (viewport == null || content == null || content.parent != viewport)
            Debug.LogError("BattlerHirePanel needs a Scroll View Content child of the origin modal's Viewport.", this);
    }

    void RebuildModals()
    {
        foreach (BattlerSelectionModal modal in spawnedModals)
        {
            if (modal == null)
                continue;

            modal.gameObject.SetActive(false);
            Destroy(modal.gameObject);
        }
        spawnedModals.Clear();

        if (originModal == null || viewport == null || content == null || content.parent != viewport)
            return;

        content.gameObject.SetActive(true);
        scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        var dataById = new Dictionary<string, BattlerData>();
        foreach (BattlerData data in Resources.LoadAll<BattlerData>("BattlerData"))
        {
            if (data != null && !string.IsNullOrEmpty(data.BattlerID))
                dataById[data.BattlerID] = data;
        }

        RectTransform originRect = originModal.GetComponent<RectTransform>();
        float modalWidth = originRect.rect.width;
        float leftMargin = viewport.rect.width * originRect.anchorMin.x
            + originRect.anchoredPosition.x - modalWidth * originRect.pivot.x;
        int position = 0;

        foreach (AvailableBattlerRecord record in PlayerProfile.AvailableBattlerData)
        {
            if (record == null || string.IsNullOrEmpty(record.BattlerID)
                || !dataById.TryGetValue(record.BattlerID, out BattlerData data))
            {
                Debug.LogWarning($"No BattlerData found for available battler '{record?.BattlerID}'.", this);
                continue;
            }

            BattlerSelectionModal modal = Instantiate(originModal, content, false);
            RectTransform modalRect = modal.GetComponent<RectTransform>();
            modalRect.anchorMin = new Vector2(0f, 0.5f);
            modalRect.anchorMax = new Vector2(0f, 0.5f);
            modalRect.anchoredPosition = new Vector2(
                leftMargin + modalWidth * modalRect.pivot.x + position * (modalWidth + ModalSpacing),
                originRect.anchoredPosition.y);
            modal.Initialization(data, record.BattlerCurrentLevel);
            modal.gameObject.SetActive(true);
            spawnedModals.Add(modal);
            position++;
        }

        float rowWidth = position == 0 ? 0f
            : leftMargin * 2f + position * modalWidth + (position - 1) * ModalSpacing;
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
            Mathf.Max(viewport.rect.width, rowWidth));
        scrollRect.horizontalNormalizedPosition = 0f;
    }

    void OnDestroy()
    {
        if (canvasGroup != null)
            canvasGroup.DOKill();
    }
}
