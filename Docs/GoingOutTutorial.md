# Post-education Going Out tutorial

After all four queued education actions finish, EducatePanel closes with its existing fade.
Input remains blocked until that fade finishes, then **Tutorial.Goingout-1** through
**Tutorial.Goingout-7** play in order. The localization keys already exist in System.csv.
Simply closing the panel, rejecting an unaffordable queue, interrupting playback, or disabling
the component does not trigger this tutorial.

GoingOutTutorial inherits MainMenuTurorial's presentation: entry/exit fades, centered textbox
sized to its text, typing, immediate click-to-advance, Tetora portrait, and the bottom-right
triangle with the authored pivot preserved. It adds no object highlights. Completion leaves
the player in MainMenu to choose their next action; it does not open an outdoor menu or start
a new day.

After completion, MainMenu waits for Energy to reach zero, then runs Tutorial.End-1 through
Tutorial.End-3. If Energy is already zero, it can start immediately after the Going Out exit.
See `Docs/EndDayTutorial.md`. Shared text controls now reveal unfinished text on the first click
and advance on the next click after the message is fully shown.

MainMenu.unity contains the inactive **Canvas / GoingOutTutorial**, copied from the existing
Tutorial hierarchy and assigned to Educate Panel's **Going Out Tutorial** field. It is outside
Educate Panel so the closed panel's CanvasGroup cannot hide the tutorial. No manual wiring is
required in this scene.

## Completion and replay

The implementation defaults to once per profile, matching the existing tutorial chain.
**Tutorials.GoingOut = 3** is appended after the existing IDs. Normal saves preserve its flag;
older saves default the newly added flag to false. It is marked complete after its exit fade.
Canceling it early leaves the flag false for the next successful education batch.

To replay it after another completed batch:

```csharp
PlayerProfile.SetTutorialTriggered(Tutorials.GoingOut, false);
```

For a quick test, open MainMenu in Play Mode with an initialized profile, finish or skip the
education introduction, queue four affordable actions and press Execute. The seven Going Out
messages should appear only after the last CG and the education panel fade finish. Click to
advance each message. Completing a later batch should skip this tutorial until its flag resets.

## Verification

Runtime and Editor source compilation pass with existing unrelated warnings. Headless checks
cover all seven keys, shared presentation, separate completion, ordered queue playback, closing
fade handoff, duplicate calls, cancellation, manual close, affordability failures, old saves,
saved completion and new-game reset. There are 90 tutorial checks, 101 queue checks, and the
existing save/load suite plus 46 additional checks. Scene IDs, parenting and references pass.

These checks use production source with UI/time/resource doubles. Live Unity Play Mode and
final appearance remain unverified. Verification files are under ignored Library folders.
