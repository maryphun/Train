using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class ButtonHoverTextColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;

    private void Awake()
    {
        if (buttonText == null)
            buttonText = GetComponentInChildren<TMP_Text>();

        buttonText.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        buttonText.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        buttonText.color = normalColor;
    }
}