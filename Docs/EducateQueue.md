# Education action queue

Implemented in `Assets/Scripts/EducatePanel.cs`.

## Flow

The first opening shows **EducateTutorial** with Tutorial.Teach-1 through Tutorial.Teach-4
before allowing education input. Subsequent openings skip it after completion is recorded.
See `Docs/EducateTutorial.md` for the copied scene UI, shared presentation and saved flag.

1. Open the Educate Panel and select a category/available option.
2. Each option click appends it to the next empty action slot, up to four. Repeats are allowed.
   Selection and cancellation do not spend energy or show CGs.
3. Filled slots use **EducateButton_0** and the selected option button's displayed text (not its icon).
   Click one to remove that entry; subsequent actions and their copied text shift left. A cleared
   slot restores **EducateButtonFrame_0** and its own original label, cached when the panel awakens.
4. Execute becomes interactable only when all four entries are filled. Its handler checks
   the entire cost before starting; insufficient energy shows the existing popup and retains
   the queue without spending anything.
5. On success, queue editing, category changes and repeated execution are locked. Each action
   deducts its own cost at its start and plays its CG/dialogue before moving to the next.
6. After the fourth CG has finished fading out, the Educate Panel closes and the queue clears.
7. Once the panel's closing fade finishes, **Tutorial.Goingout-1 through Tutorial.Goingout-7**
   play using the MainMenu tutorial presentation, unless the profile has already completed them.
   The closing fade blocks background input; ordinary closing and interrupted batches do not
   trigger this handoff. See `Docs/GoingOutTutorial.md` for wiring and replay details.

The existing CG timing is retained: three seconds of presentation, one-second text reveal,
and half-second fades. Execution now **awaits the outgoing half-second fade** so the next
action cannot overwrite a still-fading image. No new action effects, clock advancement or
availability-time rules are added. The existing explicit disabling of education options 2/3
remains unchanged.

## References and Inspector wiring

- `actionSlots`: the four existing slot Buttons in order, slots 1 through 4.
- Their existing persistent callbacks call `OnClickActionSlot(int)` using **1-based values 1–4**.
- `filledActionSlotSprite` / `emptyActionSlotSprite`: assigned in MainMenu to the existing
  **EducateButton_0** / **EducateButtonFrame_0** sprite sub-assets. Other scene instances can
  assign these fields in Inspector. No runtime icon objects are created.
- Each slot reuses its Button's target Image and existing child TMP label. Filled slots copy
  the selected option's exact displayed text, preserving it even if another category is selected.
  Compaction moves that copied text together with the action. Clearing restores each slot's own
  original text. Font/layout settings are left unchanged.
- The Button's disabled sprite also follows the filled/empty state so execution-time disabling
  cannot turn a filled slot back into a recessed one. Existing click callbacks remain intact.
- `executeButton`: existing Execute Button. Awake adds the `OnClickExecuteButton` listener;
  **do not also add the same handler in Inspector**, or it would be called twice. The existing
  scene's Execute OnClick was empty, so no scene edits are required.
- The existing `CGDisplayer`, `CGDisplayer_CG`, `CGDisplayer_Dialog`, category and
  insufficient-energy references are reused.
- Before presenting a CG, the script reparents the CG Displayer under the main Educate Panel,
  preserving world-space placement, and makes it the last sibling. MainMenu originally places
  it inside the category panel; closing that parent on Execute would otherwise fade every CG
  out too, making a running batch appear unresponsive. No saved scene hierarchy edits are needed.
- **CG presentation > CG Background Darkness** controls background dimming (implementation default
  **0.65**, editable from 0 to 1). During playback a runtime black Image stretches over the whole
  Educate Panel, above its other UI but below the CG Displayer, leaving the CG/frame/dialogue bright.
  **CG Background Fade Duration** separately controls its fade (implementation default **0.75s**).
  It uses InOutSine easing and unscaled time, stays dark between queued actions without restarting
  the fade, blocks background raycasts, and fades out when the batch or standalone CG finishes.
  An external-energy interruption also fades it out; disabling the component clears it immediately
  for cleanup. Original graphic colors are unchanged. CG/text presentation timing is unchanged.
- CG fade timing and queue size are constants in the script. This feature intentionally requires
  exactly four actions, per the confirmed flow.

Opening or normally closing the panel clears unexecuted selections. Requests to open/close it
are ignored during execution, keeping the confirmed batch intact. Disabling the component/object
does cancel running presentation and clears the queue; only actions that already started have
been charged. No automatic refunds or additional spending occur.

If an external system/debugger reduces energy during playback below the next action's cost,
execution stops safely, leaves the unexecuted actions compacted at the front, and shows the
existing insufficient-energy popup. Completed actions are not replayed or refunded. This is
an implementation safety behavior for external changes, not a new ordinary gameplay rule.

## Quick Play Mode check

1. Give the profile sufficient energy for four available options, then open the panel.
2. Add three entries; Execute stays disabled. Add the fourth; Execute enables. Repeat an
   option to verify duplicates are accepted.
3. Remove slot 2. Entries 3 and 4 shift to slots 2 and 3; slot 4 clears and Execute disables.
4. Add another entry. Reduce energy below the total and press Execute; verify the popup
   appears with no deduction and the queue remains.
5. Restore sufficient energy and execute. Verify one CG at a time, one cost per action start,
   no overlapping actions on repeated clicks, and panel close after the final outgoing fade.

## Verification

Runtime and Editor source assemblies compile with Unity 6000.3.15f1's bundled Roslyn compiler
(existing unrelated warnings remain). Seventy-eight headless checks against the production panel
source with UI/coroutine/time doubles passed, covering queue state, repeat selection, slot
compaction, filled/recessed sprites, exact copied text and original-text restoration,
disabled playback presentation, index validation, affordability/overflow, zero/negative costs, retained option
availability rules, ordered timed playback, duplicate-input guards, cancellation/disable cleanup,
external energy changes, re-open state and listener cleanup.

The hierarchy regression first reproduced failure with CG Displayer under the closing category
CanvasGroup. It then passed after the runtime reparenting fix, checking inherited alpha, root
parenting with preserved placement, overlay sibling order, and all four actions in sequence.
The dimming checks cover full-panel bounds, black overlay color, sibling order below an undimmed
CG, sustained batch dimming, configurable strength, standalone preview completion and cleanup.
Additional fade checks sample intermediate alpha at quarter/half duration, verify smooth fade-out,
and ensure repeated requests do not restart the active transition.

The live MainMenu Trello card was refreshed. Unity MCP is not exposed; actual Play Mode appearance
and scene interaction remain unverified. Verification files are ignored under
`Library/EducateQueueVerification`.
