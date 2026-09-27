using System;
using UnityEngine;

/// <summary>Play Mode-only commands for the live profile. Never initializes or saves it.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Debug/Player Profile Debugger")]
public sealed class PlayerProfileDebugger : MonoBehaviour
{
    private static PlayerProfileDebugger instance;

    public static PlayerProfileDebugger Instance
    {
        get
        {
            // Also recovers when domain reload is disabled or scripts reload during Play Mode.
            if (instance == null && Application.isPlaying)
                instance = FindFirstObjectByType<PlayerProfileDebugger>();
            return instance;
        }
    }

    public bool CanEdit => Application.isPlaying && isActiveAndEnabled && Instance == this;
    public string LastMessage { get; private set; }
    public bool LastCommandFailed { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance() => instance = null;

    private void Awake() => RegisterInstance();
    private void OnEnable() => RegisterInstance();

    private void RegisterInstance()
    {
        if (!Application.isPlaying)
            return;
        if (instance != null && instance != this)
        {
            // Do not destroy unrelated components if somebody attaches a duplicate elsewhere.
            enabled = false;
            Destroy(this);
            return;
        }

        instance = this;
        if (transform.parent == null)
            DontDestroyOnLoad(gameObject);
        else
            Debug.LogWarning("PlayerProfileDebugger must be on a root GameObject to persist across scenes.", this);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public bool SetMoney(int value) => Execute(() => PlayerProfile.Money = value, "Money updated.");
    public bool AddMoney(int amount) => Execute(() => PlayerProfile.Money = checked(PlayerProfile.Money + amount), "Money added.");
    public bool SetResearchPoint(int value) => Execute(() => PlayerProfile.ResearchPoint = value, "Research points updated.");
    public bool AddResearchPoint(int amount) => Execute(() => PlayerProfile.ResearchPoint = checked(PlayerProfile.ResearchPoint + amount), "Research points added.");
    public bool SetBattlePoint(int value) => Execute(() => PlayerProfile.BattlePoint = value, "Battle points updated.");
    public bool AddBattlePoint(int amount) => Execute(() => PlayerProfile.BattlePoint = checked(PlayerProfile.BattlePoint + amount), "Battle points added.");
    public bool SetEnergy(int value) => Execute(() => PlayerProfile.Energy = value, "Energy updated.");
    public bool AddEnergy(int amount) => Execute(() => PlayerProfile.Energy = checked(PlayerProfile.Energy + amount), "Energy added.");
    public bool SetCurrentDate(int value) => Execute(() => PlayerProfile.CurrentDate = value, "Current date updated.");
    public bool SetCurrentTime(Clock value) => Execute(() => PlayerProfile.CurrentClock = value, "Current time updated.");

    public bool AddOrUpdateBattler(BattlerData data, int level)
    {
        return Execute(() =>
        {
            if (data == null)
                throw new ArgumentException("Assign a BattlerData asset first.");
            if (string.IsNullOrWhiteSpace(data.BattlerID))
                throw new ArgumentException("The selected BattlerData needs a non-empty BattlerID.");

            AvailableBattlerRecord record = PlayerProfile.AvailableBattlerData.Find(
                item => item != null && string.Equals(item.BattlerID, data.BattlerID, StringComparison.Ordinal));
            if (record != null)
                record.BattlerCurrentLevel = level;
            else
                PlayerProfile.AvailableBattlerData.Add(new AvailableBattlerRecord
                {
                    BattlerID = data.BattlerID,
                    BattlerCurrentLevel = level
                });
        }, "Battler added or level updated. Reopen the hire panel to rebuild its list.");
    }

    public bool SetBattlerLevel(string battlerID, int level)
    {
        return Execute(() =>
        {
            AvailableBattlerRecord record = PlayerProfile.AvailableBattlerData.Find(
                item => item != null && string.Equals(item.BattlerID, battlerID, StringComparison.Ordinal));
            if (record == null)
                throw new ArgumentException("This battler is no longer in the live profile.");
            record.BattlerCurrentLevel = level;
        }, "Battler level updated.");
    }

    private bool Execute(Action command, string successMessage)
    {
        if (!CanEdit)
        {
            LastCommandFailed = true;
            LastMessage = "Commands require the active singleton in Play Mode.";
            return false;
        }

        try
        {
            command();
            LastCommandFailed = false;
            LastMessage = successMessage;
            return true;
        }
        catch (ArgumentException exception)
        {
            LastCommandFailed = true;
            LastMessage = exception.Message;
            return false;
        }
        catch (OverflowException)
        {
            LastCommandFailed = true;
            LastMessage = "The result exceeds the integer range. The value was not changed.";
            return false;
        }
    }
}
