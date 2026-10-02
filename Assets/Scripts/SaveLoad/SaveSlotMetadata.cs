using System;

/// <summary>Read-only preview of a slot. Reading it never changes PlayerProfile.</summary>
public sealed class SaveSlotMetadata
{
    public int SlotID { get; }
    public int SchemaVersion { get; }
    public string GameVersion { get; }
    public DateTimeOffset SavedAtUtc { get; }
    /// <summary>The saved instant displayed in this computer's current local time zone.</summary>
    public DateTimeOffset SavedAtLocal => SavedAtUtc.ToLocalTime();
    /// <summary>The raw saved PlayerProfile.CurrentDate, with no added day offset.</summary>
    public int InGameDay { get; }

    internal SaveSlotMetadata(int slotID, int schemaVersion, string gameVersion,
        DateTimeOffset savedAtUtc, int inGameDay)
    {
        SlotID = slotID;
        SchemaVersion = schemaVersion;
        GameVersion = gameVersion;
        SavedAtUtc = savedAtUtc.ToUniversalTime();
        InGameDay = inGameDay;
    }
}
