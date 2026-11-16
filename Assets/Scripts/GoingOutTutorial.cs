/// <summary>Post-education explanation using the same presentation as the MainMenu tutorial.</summary>
public sealed class GoingOutTutorial : MainMenuTurorial
{
    protected override int LastTextStep => 7;
    protected override string LocalizationPrefix => "Tutorial.Goingout-";
    protected override Tutorials CompletionTutorial => Tutorials.GoingOut;
    protected override bool AllowProtagonistHighlight => false;

    protected override void OnTutorialCompleted()
    {
        // Leave the player in MainMenu to choose their next action.
    }
}
