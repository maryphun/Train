# Scene transitions

Implementation: `Assets/Scripts/CommonScripts/SceneTransitionManager.cs`.
The original manager and its `.meta` were moved together, retaining the script GUID and existing calls.
No scene setup is required: its singleton creates a persistent manager and full-screen black overlay on demand.

## Call from any main-thread Unity script

```csharp
SceneTransitionManager.Load("MainMenu"); // 0.75 seconds out + 0.75 seconds in
SceneTransitionManager.Load("Battle", 0.5f); // Duration for each fade
SceneTransitionManager.Restart(); // Reload the active scene
SceneTransitionManager.Restart(1f);

// Previous API remains supported:
SceneTransitionManager.Instance.LoadScene("MainMenu", 0.75f);
SceneTransitionManager.Instance.RestartScene();
```

Static `Load` / `Restart` and instance `TryLoadScene` return whether the request was **accepted**,
not whether loading has finished. Repeated requests during a transition return false (no queue).
Use `SceneTransitionManager.IsTransitioning` to inspect progress. Names or full scene paths must be
loadable through Unity's enabled Build Profiles scene list; invalid targets/durations are rejected before fading.
Calls outside Play Mode are rejected. All calls must run on Unity's main thread.

## Sequence

1. Block UI input immediately, then smoothly fade the old scene to opaque black.
2. Start `LoadSceneAsync` in Single mode and keep the overlay fully black until `operation.isDone`.
   This includes scene activation, not merely the 0.9 preload threshold.
3. Wait one more frame for the new scene's `Start` methods, then fade from black to clear.
4. Remove the overlay and restore input. Input stays blocked throughout both fades and loading.

Fades use unscaled delta time and smoothstep easing, so `Time.timeScale == 0` does not freeze them.
The manager does not change time scale. A zero duration skips animation but still loads asynchronously
under the opaque overlay, waits for completion and the startup frame, and blocks input.

The overlay blocks UI pointer raycasts. Existing and newly loaded scene EventSystems/input modules are
temporarily disabled as well, preventing mouse/touch UI events and keyboard/controller submit on selected
buttons. Only components that this manager disabled are restored; destroyed old-scene components are skipped.
The overlay uses sorting order 32767; reserve that top layer for transitions.

## Boundaries

- Restart reloads scene objects; it does **not** reset static `PlayerProfile`, reload a save or restart a battle setup.
- For loading a saved game, call `SaveLoad.Load(slot)` first, check success, then transition. Do not call
  `PlayerProfile.Initialization()` afterward unless you intend to erase the loaded profile.
- Unity scene-load completion does not imply completion of your own web requests, async initialization or
  coroutines started by scene scripts. Those need a separate readiness contract if introduced later.
- Custom gameplay scripts polling raw input (`Input.GetMouseButton`, Input System actions, `OnMouseDown`, etc.)
  bypass EventSystem UI blocking. Guard such handlers with:

```csharp
if (SceneTransitionManager.IsTransitioning)
    return;
```

- A synchronous exception or null operation when starting the load fades the old scene back in and releases input.
  Disabling/destroying the manager also cleans up its lock, but cannot cancel an already-started Unity scene load.
  Do not disable the manager mid-transition.
- Existing direct scene loads (for example the battle-flow controller) were not automatically replaced.
  Use the helper explicitly at call sites where this behavior is wanted.

## Verification

Runtime and Editor assemblies compiled using the project's Unity 6000.3.15f1 Roslyn compiler.
24 headless checks against the actual manager source passed using Unity/coroutine test doubles, covering
request validation, input locking/restoration, waiting for activation completion, the startup frame,
paused-time fades, repeat requests, zero-duration restart, load exceptions, null operations and cleanup.
Real Unity Play Mode visual/input and scene-transition testing was not performed in this turn.
