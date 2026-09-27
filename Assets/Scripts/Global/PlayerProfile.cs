using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public sealed class AvailableBattlerRecord
{
    public string BattlerID;
    public int BattlerCurrentLevel;
}

public static partial class PlayerProfile
{
    public static event System.Action<int> BattlePointChanged;
    public static event System.Action MoneyChanged;
    public static event System.Action ResearchPointChanged;
    public static event System.Action EnergyChanged;
    public static event System.Action DateChanged;
    public static event System.Action ClockChanged;

    // Data that need to be saved
    static int currentDate;
    static Clock currentClock;
    static bool[] techUnlockStatus = new bool[(int)TechType.maxCount];
    static int money;
    static int researchPoint;
    static int battlePoint;
    static int energy;
    static Sprite tokaCurrentBody;
    static public List<AvailableBattlerRecord> AvailableBattlerData { get; } = new List<AvailableBattlerRecord>();

    static TokaBodyList tokabodylist;

    static public int CurrentDate
    {
        get => currentDate;
        set
        {
            if (currentDate == value)
                return;

            currentDate = value;
            DateChanged?.Invoke();
        }
    }

    static public Clock CurrentClock
    {
        get => currentClock;
        set
        {
            if (!System.Enum.IsDefined(typeof(Clock), value))
                throw new System.ArgumentOutOfRangeException(nameof(value), value, "Unknown clock value.");
            currentClock = value;

            ClockChanged?.Invoke();
        }
    }

    static public int Money
    {
        get => money;
        set
        {
            if (money == value)
                return;

            money = value;
            MoneyChanged?.Invoke();
        }
    }

    static public int ResearchPoint
    {
        get => researchPoint;
        set
        {
            if (researchPoint == value)
                return;

            researchPoint = value;
            ResearchPointChanged?.Invoke();
        }
    }

    static public int Energy
    {
        get => energy;
        set
        {
            if (energy == value)
                return;

            energy = value;
            EnergyChanged?.Invoke();
        }
    }

    static public int BattlePoint
    {
        get => battlePoint;
        set
        {
            if (battlePoint == value)
                return;

            battlePoint = value;
            BattlePointChanged?.Invoke(battlePoint);
        }
    }

    static public Sprite TokaCurrentBody
    {
        get
        {
            EnsureTokaBodyList();

            if (tokaCurrentBody == null && tokabodylist != null)
            {
                tokaCurrentBody = tokabodylist.defaultSprite;
            }

            return tokaCurrentBody;
        }
        set => tokaCurrentBody = value;
    }

    // Data that don't need to be saved

    static public void Initialization()
    {
        EnsureTokaBodyList();

        currentDate = 0;
        currentClock = Clock.Morning;
        System.Array.Fill(techUnlockStatus, false); // set all tech unlock status into false
        money = 0;
        researchPoint = 0;
        BattlePoint = 0;
        Energy = 0;
        tokaCurrentBody = tokabodylist != null ? tokabodylist.defaultSprite : null;
        AvailableBattlerData.Clear();
    }

    static void EnsureTokaBodyList()
    {
        if (tokabodylist == null)
        {
            tokabodylist = Resources.Load<TokaBodyList>("TokaBodyList");
        }
    }
}
