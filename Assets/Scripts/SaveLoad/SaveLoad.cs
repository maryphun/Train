using System;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>Synchronous JSON saves. Call from Unity's main thread; no GameObject is needed.</summary>
public static class SaveLoad
{
    private static string saveDirectoryOverride;
    private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        Formatting = Formatting.Indented,
        DateParseHandling = DateParseHandling.None,
        Converters = { new StringEnumConverter() }
    };

    /// <summary>Set to a custom directory, or null to restore the default location.</summary>
    public static string SaveDirectory
    {
        get => saveDirectoryOverride ?? Path.Combine(Application.persistentDataPath, "Saves");
        set => saveDirectoryOverride = string.IsNullOrWhiteSpace(value) ? null : Path.GetFullPath(value);
    }

    public static string LastError { get; private set; }

    public static string GetSavePath(int memoryslotID)
    {
        if (memoryslotID < 0)
            throw new ArgumentOutOfRangeException(nameof(memoryslotID), "Save slot IDs must be zero or greater.");
        return Path.Combine(SaveDirectory, $"slot_{memoryslotID.ToString(CultureInfo.InvariantCulture)}.json");
    }

    /// <summary>Fast file-existence check only; does not parse or validate the save.</summary>
    public static bool Exists(int memoryslotID)
    {
        return memoryslotID >= 0 && File.Exists(GetSavePath(memoryslotID));
    }

    /// <summary>perform an autosave.</summary>
    public static bool AutoSave()
    {
        return Save(4);
    }

    /// <summary>Writes the profile to the slot. An overwritten slot is retained as .json.bak.</summary>
    public static bool Save(int memoryslotID)
    {
        LastError = null;
        string temporaryPath = null;
        try
        {
            string path = GetSavePath(memoryslotID);
            var data = new SaveData
            {
                GameVersion = Application.version,
                SavedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                PlayerProfile = PlayerProfile.CaptureSaveData()
            };
            string json = JsonConvert.SerializeObject(data, JsonSettings);
            Directory.CreateDirectory(SaveDirectory);

            // Finish writing first, then replace the slot so a failed write keeps the old save.
            temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            byte[] bytes = new UTF8Encoding(false).GetBytes(json);
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }

            if (File.Exists(path))
                File.Replace(temporaryPath, path, path + ".bak");
            else
                File.Move(temporaryPath, path);
            return true;
        }
        catch (Exception exception)
        {
            return ReportFailure("save", memoryslotID, exception.Message);
        }
        finally
        {
            if (temporaryPath != null)
            {
                try
                {
                    if (File.Exists(temporaryPath))
                        File.Delete(temporaryPath);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Could not clean up temporary save '{temporaryPath}': {exception.Message}");
                }
            }
        }
    }

    /// <summary>Restores the profile only. The caller chooses scene transitions and UI refreshes.</summary>
    public static bool Load(int memoryslotID)
    {
        LastError = null;
        if (!TryRead(memoryslotID, out SaveData data, out string error))
            return ReportFailure("load", memoryslotID, error);

        try
        {
            PlayerProfile.ApplySaveData(data.PlayerProfile);
            return true;
        }
        catch (Exception exception)
        {
            return ReportFailure("load", memoryslotID, exception.Message);
        }
    }

    /// <summary>
    /// Reads only preview fields through a streaming reader, without constructing the full save/profile,
    /// resolving assets, or changing live data. Scans the JSON file but skips unrelated values.
    /// Success verifies preview fields, not that every gameplay field can be loaded.
    /// Errors are returned separately and do not change LastError.
    /// </summary>
    public static bool TryReadMetadata(int memoryslotID, out SaveSlotMetadata metadata, out string error)
    {
        metadata = null;
        error = null;
        try
        {
            string path = GetSavePath(memoryslotID);
            if (!File.Exists(path))
                throw new FileNotFoundException($"No save exists in slot {memoryslotID}.", path);

            SlotPreviewData preview;
            using (var stream = new StreamReader(path, Encoding.UTF8))
            using (var reader = new JsonTextReader(stream) { DateParseHandling = DateParseHandling.None })
            {
                JsonSerializer serializer = JsonSerializer.Create(JsonSettings);
                serializer.CheckAdditionalContent = true;
                preview = serializer.Deserialize<SlotPreviewData>(reader);
            }

            if (preview == null || !preview.SchemaVersion.HasValue)
                throw new InvalidDataException("The save file is missing its SchemaVersion.");
            if (preview.SchemaVersion < 1 || preview.SchemaVersion > SaveData.CurrentSchemaVersion)
                throw new InvalidDataException($"Save format {preview.SchemaVersion} is not supported.");
            if (preview.PlayerProfile == null)
                throw new InvalidDataException("The save file has no PlayerProfile object.");
            // Save() has always emitted the round-trip UTC format. Do not invent a timestamp
            // from file modification time when a hand-edited/old file lacks valid metadata.
            if (!DateTimeOffset.TryParseExact(preview.SavedAtUtc, "O", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out DateTimeOffset savedAt))
                throw new InvalidDataException("The save file has no valid SavedAtUtc timestamp.");

            metadata = new SaveSlotMetadata(memoryslotID, preview.SchemaVersion.Value,
                preview.GameVersion, savedAt, preview.PlayerProfile.CurrentDate);
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }

    // A small projection of the existing JSON format: no duplicate day field or sidecar to go stale.
    [JsonObject(MemberSerialization.OptIn)]
    private sealed class SlotPreviewData
    {
        [JsonProperty] public int? SchemaVersion { get; set; }
        [JsonProperty] public string GameVersion { get; set; }
        [JsonProperty] public string SavedAtUtc { get; set; }
        [JsonProperty] public ProfilePreviewData PlayerProfile { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class ProfilePreviewData
    {
        [JsonProperty] public int CurrentDate { get; set; }
    }

    /// <summary>Reads the full save DTO without changing the active profile. Prefer TryReadMetadata for previews.</summary>
    public static bool TryRead(int memoryslotID, out SaveData data, out string error)
    {
        data = null;
        error = null;
        try
        {
            string path = GetSavePath(memoryslotID);
            if (!File.Exists(path))
                throw new FileNotFoundException($"No save exists in slot {memoryslotID}.", path);

            // Keep ISO timestamps as strings while inspecting/migrating the JSON tree.
            JObject json = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(path, Encoding.UTF8), JsonSettings);
            if (json == null)
                throw new InvalidDataException("The save file must contain a JSON object.");
            UpgradeSaveData(json);
            if (!(json[nameof(SaveData.PlayerProfile)] is JObject))
                throw new InvalidDataException("The save file has no PlayerProfile object.");

            data = json.ToObject<SaveData>(JsonSerializer.Create(JsonSettings));
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }

    private static void UpgradeSaveData(JObject json)
    {
        JToken versionToken = json[nameof(SaveData.SchemaVersion)];
        if (versionToken == null || versionToken.Type != JTokenType.Integer)
            throw new InvalidDataException("The save file is missing its integer SchemaVersion.");

        int version = versionToken.Value<int>();
        if (version < 1 || version > SaveData.CurrentSchemaVersion)
            throw new InvalidDataException($"Save format {version} is not supported by this game (current: {SaveData.CurrentSchemaVersion}).");

        // Add an upgrade step here when changing/removing fields in a released save format.
        // Adding optional fields with defaults does not require a new schema version.
        while (version < SaveData.CurrentSchemaVersion)
        {
            UpgradeOneVersion(json, version);
            json[nameof(SaveData.SchemaVersion)] = ++version;
        }
    }

    private static void UpgradeOneVersion(JObject json, int fromVersion)
    {
        // Future example: case 1 renames a JSON field, then returns (upgrading 1 -> 2).
        // No older save formats exist yet, so no migrations are needed for version 1.
        throw new InvalidDataException($"No migration has been defined from save format {fromVersion}.");
    }

    private static bool ReportFailure(string operation, int memoryslotID, string error)
    {
        LastError = error;
        Debug.LogWarning($"Could not {operation} slot {memoryslotID}: {error}");
        return false;
    }
}
