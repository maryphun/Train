using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Assets.SimpleLocalization.Scripts;

public class BattlerSelectionModal : MonoBehaviour
{
    [Header("References")]
    [SerializeField] TMP_Text battler_level_text;
    [SerializeField] TMP_Text battler_name_text;
    [SerializeField] TMP_Text battler_requiredpoint_text;
    [SerializeField] Button battler_hire_button;

    [Header("data")]
    string character_name_ID;
    int requiredBattlePoint;
    int currentBattlerLevel;

    // Update is called once per frame
    void UpdateModalUI()
    {
        battler_level_text.text = "Lv " + currentBattlerLevel.ToString();
        battler_name_text.text = LocalizationManager.Localize(character_name_ID);
        battler_requiredpoint_text.text = requiredBattlePoint.ToString();

        // check if player has enough battler point, then decide the state of the hire button.
        battler_hire_button.interactable = PlayerProfile.BattlePoint >= requiredBattlePoint;
    }

    // data initialization
    public void Initialization(BattlerData data, int battlerLevel)
    {
        requiredBattlePoint = data.BattlerRequiredPoint;
        currentBattlerLevel = battlerLevel;
        character_name_ID = data.BattlerNameID;

        UpdateModalUI();
    }

    public void OnClickHireButton()
    {
        // enter battle

    }
}
