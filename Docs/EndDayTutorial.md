# Ending the day tutorial

After the Going Out tutorial has completed, MainMenuManager waits until PlayerProfile.Energy
equals zero. If Energy is positive at Going Out completion, it keeps checking for a later zero.
It waits for existing tutorials, education playback/closing, daily conversation, day-change
overlay and scene transitions before starting the End tutorial.

The inactive **Canvas / EndDayTutorial** in MainMenu is copied from the current tutorial UI.
Its manager and End Today button references are already assigned. EndDayTutorial derives
from MainMenuTurorial and displays the existing **Tutorial.End-1**, **Tutorial.End-2** and
**Tutorial.End-3** keys with the same centered text sizing, Tetora portrait, fades and arrow.
The authored triangle pivot is preserved.

At End-3, a visual copy of **End Today Button** is displayed above the tutorial's dimming.
The copy has its Button components removed and does not block input. The original button
keeps its existing StartNewDay(true) callback and cannot be used through the tutorial overlay.
Click anywhere to reveal unfinished text, then click again to advance once it is fully shown.
Finishing the exit fade removes the tutorial, leaving the real button available normally.
The tutorial does not automatically end the day.

The same reveal-then-advance behavior now applies to MainMenu, Teach, Going Out and End.
Completing a reveal kills its old tween so it cannot later overwrite text or advance a step.
The same-frame click guard remains in place. The highlight stays during reveal and is cleaned
when leaving End-3, finishing, disabling or restarting the tutorial.

## Saved state and testing

Tutorials.EndDay is appended as ID 4. Existing IDs are unchanged. Older saves default its
flag to false through the existing save-array padding. The implementation follows the current
once-per-profile tutorial rule: completion marks the flag for subsequent saves, and new-game
initialization resets it. Canceling early does not mark completion.

To test, finish Going Out with positive Energy, verify no End messages appear, then set Energy
to zero with PlayerProfileDebugger. End-1 through End-3 should play. Test a click while typing
to reveal the whole message without changing steps, then another click to advance. End-3
should highlight End Today; after finishing, use the actual button to end the day.

To replay just this tutorial in a development script:

```csharp
PlayerProfile.SetTutorialTriggered(Tutorials.EndDay, false);
```

It will start in MainMenu when Going Out is completed, Energy is zero and playback is idle.

Runtime and Editor compilation pass. Headless production-source checks pass for 122 tutorial
cases, 11 manager handoff cases, 101 education queue cases, and the existing save/load suite
plus 51 additional compatibility/default/resource cases. Scene references, unique IDs and
the copied triangle pivot were checked. Live Unity Play Mode remains unverified.
