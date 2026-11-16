/// <summary>The copied education tutorial UI uses the same presentation as the MainMenu tutorial.</summary>
public sealed class EducateTutorial : MainMenuTurorial
{
    protected override int LastTextStep => 4;
    protected override string LocalizationPrefix => "Tutorial.Teach-";
    protected override Tutorials CompletionTutorial => Tutorials.Educate;
    protected override bool AllowProtagonistHighlight => false;

    protected override void OnTutorialCompleted()
    {
        // Stay in the already-open Educate Panel; do not restart the day or a scenario.
    }
}
