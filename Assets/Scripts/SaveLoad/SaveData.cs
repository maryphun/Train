using System;
using System.Collections.Generic;

/// <summary>Plain JSON data. Add new systems as fields here.</summary>
[Serializable]
public sealed class SaveData
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion = CurrentSchemaVersion;
    public string GameVersion;
    public string SavedAtUtc;
    public PlayerProfileSaveData PlayerProfile;
}

/// <summary>Persistent values only; do not put Unity objects or scene references here.</summary>
[Serializable]
public sealed class PlayerProfileSaveData
{
    // These defaults also apply when an older JSON file is missing a new field.
    public int CurrentDate;
    public Clock CurrentClock = Clock.Morning;
    public bool[] TechUnlockStatus = new bool[(int)TechType.maxCount];
    public int Money;
    public int ResearchPoint;
    public int BattlePoint;
    public int Energy;
    public string TokaBodySpriteName;
    public List<AvailableBattlerRecord> AvailableBattlerData = new List<AvailableBattlerRecord>();
}
