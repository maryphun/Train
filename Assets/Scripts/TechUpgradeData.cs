using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TechUpgradeData", menuName = "Scriptable Objects/TechUpgradeData")]
public class TechUpgradeData : ScriptableObject
{
    [Tooltip("Stable ID for this upgrade. Keep it unchanged once used in save data.")]
    public string TechID;

    [Tooltip("All listed upgrades are prerequisites. Leave empty for a starting upgrade.")]
    public List<TechUpgradeData> RequiredUpgrades = new List<TechUpgradeData>();

    [Min(0)] public int RequiredResearchPoint;

    [Tooltip("Localization key for the upgrade name.")]
    public string TechNameID;

    [Tooltip("Localization key for the upgrade description.")]
    public string TechDescriptionID;

    [Tooltip("Icon displayed inside the research node's existing button frame.")]
    public Sprite TechSprite;
}
