using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections.Generic;

public class DressPanel : MonoBehaviour
{
    const float FadeDuration = 0.5f;

    [Header("References")]
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] DressChoiceModal[] dressChoiceModal;
    [SerializeField] Button ChangeDressButton;
    [SerializeField] TokaBodyList bodyList;
    [SerializeField] ScrollRect scrollRect;
    [SerializeField] Sprite unavailableIcon;

    [Header("Layout")]
    [SerializeField, Min(0f)] float modalGap = 24f;

    readonly List<DressChoiceModal> modals = new List<DressChoiceModal>();
    readonly List<DressChoiceModal> row = new List<DressChoiceModal>();
    DressChoiceModal selectedModal;
    bool isOpen;
    bool arranging;

    void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (scrollRect == null)
            scrollRect = GetComponentInChildren<ScrollRect>(true);
        if (bodyList == null)
            bodyList = Resources.Load<TokaBodyList>("TokaBodyList");
        DiscoverModals();
        RefreshSelection();
    }

    void OnEnable() => PlayerProfile.TokaBodyChanged += RefreshSelection;
    void OnDisable()
    {
        PlayerProfile.TokaBodyChanged -= RefreshSelection;
        if (canvasGroup != null)
            canvasGroup.DOKill();
        isOpen = false;
        if (ChangeDressButton != null)
            ChangeDressButton.interactable = false;
    }

    public void DisplayUI(bool display)
    {
        if (canvasGroup == null)
            return;
        isOpen = display;
        if (display)
            Init();
        else
            RefreshSelection();
        canvasGroup.DOKill();
        canvasGroup.interactable = display;
        canvasGroup.blocksRaycasts = display;
        canvasGroup.DOFade(display ? 1f : 0f, FadeDuration);
    }

    public void Init()
    {
        DiscoverModals();
        selectedModal = null;
        row.Clear();
        Sprite current = PlayerProfile.TokaCurrentBody;
        foreach (DressChoiceModal modal in modals)
        {
            bool available = IsAvailable(modal);
            modal.SetAvailable(available, unavailableIcon);
            modal.gameObject.SetActive(true);
            if (available && SameDress(modal.DressSprite, current) && selectedModal == null)
                selectedModal = modal;
            if (available)
                row.Add(modal);
        }
        foreach (DressChoiceModal modal in modals)
            if (!modal.IsAvailable)
                row.Add(modal);
        ArrangeModals();
        RefreshSelection();
    }

    public void OnClickChangeDressButton()
    {
        if (!isOpen || selectedModal == null || !IsAvailable(selectedModal)
            || SameDress(selectedModal.DressSprite, PlayerProfile.TokaCurrentBody))
        {
            RefreshSelection();
            return;
        }
        PlayerProfile.TokaCurrentBody = selectedModal.DressSprite;
        DisplayUI(false);
    }

    void DiscoverModals()
    {
        if (dressChoiceModal != null)
            foreach (DressChoiceModal modal in dressChoiceModal)
                RegisterModal(modal);
        foreach (DressChoiceModal modal in GetComponentsInChildren<DressChoiceModal>(true))
            RegisterModal(modal);
        modals.RemoveAll(modal => modal == null);
    }

    void RegisterModal(DressChoiceModal modal)
    {
        if (modal == null || modals.Contains(modal))
            return;
        modals.Add(modal);
        modal.Initialization();
        modal.Selected += SelectModal;
    }

    public void SelectModal(DressChoiceModal modal)
    {
        if (!isOpen || modal == null || !modals.Contains(modal) || !IsAvailable(modal))
            return;
        selectedModal = modal;
        RefreshSelection();
    }

    bool IsAvailable(DressChoiceModal modal)
    {
        if (modal == null || modal.DressSprite == null || bodyList == null || bodyList.spriteList == null)
            return false;
        for (int i = 0; i < bodyList.spriteList.Count; i++)
            if (SameDress(modal.DressSprite, bodyList.spriteList[i]))
                foreach (int id in PlayerProfile.TokaAvailableBody)
                    if (id == i)
                        return true;
        return false;
    }

    static bool SameDress(Sprite first, Sprite second) => first != null && second != null
        && string.Equals(first.name, second.name, System.StringComparison.Ordinal);

    void RefreshSelection()
    {
        // Hide all distinct/shared frames first, then show the selected frame once.
        foreach (DressChoiceModal modal in modals)
        {
            if (modal == null)
                continue;
            modal.SetSelected(modal == selectedModal);
            if (modal.SelectionFrame != null)
                modal.SelectionFrame.SetActive(false);
        }
        bool available = selectedModal != null && IsAvailable(selectedModal);
        if (available && selectedModal.SelectionFrame != null)
        {
            GameObject frame = selectedModal.SelectionFrame;
            RectTransform frameRect = frame.transform as RectTransform;
            if (frameRect != null)
            {
                frameRect.SetParent(selectedModal.transform, false);
                frameRect.anchorMin = Vector2.zero;
                frameRect.anchorMax = Vector2.one;
                frameRect.offsetMin = Vector2.zero;
                frameRect.offsetMax = Vector2.zero;
                frameRect.localScale = Vector3.one;
                frameRect.localRotation = Quaternion.identity;
                frameRect.SetAsLastSibling();
            }
            foreach (Graphic graphic in frame.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
            frame.SetActive(true);
        }
        if (ChangeDressButton != null)
            ChangeDressButton.interactable = isOpen && available
                && !SameDress(selectedModal.DressSprite, PlayerProfile.TokaCurrentBody);
    }

    void ArrangeModals()
    {
        if (arranging || scrollRect == null || scrollRect.content == null || scrollRect.viewport == null)
            return;
        arranging = true;
        try
        {
            RectTransform content = scrollRect.content;
            RectTransform viewport = scrollRect.viewport;
            content.gameObject.SetActive(true);
            scrollRect.StopMovement();
            float totalWidth = 0f;
            foreach (DressChoiceModal modal in row)
            {
                RectTransform rect = (RectTransform)modal.transform;
                totalWidth += rect.rect.width * Mathf.Abs(rect.localScale.x);
            }
            float gap = Mathf.Max(0f, modalGap);
            totalWidth += Mathf.Max(0, row.Count - 1) * gap;
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 0.5f);
            content.pivot = new Vector2(0.5f, 0.5f);
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(viewport.rect.width, totalWidth));
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, viewport.rect.height);
            content.anchoredPosition = Vector2.zero;
            float left = -totalWidth * 0.5f;
            foreach (DressChoiceModal modal in row)
            {
                RectTransform rect = (RectTransform)modal.transform;
                rect.SetParent(content, false);
                float width = rect.rect.width * Mathf.Abs(rect.localScale.x);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(left + width * rect.pivot.x,
                    rect.rect.height * Mathf.Abs(rect.localScale.y) * (rect.pivot.y - 0.5f));
                left += width + gap;
            }
            scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scrollRect.horizontalNormalizedPosition = 0.5f;
        }
        finally { arranging = false; }
    }

    void OnRectTransformDimensionsChange()
    {
        if (isOpen)
            ArrangeModals();
    }

    void OnDestroy()
    {
        foreach (DressChoiceModal modal in modals)
            if (modal != null)
                modal.Selected -= SelectModal;
        if (canvasGroup != null)
            canvasGroup.DOKill();
    }
}
