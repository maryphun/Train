# Player profile debugger

`Assets/Scenes/MainMenu.unity` contains a root **PlayerProfileDebugger** GameObject.
It becomes a persistent singleton when the game runs. On scene changes, look under the
**DontDestroyOnLoad** scene in the Hierarchy, or lock its Inspector before changing scenes.

## Use

1. Enter Play Mode and select **PlayerProfileDebugger**.
2. Change **Money**, **Research points**, **Battle points**, **Energy**, or **Current date**.
   Press Enter or leave the numeric field to commit the change. These fields show live profile values.
3. To increment a resource, enter **Amount to add** below it and press **Add**.
   Negative amounts subtract; integer overflow is rejected without changing the value.
4. Select **Current time** from the project's existing `Clock` enum:
   Morning, Evening, Night, Midnight. Current date is the raw day counter, initialized to 0 by New Game.
5. Drag a **BattlerData** asset into the battler slot, choose **Level to assign**, then click
   **Add / Update Battler**. An existing matching ID gets the selected level instead of another record.
   Expand **Available battlers** to edit existing levels directly.
6. Click **Initialize New Player Profile** under **New game profile** to call
   `PlayerProfile.Initialization()` and discard the live profile's unsaved progress.
   Uses the existing new-game defaults; save files and the current scene remain unchanged.
   The debugger fields repaint immediately. Other UI follows the existing initialization
   notifications; initialization currently assigns money, research points, date and clock
   directly, so their subscribed UI may need refreshing separately.

Adding a battler unlocks availability, not hiring. It does not spend points or modify the ScriptableObject.
Only `BattlerID` and `BattlerCurrentLevel` are stored in the profile. The existing hire panel expects
assets in `Resources/BattlerData`; close and reopen that panel to refresh its list after a debug change.

## Scope and safety

- Editing controls are available only on the active, enabled singleton during Play Mode.
- The debugger does not apply preset values on startup, advance turns, change scenes,
  or save anything automatically. Initialization runs only when explicitly requested.
- Ordinary edit commands write only the chosen field/record; the explicit initialization
  command instead resets the entire live profile to the existing new-game defaults.
- Existing profile change events are raised by the corresponding setters. `MainMenuManager`
  listens for Energy, money, date, and time changes; other screens follow their own subscriptions.
- Energy remains an unrestricted saved integer. The four-segment main-menu gauge displays the
  nearest available state for values outside `0`–`4` without changing the profile value.
- Values are not clamped to invented gameplay limits. Invalid clock values and missing/blank battler
  assets are rejected. The level input is a command argument, not a definition of the game's starting level.
- Modified profile data can be saved through the existing `SaveLoad.Save(slot)` workflow if desired.
- No in-game debug overlay or automatic singleton spawning is added. For direct entry into another
  scene, attach the component to one root GameObject in that scene; extra debugger components reject
  themselves without destroying unrelated components.

## Extension

Runtime commands live in `Assets/Scripts/PlayerProfileDebugger.cs`; the Inspector lives in
`Assets/Scripts/Editor/PlayerProfileDebuggerEditor.cs`. Add a command through the existing `Execute`
guard, then expose it in the custom Inspector. `PlayerProfile.CurrentDate` and `CurrentClock` expose
the existing saved fields; this feature does not change the save schema.

## Verification

- The initialization button changes compile in both runtime and Editor assemblies. The extended
  headless debugger suite passes 31 checks, including reset defaults and rejection outside Play
  Mode, on disabled components, and on duplicate singletons. Interactive Inspector testing remains pending.
- The initialization button changes compile in both runtime and Editor assemblies. The extended
  headless debugger suite passes 31 checks, including reset defaults and rejection outside Play
  Mode, on disabled components, and on duplicate singletons. Interactive Inspector testing remains pending.
- Runtime and Editor assemblies compile using Unity 6000.3.15f1's bundled Roslyn compiler,
  including the new Energy controls.
- 24 headless checks passed against the real profile/debugger sources with lightweight Unity test
  doubles: Play Mode and enabled-state guards, resource set/add, negative values, overflow preservation,
  battle-point notifications, date/time writes, invalid clock rejection, null/blank battler rejection,
  add/update by ID, no hiring cost or asset mutation, level updates, duplicate-singleton handling,
  persistence registration, and recovery after a static reset.
- The prior 24 headless checks do not cover the new Energy controls. Interactive Inspector,
  save/load verification, and real scene-transition checks still require Unity Editor execution.
