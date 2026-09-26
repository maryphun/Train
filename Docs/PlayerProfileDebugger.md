# Player profile debugger

`Assets/Scenes/MainMenu.unity` contains a root **PlayerProfileDebugger** GameObject.
It becomes a persistent singleton when the game runs. On scene changes, look under the
**DontDestroyOnLoad** scene in the Hierarchy, or lock its Inspector before changing scenes.

## Use

1. Enter Play Mode and select **PlayerProfileDebugger**.
2. Change **Money**, **Research points**, **Battle points**, or **Current date**.
   Press Enter or leave the numeric field to commit the change. These fields show live profile values.
3. To increment a resource, enter **Amount to add** below it and press **Add**.
   Negative amounts subtract; integer overflow is rejected without changing the value.
4. Select **Current time** from the project's existing `Clock` enum:
   Morning, Evening, Night, Midnight. Current date is the raw day counter, initialized to 0 by New Game.
5. Drag a **BattlerData** asset into the battler slot, choose **Level to assign**, then click
   **Add / Update Battler**. An existing matching ID gets the selected level instead of another record.
   Expand **Available battlers** to edit existing levels directly.

Adding a battler unlocks availability, not hiring. It does not spend points or modify the ScriptableObject.
Only `BattlerID` and `BattlerCurrentLevel` are stored in the profile. The existing hire panel expects
assets in `Resources/BattlerData`; close and reopen that panel to refresh its list after a debug change.

## Scope and safety

- Editing controls are available only on the active, enabled singleton during Play Mode.
- The debugger does not initialize the profile, apply preset values on startup, advance turns,
  invoke date events, reset the game, or save anything automatically.
- It writes only the chosen field/record; unrelated gameplay changes are not overwritten by a snapshot.
- Existing UI subscribers still receive `BattlePointChanged`. Other screens update according to
  their existing behavior; the debugger does not invent a global UI refresh system.
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

- Runtime and Editor assemblies compile using Unity 6000.3.15f1's bundled Roslyn compiler.
- 24 headless checks passed against the real profile/debugger sources with lightweight Unity test
  doubles: Play Mode and enabled-state guards, resource set/add, negative values, overflow preservation,
  battle-point notifications, date/time writes, invalid clock rejection, null/blank battler rejection,
  add/update by ID, no hiring cost or asset mutation, level updates, duplicate-singleton handling,
  persistence registration, and recovery after a static reset.
- Interactive Inspector and real scene-transition verification still require Unity Play Mode.
