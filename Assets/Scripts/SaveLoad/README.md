# Save/load usage

Call from Unity's main thread (buttons, MonoBehaviours, dialogue commands, etc.). No manager GameObject is required.

```csharp
SaveLoad.Save(0); // Create or overwrite slot 0.
SaveLoad.Load(0); // Restore the profile from slot 0.

if (!SaveLoad.Save(1))
    Debug.Log(SaveLoad.LastError);

if (SaveLoad.Load(1))
{
    // Refresh the UI or load your chosen scene here.
}
```

Both return `bool`. A failed load leaves the profile intact. Loading restores data only;
it does not start a new game, change scenes, or resume an in-progress dialogue/battle.
Do not call `PlayerProfile.Initialization()` after loading, since that resets the loaded data.
`TitleManager.OnClickLoad` is still a UI integration point for you to choose a slot/scene.

## Files and metadata

- Default folder: `Path.Combine(Application.persistentDataPath, "Saves")`.
- Filenames: `slot_0.json`, `slot_1.json`, etc. Any nonnegative integer is accepted.
- Find the exact location: `Debug.Log(SaveLoad.GetSavePath(0));`.
- Optional location override: `SaveLoad.SaveDirectory = myFolder;`. Set it to `null` to restore the default.
- JSON is indented UTF-8, editable, and unencrypted. Numeric gameplay values are not clamped.
- `GameVersion` comes from `Application.version` (Unity Player Settings → Version).
- `SavedAtUtc` is the real-world save date/time in ISO 8601 UTC, for example `2026-09-26T08:30:00.0000000Z`.
- `SchemaVersion` is the data-format version, separate from the game's release version.
- Overwriting a slot keeps the previous file at `slot_0.json.bak`. A temporary file is fully written before replacing the slot.
  To recover a backup manually, copy it over the corresponding `.json` file. Loading never silently substitutes a backup.

Inspect a slot for a load menu without applying it:

```csharp
if (SaveLoad.TryRead(0, out SaveData data, out string error))
{
    string version = data.GameVersion;
    string date = data.SavedAtUtc;
    int day = data.PlayerProfile.CurrentDate;
}
```

`TryRead` reports its own `error`; `LastError` describes the most recent `Save` or `Load` call.

## What is saved

`PlayerProfileSaveData` contains the current date/clock, technology unlocks, money,
research points, battle points, unlocked battler IDs/levels, and current body sprite name.
Snapshots copy arrays/lists/records so changing a snapshot does not mutate the active profile.

Body sprites are resolved by name through `Resources/TokaBodyList` (default sprite and `spriteList`).
Keep those names unique and stable. Register any selectable body sprite there before saving it.
A missing body name uses the default sprite; an unknown nonempty name fails loading with a useful error.
No Unity instance IDs or sprite asset references are written into JSON.

Technology unlocks currently follow `TechType` array order: append new enum entries before `maxCount`;
do not reorder old entries without a migration. Added entries load as `false` from shorter arrays.

## Adding more data

For another player field, edit these three places:

1. Add the runtime field/property in `PlayerProfile.cs`.
2. Add a public field with its desired old-save default in `PlayerProfileSaveData` (`SaveData.cs`).
3. Map it in `PlayerProfile.CaptureSaveData()` and `ApplySaveData()` (`PlayerProfile.Persistence.cs`).

For example, add `public int Reputation;` to the data class, capture the profile's reputation,
and apply it on load. Existing JSON without `Reputation` will load it as `0`.
Also decide/reset its new-game default in `PlayerProfile.Initialization()`.

For a new system (inventory, story flags, etc.), add a serializable data class and a field in `SaveData`.
Capture that system in `SaveLoad.Save()` and apply it in `Load()`.
Resolve and validate **all** systems before changing live state so failed loads remain all-or-nothing.
This uses the already installed Newtonsoft.Json package, so lists, nested classes, and dictionaries work.
Prefer stable string IDs over scene objects and ScriptableObject references.

Adding optional fields with defaults does not require a format-version bump. Avoid renaming a released
JSON field unless you keep its old JSON name with `[JsonProperty("OldName")]` or add a migration.
For incompatible changes, increment `SaveData.CurrentSchemaVersion` and implement the corresponding
step in `SaveLoad.UpgradeOneVersion(JObject json, int fromVersion)`. Each step upgrades one version;
the caller updates `SchemaVersion` and repeats as needed. A different `GameVersion` alone does not block loading.
Unsupported newer schemas fail with an error. Unknown JSON fields are ignored and will not survive re-saving.

## Verification

In Edit mode, run **Tools → Save Load → Run Verification**. It uses a separate temporary folder under
`Library`, restores the original profile afterwards, and never touches real save slots.
The Console and `Library/SaveLoadVerification-result.txt` report the outcome. Expected warning logs
exercise malformed/missing files, unsupported formats, invalid slots, and write errors.
