using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static partial class PlayerProfile
{
    /// <summary>Creates an independent snapshot for saving or inspecting the profile.</summary>
    public static PlayerProfileSaveData CaptureSaveData()
    {
        Sprite body = TokaCurrentBody;
        string bodyName = body != null ? body.name : null;
        if (body != null && ResolveBodySprite(bodyName) != body)
            throw new InvalidDataException("The current body sprite must be registered in Resources/TokaBodyList.");

        return new PlayerProfileSaveData
        {
            CurrentDate = currentDate,
            CurrentClock = currentClock,
            TechUnlockStatus = (bool[])techUnlockStatus.Clone(),
            Money = money,
            ResearchPoint = researchPoint,
            BattlePoint = battlePoint,
            Energy = energy,
            TokaBodySpriteName = bodyName,
            AvailableBattlerData = CopyBattlers(AvailableBattlerData)
        };
    }

    /// <summary>Resolves and prepares all values before replacing the current profile.</summary>
    public static void ApplySaveData(PlayerProfileSaveData data)
    {
        if (data == null)
            throw new InvalidDataException("The save file has no PlayerProfile data.");
        if (!Enum.IsDefined(typeof(Clock), data.CurrentClock))
            throw new InvalidDataException($"Unknown clock value: {data.CurrentClock}.");

        Sprite body = ResolveBodySprite(data.TokaBodySpriteName);
        var technologies = new bool[(int)TechType.maxCount];
        if (data.TechUnlockStatus != null)
            Array.Copy(data.TechUnlockStatus, technologies, Math.Min(data.TechUnlockStatus.Length, technologies.Length));
        List<AvailableBattlerRecord> battlers = CopyBattlers(data.AvailableBattlerData);

        // No gameplay limits are applied: edited resources and levels are preserved.
        // Use the public setters so UI subscribers see values restored from a save.
        CurrentDate = data.CurrentDate;
        CurrentClock = data.CurrentClock;
        techUnlockStatus = technologies;
        Money = data.Money;
        ResearchPoint = data.ResearchPoint;
        BattlePoint = data.BattlePoint;
        Energy = data.Energy;
        tokaCurrentBody = body;
        AvailableBattlerData.Clear();
        AvailableBattlerData.AddRange(battlers);
    }

    private static List<AvailableBattlerRecord> CopyBattlers(List<AvailableBattlerRecord> source)
    {
        var copy = new List<AvailableBattlerRecord>();
        if (source == null)
            return copy;

        foreach (AvailableBattlerRecord battler in source)
        {
            if (battler == null)
                throw new InvalidDataException("AvailableBattlerData contains a null record.");
            copy.Add(new AvailableBattlerRecord
            {
                BattlerID = battler.BattlerID,
                BattlerCurrentLevel = battler.BattlerCurrentLevel
            });
        }
        return copy;
    }

    private static Sprite ResolveBodySprite(string spriteName)
    {
        EnsureTokaBodyList();
        if (string.IsNullOrEmpty(spriteName))
            return tokabodylist != null ? tokabodylist.defaultSprite : null;

        Sprite match = null;
        if (tokabodylist != null)
        {
            if (tokabodylist.defaultSprite != null && tokabodylist.defaultSprite.name == spriteName)
                match = tokabodylist.defaultSprite;

            if (tokabodylist.spriteList != null)
            {
                foreach (Sprite sprite in tokabodylist.spriteList)
                {
                    if (sprite == null || sprite.name != spriteName)
                        continue;
                    if (match != null && match != sprite)
                        throw new InvalidDataException($"More than one body sprite is named '{spriteName}'. Give each body sprite a unique name.");
                    match = sprite;
                }
            }
        }

        if (match == null)
            throw new InvalidDataException($"Body sprite '{spriteName}' is missing from Resources/TokaBodyList.");
        return match;
    }
}
