using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class EnergyGauge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image UI;

    [Header("Sprites")]
    [SerializeField] private List<Sprite> sprites; 

    public void UpdateEnergyGauge(int energy)
    {
        if (UI == null || sprites == null || sprites.Count == 0)
        {
            Debug.LogError("EnergyGauge needs an Image and at least one sprite.", this);
            return;
        }

        // The profile remains unrestricted; only the available artwork is bounded.
        UI.sprite = sprites[Mathf.Clamp(energy, 0, sprites.Count - 1)];
    }
}
