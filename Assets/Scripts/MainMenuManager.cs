using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Assets.SimpleLocalization.Scripts;
using DG.Tweening;

public class MainMenuManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] TMPro.TMP_Text date_label;
    [SerializeField] TMPro.TMP_Text weekday_label;
    [SerializeField] TMPro.TMP_Text clock_label;
    [SerializeField] TMPro.TMP_Text money_label;
    [SerializeField] EnergyGauge energyGauge;
    [SerializeField] Image background;
    [SerializeField] MainMenuTurorial tutorial;
    [SerializeField] GoingOutTutorial goingOutTutorial;
    [SerializeField] EndDayTutorial endDayTutorial;
    [SerializeField] EducatePanel educatePanel;
    [SerializeField] Image tokaBodyImage;
    [SerializeField] CanvasGroup dayChange;
    [SerializeField] TMPro.TMP_Text todayDate;
    [SerializeField] TMPro.TMP_Text endTodayBtnText;
    [SerializeField, Min(0.01f)] float dayChangeFadeDuration = 0.5f;
    

    private Coroutine dayChangeRoutine;
    private Tween dayChangeTween;

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
        StopDayChange();
    }

    void UpdateTokaBody()
    {
        if (tokaBodyImage != null)
            tokaBodyImage.sprite = PlayerProfile.TokaCurrentBody;

        // perform an autosave everytime there is a change in UI in the mainmenu
        SaveLoad.AutoSave();
    }

    public void UpdateUI()
    {
        string calenderDate = PlayerProfile.CurrentDate.ToString();
        string currentClock = LocalizationManager.Localize("Calender." + PlayerProfile.CurrentClock.ToString());
        string money = PlayerProfile.Money.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)
            + LocalizationManager.Localize("Common.MoneyUnit");
        int energyPoint = PlayerProfile.Energy;

        date_label.text = calenderDate;
        weekday_label.text = LocalizationManager.Localize("Calender." + GetWeekday(PlayerProfile.CurrentDate));
        clock_label.text = currentClock;
        money_label.text = money;
        energyGauge.UpdateEnergyGauge(energyPoint);

        // perform an autosave everytime there is a change in UI in the mainmenu
        SaveLoad.AutoSave();
    }

    /// <summary>Converts the game's day counter to a weekday: day 1 is Thursday.</summary>
    public static System.DayOfWeek GetWeekday(int currentDate)
    {
        // Normalize before adding the offset to support negative debug dates and avoid overflow.
        int dayInWeek = ((currentDate % 7) + 7) % 7;
        return (System.DayOfWeek)((dayInWeek + (int)System.DayOfWeek.Wednesday) % 7);
    }

    private IEnumerator Start()
    {
        while (DialogueFlow.IsRunning || SceneTransitionManager.IsTransitioning) yield return null;
        if (PlayerProfile.GetTutorialTriggered(Tutorials.MainMenu) == false)
        {
            // Start tutorial
            tutorial.StartTutorial();
        }
    }

    public void StartNewDay(bool incraseDayCnt)
    {
        if (!isActiveAndEnabled || dayChange == null || todayDate == null)
        {
            Debug.LogWarning("StartNewDay requires an enabled MainMenuManager with Day Change and Today Date assigned.", this);
            return;
        }

        StopDayChange();
        if (incraseDayCnt) PlayerProfile.CurrentDate++;
        todayDate.text = "DAY " + PlayerProfile.CurrentDate;
        endTodayBtnText.text = LocalizationManager.Localize("Home.EndToday", PlayerProfile.CurrentDate);
        dayChange.gameObject.SetActive(true);
        dayChange.blocksRaycasts = true;
        dayChange.interactable = true;
        dayChangeRoutine = StartCoroutine(ShowDayChange());
    }

    private void LateUpdate()
    {
        // Check each frame as Energy can reach zero later than the Going Out tutorial.
        // Wait for existing playback/overlays so the last education CG is never interrupted.
        if (endDayTutorial == null || !endDayTutorial.CanStart
            || DialogueFlow.IsRunning || SceneTransitionManager.IsTransitioning || dayChangeRoutine != null
            || (tutorial != null && tutorial.IsRunning)
            || (goingOutTutorial != null && goingOutTutorial.IsRunning)
            || (educatePanel != null && educatePanel.IsBusy)) return;

        endDayTutorial.transform.SetAsLastSibling();
        endDayTutorial.StartTutorial();
    }

    private IEnumerator ShowDayChange()
    {
        bool isDay1 = PlayerProfile.CurrentDate == 1;
        string dailyConversationNode = "Conversation_Day"
            + PlayerProfile.CurrentDate.ToString(System.Globalization.CultureInfo.InvariantCulture);
        float duration = Mathf.Max(0.01f, dayChangeFadeDuration);
        dayChangeTween = dayChange.DOFade(1f, duration).SetUpdate(true);
        yield return dayChangeTween.WaitForCompletion();
        dayChangeTween = null;

        // Do not dismiss with the mouse press that opened the overlay.
        yield return null;
        while (!Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1) && !Input.GetMouseButtonDown(2))
            yield return null;

        dayChangeTween = dayChange.DOFade(0f, duration).SetUpdate(true);
        yield return dayChangeTween.WaitForCompletion();
        dayChangeTween = null;

        // Keep the overlay blocking until the existing scene-transition flow takes over.
        while (DialogueFlow.IsRunning || SceneTransitionManager.IsTransitioning)
            yield return null;
        if (!DialogueFlow.Start(dailyConversationNode, "MainMenu",
            nextNodeOnReturn: isDay1 ? "Tutorial_Teach" : null,
            onCompleted: isDay1 ? CompleteDay1TeachingTutorial : (System.Action)null))
            Debug.LogWarning($"Could not start daily conversation '{dailyConversationNode}'.", this);

        dayChange.blocksRaycasts = false;
        dayChange.interactable = false;
        dayChangeRoutine = null;
    }

    private static void CompleteDay1TeachingTutorial()
    {
        // The manager that started the conversation was unloaded; use the returned scene's instance.
        MainMenuManager manager = FindFirstObjectByType<MainMenuManager>();
        if (manager != null && manager.isActiveAndEnabled)
            manager.OnDay1TeachingTutorialCompleted();
        else
            Debug.LogWarning("Day 1 teaching tutorial finished, but no active MainMenuManager was found.");
    }

    public void OnDay1TeachingTutorialCompleted()
    {
        // Add the next forced-tutorial step here. MainMenu is loaded and its return fade has finished.
        Debug.Log("Day 1 teaching tutorial completed. Edit OnDay1TeachingTutorialCompleted() for the next step.", this);

        FindFirstObjectByType<EducatePanel>().DisplayUI(true, true);
    }

    private void StopDayChange()
    {
        if (dayChangeRoutine != null)
            StopCoroutine(dayChangeRoutine);
        dayChangeRoutine = null;
        dayChangeTween?.Kill();
        dayChangeTween = null;
        if (dayChange != null)
        {
            dayChange.alpha = 0f;
            dayChange.blocksRaycasts = false;
            dayChange.interactable = false;
        }
    }
}
