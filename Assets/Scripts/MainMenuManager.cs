using UnityEngine;
using UnityEngine.UI;
using Assets.SimpleLocalization.Scripts;

public class MainMenuManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] TMPro.TMP_Text date_label;
    [SerializeField] TMPro.TMP_Text clock_label;
    [SerializeField] TMPro.TMP_Text money_label;
    [SerializeField] EnergyGauge energyGauge;
    [SerializeField] Image background;
    [SerializeField] MainMenuTurorial tutorial;
    [SerializeField] Image tokaBodyImage;

    void OnEnable()
    {
        PlayerProfile.DateChanged += UpdateUI;
        PlayerProfile.ClockChanged += UpdateUI;
        PlayerProfile.MoneyChanged += UpdateUI;
        PlayerProfile.ResearchPointChanged += UpdateUI;
        PlayerProfile.EnergyChanged += UpdateUI;
        PlayerProfile.TokaBodyChanged += UpdateTokaBody;
        UpdateUI();
        UpdateTokaBody();
    }

    void OnDisable()
    {
        PlayerProfile.DateChanged -= UpdateUI;
        PlayerProfile.ClockChanged -= UpdateUI;
        PlayerProfile.MoneyChanged -= UpdateUI;
        PlayerProfile.ResearchPointChanged -= UpdateUI;
        PlayerProfile.EnergyChanged -= UpdateUI;
        PlayerProfile.TokaBodyChanged -= UpdateTokaBody;
    }

    void UpdateTokaBody()
    {
        if (tokaBodyImage != null)
            tokaBodyImage.sprite = PlayerProfile.TokaCurrentBody;
    }

    public void UpdateUI()
    {
        string calenderDate = PlayerProfile.CurrentDate.ToString();
        string currentClock = LocalizationManager.Localize("Calender." + PlayerProfile.CurrentClock.ToString());
        string money = PlayerProfile.Money.ToString() + LocalizationManager.Localize("Common.MoneyUnit");
        int energyPoint = PlayerProfile.Energy;

        date_label.text = calenderDate;
        clock_label.text = currentClock;
        money_label.text = money;
        energyGauge.UpdateEnergyGauge(energyPoint);
    }

    private void Start()
    {
        if (PlayerProfile.GetTutorialTriggered(Tutorials.MainMenu) == false)
        {
            // Start tutorial
            tutorial.StartTutorial();
        }
    }
}
