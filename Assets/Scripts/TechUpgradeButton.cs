using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class TechUpgradeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool IsHovered { get; private set; }

    [Header("Setting")]
    [SerializeField] string techName;
    [SerializeField, TextArea(5, 20)] string techDescription;
    [SerializeField] int techRequiredPoints;
    [SerializeField] List<TechUpgradeButton> requiredTechNode;
    [SerializeField] float iconRadius = 50.0f;
    [SerializeField] TechType techID;

    [Header("Debug")]
    [SerializeField] bool isUnlocked;
    [SerializeField] bool isUpgradable;
    [SerializeField] RectTransform rect;

    [Header("References")]
    [SerializeField] RectTransform arrowPrefab;
    [SerializeField] TechUpgradeDescription description;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rect = GetComponent<RectTransform>();

        // check if all the previous required node is unlocked
        if (!isUnlocked)
        {
            isUpgradable = true;
            foreach (TechUpgradeButton node in requiredTechNode)
            {
                if (!node.isUnlocked)
                {
                    isUpgradable = false;
                }
            }
        }

        // visuals
        Image img = transform.GetComponent<Image>();

        if (!isUnlocked)
        {
            img.color = new Color(1, 1, 1, 0.5f);
        }
        else if (!isUpgradable)
        {
            img.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        }

        // draw arrows = GetComponent<RectTransform>();
        foreach (TechUpgradeButton node in requiredTechNode)
        {
            SpawnArrow(node.GetComponent<RectTransform>().position, rect.position, rect);
        }
    }

    public void SpawnArrow(Vector2 pointA, Vector2 pointB, RectTransform parent)
    {
        RectTransform arrow = Instantiate(arrowPrefab, parent);
        arrow.gameObject.SetActive(true);

        // Position arrow between A and B
        arrow.position = (pointA + pointB) * 0.5f;

        // Direction from A to B
        Vector2 dir = pointB - pointA;

        // Because the image points UP by default, use Atan2(x, y)
        float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;

        arrow.localRotation = Quaternion.Euler(0, 0, -angle);

        // Optional: stretch arrow length to match distance
        float length = dir.magnitude - (iconRadius * 2.0f);
        arrow.sizeDelta = new Vector2(arrow.sizeDelta.x, length);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        IsHovered = true;
        description.SetupText(techName, techDescription, techRequiredPoints);
        description.SetNodePosition(new Vector2(iconRadius + rect.position.x, rect.position.y));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        IsHovered = false;
        description.Hide();
    }
}
