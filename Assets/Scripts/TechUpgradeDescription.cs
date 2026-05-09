using UnityEngine;
using TMPro;
using DG.Tweening;

public class TechUpgradeDescription : MonoBehaviour
{
    [Header("References")]
    [SerializeField] TMP_Text name;
    [SerializeField] TMP_Text description;
    [SerializeField] TMP_Text cost;
    [SerializeField] TMP_Text cost_value;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] RectTransform rect;

    private void Start()
    {
        Init();
    }

    private void Init()
    {
        canvasGroup.alpha = 0.0f;
        
        this.name.text = string.Empty;
        this.description.text = string.Empty;
        this.cost_value.text = string.Empty;
    }

    public void SetupText(string name, string description, int cost)
    {
        SetText(description);
        this.name.DOText(name, 0.2f);
        this.description.DOText(description, 0.2f);
        this.cost_value.DOText(cost.ToString(), 0.35f, true, ScrambleMode.Numerals);
        canvasGroup.DOFade(1.0f, 0.2f);
    }

    public void SetNodePosition(Vector2 newPosition)
    {
        rect.position = new Vector2(newPosition.x + (rect.sizeDelta.x * 0.5f), newPosition.y);
    }

    public void Hide()
    {
        canvasGroup.DOFade(0.0f, 0.2f);
    }

    public void SetText(string newText)
    {
        description.text = newText;

        // Force TMP/layout to update before reading preferredHeight
        description.ForceMeshUpdate();

        float preferredHeight = description.preferredHeight;

        rect.sizeDelta = new Vector2(
            rect.sizeDelta.x,
            preferredHeight + 150.0f
        );
    }
}
