using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EducateOptionData", menuName = "Scriptable Objects/EducateOptionData")]
public class EducateOptionData : ScriptableObject
{
    [Min(0)] public int energyCost;

    public Sprite icon;
    public Sprite image;

    [Tooltip("Localization key for the option name.")]
    public string nameID;

    [Tooltip("Localization key for the option description.")]
    public string descriptionID;

    public string dialogueID;

    [Tooltip("Time slots when this option is available.")]
    public List<Clock> availableDayTime = new List<Clock>();
}
