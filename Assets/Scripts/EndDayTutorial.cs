using UnityEngine;

/// <summary>Explains ending the day once Going Out is complete and Energy reaches zero.</summary>
public sealed class EndDayTutorial : MainMenuTurorial
{
    [SerializeField] private GameObject endTodayButton;

    protected override int LastTextStep => 3;
    protected override string LocalizationPrefix => "Tutorial.End-";
    protected override Tutorials CompletionTutorial => Tutorials.EndDay;
    protected override bool AllowProtagonistHighlight => false;
    protected override GameObject GetHighlightTarget(int step) => step == 3 ? endTodayButton : null;

    public bool CanStart => !IsRunning
        && PlayerProfile.GetTutorialTriggered(Tutorials.GoingOut)
        && !PlayerProfile.GetTutorialTriggered(Tutorials.EndDay)
        && PlayerProfile.Energy == 0;

    protected override void OnTutorialCompleted()
    {
        // Leave the real End Today button to the player; do not end the day automatically.
    }
}
