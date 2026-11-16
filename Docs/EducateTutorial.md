# First-opening education tutorial

`MainMenu.unity` now contains **Canvas / Educate Panel / EducateTutorial**, copied from the
existing MainMenu **Tutorial** hierarchy (6 GameObjects and their 23 components). It retains
the textbox, Tetora portrait, next triangle, fonts, artwork and authored triangle pivot Y 0.5.
The original MainMenu tutorial hierarchy is unchanged. The copy starts inactive and is assigned
to the Educate Panel's **Educate Tutorial** Inspector reference.

`Assets/Scripts/EducateTutorial.cs` inherits the presentation from `MainMenuTurorial.cs`:

- Entry fade, four centered text steps, then exit fade.
- Uses exactly `Tutorial.Teach-1`, `Tutorial.Teach-2`, `Tutorial.Teach-3`, `Tutorial.Teach-4`
  from the existing System.csv; no text/content was changed.
- Same immediate click-to-advance, typing, text-based box sizing, balanced padding,
  bottom-right triangle, preserved pivot and rapid-click/callback safeguards as MainMenu.
- No education-control or protagonist highlights/clones; the copied narrator portrait remains.
- Completion only closes this tutorial and leaves the Educate Panel open. It does not start
  a new day, enter a scenario, or mark the separate MainMenu tutorial complete.

The panel waits for its existing opening animation, then shows the tutorial if the profile's
education tutorial flag is false. Both `DisplayUI(true)` and `DisplayUI(true, true)` follow
this first-opening rule; the old bool argument does not force completed tutorials to replay.
Queue/category/Execute calls are ignored while the tutorial is running, and its overlay blocks
the controls underneath. Closing/disabling the panel early cancels it without marking completion,
so it will appear again next time. Repeated open requests do not restart an active tutorial.

## Saved completion

After a successful four-action batch and the panel's closing fade, the separate seven-step
Going Out tutorial can run once per profile. It has its own completion flag and is outside
the panel hierarchy. See [GoingOutTutorial.md](GoingOutTutorial.md) for its flow and replay setup.

Appended **Tutorials.Educate = 2**, preserving **MainMenu = 0** and **Battle = 1**. The existing
saved tutorial array already supports missing/new entries, so no new save schema or special
migration is needed. Older two-flag saves load education as false; new-game initialization also
clears it. Completing the teaching tutorial sets it true, included in subsequent normal saves.

To test again in a development script, use:

```csharp
PlayerProfile.SetTutorialTriggered(Tutorials.Educate, false);
educatePanel.DisplayUI(true);
```

Or use the existing debugger's **Initialize New Player Profile** button, which resets the whole
profile (not just this flag). MainMenu's existing profile-change autosaves are unchanged.

## Verification

- Runtime and Editor source assemblies compile with Unity's bundled Roslyn compiler.
- 66 headless production tutorial/presentation checks pass, including the original MainMenu flow
  and all four teaching keys, distinct completion flags, copied layout/pivot and no new-day handoff.
- 87 headless production education-panel checks pass, including first-open/reopen/skip behavior,
  canceled tutorials, protected queue controls, and existing queued action execution/dimming.
- Existing actual-source save/load suite plus 41 additional resource/default/failure checks pass,
  including Educate flag round-trip, old two-flag saves, stable enum IDs and new-game reset.
- Copied scene IDs/references/parenting were validated; original tutorial blocks verified unchanged.

These use lightweight UI/time/resource doubles, not live Unity Play Mode. Actual glyph layout,
final visual appearance and interactive playback remain unverified because Unity MCP is not exposed.
Files remain under ignored `Library/MainMenuTutorialLayoutVerification`,
`Library/EducateQueueVerification`, `Library/EducateTutorialVerification` and
`Library/EducationSaveVerification`.
