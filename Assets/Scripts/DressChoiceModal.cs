using UnityEngine;
using UnityEngine.UI;

public class DressChoiceModal : MonoBehaviour
{
    [Header("Setting")]
    [SerializeField] private string dressID; 
    [SerializeField] private Sprite dressSprite;

    [Header("References")]
    [SerializeField] private Image dressImage;
    [SerializeField] private GameObject selectionFrame;

    public Sprite DressSprite => dressSprite;
    public GameObject SelectionFrame => selectionFrame;
    public bool IsAvailable => isAvailable;
    public event System.Action<DressChoiceModal> Selected;

    private Button modalButton;
    private Image unavailableImage;

    [Header("For Debug")]
    [SerializeField] private bool isAvailable; 
    [SerializeField] private bool isSelected; 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Initialization()
    {
        if (modalButton == null)
            modalButton = GetComponent<Button>();
        if (modalButton != null)
        {
            modalButton.onClick.RemoveListener(OnClick);
            modalButton.onClick.AddListener(OnClick);
        }
        if (dressImage != null && dressSprite != null)
            dressImage.sprite = dressSprite;
    }

    public void SetAvailable(bool available, Sprite unavailableIcon)
    {
        isAvailable = available;
        if (modalButton == null)
            modalButton = GetComponent<Button>();
        if (modalButton != null)
            modalButton.interactable = available;
        if (dressImage != null)
            dressImage.gameObject.SetActive(available);
        if (unavailableImage == null && unavailableIcon != null)
        {
            var iconObject = new GameObject("Unavailable Dress", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.layer = gameObject.layer;
            iconObject.transform.SetParent(transform, false);
            unavailableImage = iconObject.GetComponent<Image>();
            unavailableImage.preserveAspect = true;
            unavailableImage.raycastTarget = false;
            RectTransform iconRect = unavailableImage.rectTransform;
            iconRect.anchorMin = new Vector2(0.2f, 0.2f);
            iconRect.anchorMax = new Vector2(0.8f, 0.8f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
        }
        if (unavailableImage != null)
        {
            unavailableImage.sprite = unavailableIcon;
            unavailableImage.gameObject.SetActive(!available);
        }
    }

    // The panel owns the highlight because the scene uses a shared Selection object.
    public void SetSelected(bool selected) => isSelected = selected;

    private void OnClick()
    {
        if (isAvailable)
            Selected?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (modalButton != null)
            modalButton.onClick.RemoveListener(OnClick);
    }
}
