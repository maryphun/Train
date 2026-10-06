# Dialogue entry API

Implementation: `Assets/Scripts/DialogueFlow.cs`.

Call from any Unity script on the main thread in Play Mode:

```csharp
bool accepted = DialogueFlow.Start("Tutorial_Start", "MainMenu");

// Optional duration for each fade; defaults to SceneTransitionManager.DefaultFadeTime.
DialogueFlow.Start("1日目", "MainMenu", fadeTime: 0f);

// Optional override using an imported YarnProject asset referenced by the caller.
DialogueFlow.Start("Tutorial_Start", "MainMenu", project: loadedProject);
```

The first argument is the creator's exact, case-sensitive **Node name**, not a line ID,
sheet name, scene name, or `.yarn` filename. Current imported nodes include `Tutorial_Start`,
`Tutorial_A`, `Tutorial_B`, and `1日目`. Use the names in your newly imported content as it changes.
The second argument is the caller's return scene name or full path.

The return value reports whether the request was accepted, not whether playback succeeded or
finished. `DialogueFlow.IsRunning` stays true during prepared setup, entry, playback, and the return transition.
Requests during an existing dialogue flow or scene transition return false and are not queued.

## Use your own entry transition

```csharp
if (DialogueFlow.Setup("Tutorial_Start", "MainMenu"))
{
    SceneTransitionManager.Instance.LoadScene("Dialogue", 0.75f);
}

// Optional return fade and imported project; the caller chooses the entry fade separately.
// DialogueFlow.Setup("Tutorial_Start", "MainMenu", returnFadeTime: 0.5f, project: loadedProject);
```

`Setup` stores the Node, return scene, and optional YarnProject without loading a scene or
starting playback. Call it before your own transition, including before an animation sequence
that later calls `LoadScene`. On the next Dialogue scene load, the helper consumes the setup,
waits for scene `Start` methods and any `SceneTransitionManager` entry fade, then runs the Node.
The existing automatic return is preserved; `returnFadeTime` affects only that return transition.

A prepared request keeps `IsRunning` true and rejects competing requests. If you abandon the
entry transition, call `DialogueFlow.CancelSetup()` to release it. This returns true only while
the setup is still waiting for scene entry; it does not cancel a scene load or active playback.
Unrelated scene loads do not consume the setup, and later Dialogue loads do not replay a consumed
setup. `Start` continues to handle both preparation and entry loading in one call.

## Import updated creator content manually

1. Save the intended dialogue into the Google Sheet used by the creator/converter. For the
   requested entry, its `Node` column must contain the exact name `Prologue`.
2. Exit Unity Play Mode. In the Project window, select
   `Assets/Dialogues/Dialogue Sheet Converter.asset`.
3. In its Inspector, confirm `Spreadsheet Id` points at the intended workbook. The existing
   converter is already configured and outputs `Assets/Dialogues/GeneratedDialogue.yarn`.
4. Click **Download & Generate Yarn**. Wait for the asset refresh/import to finish and check
   the Console for success or errors.
5. Verify the generated file contains `title: Prologue`. Unity imports the generated Yarn
   through `Assets/Dialogues/Project.yarnproject`, which the Dialogue runner already references.
6. Enter Play Mode and call `DialogueFlow.Setup("Prologue", "MainMenu")` before your transition.

Repeat the generation step after updating the spreadsheet; `DialogueFlow` plays the imported
project and does not fetch new creator content itself.

The converter now excludes the entire content of Nodes whose names contain Japanese characters
(Kana or Kanji), including mixed names such as `Prologue_朝`. Japanese dialogue in English-named
Nodes is preserved, and Japanese spreadsheet tab names do not exclude their English-named Nodes.
It also skips empty Nodes that emit no dialogue, commands, or choices. An empty row does not remove
later content in the same retained Node, and command-only or choice-only Nodes are retained.

Both filters have been applied to the current generated export. Its 14 retained Nodes compile
with zero errors/warnings and include Prologue; retained Node blocks were verified unchanged.
32 checks against the production converter verify the filters and CSV content preservation.
Filtering is performed on every **Download & Generate Yarn** run, and skipped Nodes are logged
with their sheet name. The source spreadsheet is not modified by either filter.

If generation succeeds but a newly added Node remains unavailable, check **Yarn compilation
errors** as well as the converter's download log. All Nodes exported from all spreadsheet tabs
share one project-wide namespace. Duplicate nonempty Node names still need to be resolved in the
source spreadsheet.

## Set up the Dialogue scene

Per the user's request, this change adds the API only. It does not populate `Dialogue.unity`,
change its UI, or wire a particular Node into `TitleManager`. A later transition troubleshooting
task registered Dialogue in the shared Build Profiles scene list because its absence blocked loading.

1. Add `Assets/Scenes/Dialogue.unity` and every return scene to the enabled Build Profiles scene list.
2. In `Dialogue`, add exactly one active Yarn **Dialogue Runner** with **Auto Start disabled**.
3. Assign `Assets/Dialogues/Project.yarnproject` to that runner, unless the caller supplies a
   `YarnProject` override. Import the creator's content first using the existing converter;
   the helper plays the compiled asset and does not download or import spreadsheets at runtime.
4. Configure the runner's line and option presenters, continue input, and EventSystem.
   `DialogueTest.unity` demonstrates the existing runner/UI setup.
5. Configure the existing background, character, and other command controllers used by your nodes.
   See `Docs/DialogueCommands_JA.md` for those commands.

The helper creates its persistent owner automatically; no `DialogueFlow` component needs to be
placed in the scene. Scene readiness means normal Unity scene activation and `Start` methods,
followed by the entry fade. If you later add asynchronous scene initialization, it will need an
explicit readiness contract.

## Playback and failure behavior

The helper uses the existing `SceneTransitionManager` fades to enter `Dialogue`, finds its
configured runner, validates the selected project/Node, and starts playback. It awaits both
`StartDialogue` and `DialogueTask`: Yarn's start task alone does not wait for the player's lines,
choices, or presenter cleanup. Completion transitions to the supplied return scene.

Blank nodes, unavailable scenes, invalid fade durations, and invalid explicitly supplied projects
are rejected before entering. A missing runner, running Auto Start dialogue, invalid scene-assigned
project/Node, or playback exception is logged and releases the request without a return transition.
Because the scene-assigned project is discovered after loading, its validation can only happen then.
Leaving `Dialogue` through another system ends the helper's request and does not redirect the new scene.
The API does not change `PlayerProfile`, save/load data, or Yarn variables.

## Verification

Runtime and Editor assemblies compiled with the project's Unity 6000.3.15f1 Roslyn compiler.
66 headless checks against the actual helper source passed using Unity/Yarn/transition test doubles.
They cover request validation, transition timing, full playback completion, synchronous completion,
project overrides, missing/ambiguous runners, playback faults, task cancellation, external scene
changes, return rejection, staged setup with caller-controlled entry, one-time consumption,
startup/fade timing, setup cancellation, and owner cleanup. The harness is in ignored
`Library/DialogueFlowVerification/Checks.cs`.

Unity Play Mode playback was not tested; the user is setting up the Dialogue scene.
