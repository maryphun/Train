# Save/load usage

Call from Unity's main thread (buttons, MonoBehaviours, dialogue commands, etc.). No manager GameObject is required.

```csharp
SaveLoad.Save(0); // Create or overwrite slot 0.
SaveLoad.Load(0); // Restore the profile from slot 0.

if (!SaveLoad.Save(1))
    Debug.Log(SaveLoad.LastError);

if (SaveLoad.Load(1))
{
    // Load your chosen scene here if needed.
}
```

Both return `bool`. A failed load leaves the profile intact. Loading restores data only;
it does not start a new game, change scenes, or resume an in-progress dialogue/battle.
Profile change events refresh active subscribers such as `MainMenuManager`; other UI may need
its own refresh after loading.
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

## Save/load menu previews (no profile load)

`Save()` already records the real-world date/time in `SavedAtUtc` and the in-game day in
`PlayerProfile.CurrentDate`. No additional call is required when saving. Read them with:

```csharp
int slotID = 0;
if (!SaveLoad.Exists(slotID))
{
    // Show an empty slot.
}
else if (SaveLoad.TryReadMetadata(slotID, out SaveSlotMetadata info, out string error))
{
    var local = info.SavedAtLocal;
    calendarDateText.text = local.ToString("yyyy/MM/dd");
    clockTimeText.text = local.ToString("HH:mm:ss");
    gameDayText.text = $"Day {info.InGameDay}";
}
else
{
    // File exists, but its preview could not be read. Do not label it an empty slot.
    Debug.LogWarning(error);
}
```

- `Exists(slotID)` is a quick file-presence check. It does not parse JSON or guarantee loadability.
- `TryReadMetadata` never calls `Load`, reads the live profile, fires its events, or resolves sprites.
  It streams the JSON into a small projection, skipping unrelated properties instead of allocating
  the full save/profile, battler lists or technology arrays. It still scans the file; it is not a
  constant-time header/index lookup. Call when opening/refreshing the menu, not every frame.
- Fields: `SlotID`, `SchemaVersion`, `GameVersion`, `SavedAtUtc`, `SavedAtLocal`, `InGameDay`.
- `SavedAtLocal` converts the saved UTC instant to the computer's **current local time zone**.
  The save does not record the original time-zone identity. Use `SavedAtUtc` if UTC display is desired.
- `InGameDay` is the raw saved `CurrentDate`, with no automatic +1 offset. A missing day defaults to
  0, matching full-load behavior. The real-world clock is not the in-game `Clock` enum.
- Existing normal saves work without resaving. A missing/invalid timestamp, malformed JSON, missing
  profile or unsupported schema returns `false`, null metadata and an error. No file-modification
  date is substituted for missing metadata.
- Successful preview reading does not validate all gameplay fields: loading can still fail later
  (for example, a removed body sprite). Always check the result of `SaveLoad.Load` on selection.
- This method returns its own `error` and leaves `SaveLoad.LastError` unchanged.
- No separate metadata file or duplicated game-day value is written, so previews reflect the JSON
  even after a player edits a save. Existing file replacement/backup behavior remains unchanged.

If you need the **entire saved DTO** rather than the small preview, use `TryRead` without applying it:

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
research points, battle points, Energy, unlocked battler IDs/levels, and current body sprite name.
Older save files without Energy load it as `0`; no schema migration is required.
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
