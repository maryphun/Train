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
    public static event System.Action TokaBodyChanged;

    // Data that need to be saved
    static int currentDate;
    static Clock currentClock;
    static bool[] techUnlockStatus = new bool[(int)TechType.maxCount];
    static int money;
    static int researchPoint;
    static int battlePoint;
    static int energy;
    static Sprite tokaCurrentBody;
    static List<int> tokaAvailableBody = new List<int>();
    static public List<AvailableBattlerRecord> AvailableBattlerData { get; } = new List<AvailableBattlerRecord>();
    static bool[] isTutorialTriggered = new bool[(int)Tutorials.maxCount];

    static TokaBodyList tokabodylist;

    public static IReadOnlyList<int> TokaAvailableBody => tokaAvailableBody;

    public static bool GetTutorialTriggered(Tutorials tutorial)
    {
        return isTutorialTriggered[GetTutorialIndex(tutorial)];
    }

    public static void SetTutorialTriggered(Tutorials tutorial, bool triggered)
    {
        isTutorialTriggered[GetTutorialIndex(tutorial)] = triggered;
    }

    private static int GetTutorialIndex(Tutorials tutorial)
    {
        int index = (int)tutorial;
        if (index < 0 || index >= (int)Tutorials.maxCount
            || !System.Enum.IsDefined(typeof(Tutorials), tutorial))
            throw new System.ArgumentOutOfRangeException(nameof(tutorial), tutorial, "Unknown tutorial value.");
        return index;
    }

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
        set
        {
            if (tokaCurrentBody == value)
                return;
            tokaCurrentBody = value;
            TokaBodyChanged?.Invoke();
        }
    }


    // Data that don't need to be saved

    static public void Initialization()
    {
        EnsureTokaBodyList();

        currentDate = 1;
        currentClock = Clock.Evening;
        System.Array.Fill(techUnlockStatus, false); // set all tech unlock status into false
        money = 10000;
        researchPoint = 0;
        BattlePoint = 0;
        Energy = 4;
        AvailableBattlerData.Clear();
        isTutorialTriggered = new bool[(int)Tutorials.maxCount];
        tokaAvailableBody = CreateDefaultAvailableBodies();
        TokaCurrentBody = tokabodylist != null ? tokabodylist.defaultSprite : null;
    }

    // Shared by new-game initialization and saves that predate available-body tracking.
    internal static List<int> CreateDefaultAvailableBodies() => new List<int> { 0, 2, 3 };

    static void EnsureTokaBodyList()
    {
        if (tokabodylist == null)
        {
            tokabodylist = Resources.Load<TokaBodyList>("TokaBodyList");
        }
    }
}
