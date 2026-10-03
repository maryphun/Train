using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class SaveLoadVerification
{
    private const string RequestPath = "Library/SaveLoadVerification.request";
    private const string ResultPath = "Library/SaveLoadVerification-result.txt";

    // Also allows an automated Editor check: create the request file, then recompile.
    [InitializeOnLoadMethod]
    private static void RunWhenRequested()
    {
        if (!File.Exists(RequestPath))
            return;
        EditorApplication.delayCall += Run;
    }

    [MenuItem("Tools/Save Load/Run Verification")]
    public static void Run()
    {
        if (File.Exists(RequestPath))
            File.Delete(RequestPath);
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            File.WriteAllText(ResultPath, "SKIPPED: Exit Play mode before running save/load verification.");
            Debug.LogWarning("Exit Play mode before running save/load verification.");
            return;
        }

        string originalDirectory = SaveLoad.SaveDirectory;
        PlayerProfileSaveData original = null;
        string testDirectory = Path.GetFullPath(Path.Combine("Library", "SaveLoadVerification", Guid.NewGuid().ToString("N")));
        try
        {
            original = PlayerProfile.CaptureSaveData();
            SaveLoad.SaveDirectory = testDirectory;
            Verify();
            string result = "PASS: round-trip, slot isolation, metadata, backups, edited JSON, missing-field defaults, "
                + "copy isolation, tutorial compatibility, new-game reset, invalid slot, missing file, malformed JSON, future schema, "
                + "missing body sprite, and failed-write preservation. " + DateTime.UtcNow.ToString("O");
            File.WriteAllText(ResultPath, result);
            Debug.Log(result);
        }
        catch (Exception exception)
        {
            File.WriteAllText(ResultPath, "FAIL: " + exception);
            Debug.LogException(exception);
        }
        finally
        {
            SaveLoad.SaveDirectory = originalDirectory;
            if (original != null)
                PlayerProfile.ApplySaveData(original);
            // Only this run's freshly created directory is removed; real save slots are untouched.
            if (Directory.Exists(testDirectory))
                Directory.Delete(testDirectory, true);
        }
    }

    private static void Verify()
    {
        TokaBodyList bodies = Resources.Load<TokaBodyList>("TokaBodyList");
        Sprite testBody = PlayerProfile.TokaCurrentBody;
        if (bodies != null && bodies.spriteList != null)
            testBody = bodies.spriteList.Find(sprite => sprite != null && sprite != bodies.defaultSprite) ?? testBody;
        var profile = new PlayerProfileSaveData
        {
            CurrentDate = 7,
            CurrentClock = Clock.Night,
            Money = 1234,
            ResearchPoint = 56,
            BattlePoint = 78,
            Energy = 3,
            TokaBodySpriteName = testBody != null ? testBody.name : null
        };
        profile.TechUnlockStatus[0] = true;
        profile.IsTutorialTriggered[(int)Tutorials.MainMenu] = true;
        profile.AvailableBattlerData.Add(new AvailableBattlerRecord { BattlerID = "テスト怪人", BattlerCurrentLevel = 4 });
        PlayerProfile.ApplySaveData(profile);
        string expectedProfile = Snapshot();

        // Neither capture nor apply may share mutable collections with the running profile.
        profile.AvailableBattlerData[0].BattlerCurrentLevel = 999;
        profile.TechUnlockStatus[0] = false;
        profile.IsTutorialTriggered[(int)Tutorials.MainMenu] = false;
        PlayerProfileSaveData captured = PlayerProfile.CaptureSaveData();
        captured.AvailableBattlerData.Clear();
        captured.TechUnlockStatus[0] = false;
        captured.IsTutorialTriggered[(int)Tutorials.MainMenu] = false;
        Check(Snapshot() == expectedProfile, "Profile snapshots share mutable data.");

        Check(SaveLoad.Save(0), SaveLoad.LastError);
        Check(SaveLoad.Exists(0), "Slot 0 was not created.");
        Check(SaveLoad.TryRead(0, out SaveData saved, out string error), error);
        Check(saved.GameVersion == Application.version, "Game version was not recorded.");
        Check(saved.SchemaVersion == SaveData.CurrentSchemaVersion, "Schema version was not recorded.");
        Check(saved.PlayerProfile.Energy == 3, "Energy was not saved.");
        Check(saved.PlayerProfile.IsTutorialTriggered[(int)Tutorials.MainMenu], "Tutorial flags were not saved.");
        VerifyMetadata();
        Check(DateTime.TryParse(saved.SavedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime timestamp)
            && timestamp.Kind == DateTimeKind.Utc && Math.Abs((DateTime.UtcNow - timestamp).TotalMinutes) < 1,
            "Save timestamp was not recorded as current UTC.");
        int energyNotifications = 0;
        Action energyListener = () => energyNotifications++;
        PlayerProfile.EnergyChanged += energyListener;
        try
        {
            PlayerProfile.Initialization();
            Check(PlayerProfile.BattlePoint == 0 && PlayerProfile.Energy == 0 && energyNotifications == 1,
                "New game retained battle points or Energy, or did not notify Energy subscribers.");
            Check(Array.TrueForAll(PlayerProfile.CaptureSaveData().IsTutorialTriggered, flag => !flag),
                "New game retained tutorial flags.");
            Check(SaveLoad.Load(0), SaveLoad.LastError);
            Check(Snapshot() == expectedProfile && energyNotifications == 2,
                "Profile did not round-trip or loading did not notify Energy subscribers.");
        }
        finally
        {
            PlayerProfile.EnergyChanged -= energyListener;
        }

        PlayerProfile.Money = 2222;
        Check(SaveLoad.Save(1), SaveLoad.LastError);
        Check(SaveLoad.Load(0) && PlayerProfile.Money == 1234, "Saving slot 1 changed slot 0.");
        string slot = SaveLoad.GetSavePath(0);
        string previousJson = File.ReadAllText(slot);
        PlayerProfile.Money = 4321;
        Check(SaveLoad.Save(0), SaveLoad.LastError);
        Check(File.ReadAllText(slot + ".bak") == previousJson, "Overwrite did not preserve the previous slot.");
        PlayerProfile.Money = 9876;
        Check(SaveLoad.Save(0), SaveLoad.LastError);
        Check(JObject.Parse(File.ReadAllText(slot + ".bak"))["PlayerProfile"]["Money"].Value<int>() == 4321,
            "Second overwrite did not update the backup.");

        JObject edited = JObject.Parse(File.ReadAllText(slot));
        edited["GameVersion"] = "modded-build";
        edited["PlayerProfile"]["Money"] = -123;
        edited["PlayerProfile"]["ResearchPoint"] = 1000000;
        edited["PlayerProfile"]["Energy"] = -5;
        File.WriteAllText(slot, edited.ToString());
        Check(SaveLoad.Load(0) && PlayerProfile.Money == -123 && PlayerProfile.ResearchPoint == 1000000
            && PlayerProfile.Energy == -5,
            "Hand-edited values were rejected or clamped.");

        // Simulates an older save missing fields added later.
        File.WriteAllText(slot, "{\"SchemaVersion\":1,\"PlayerProfile\":{\"Money\":42,\"TechUnlockStatus\":[true]}}");
        Check(SaveLoad.Load(0), SaveLoad.LastError);
        PlayerProfileSaveData defaults = PlayerProfile.CaptureSaveData();
        Check(defaults.Money == 42 && defaults.BattlePoint == 0 && defaults.ResearchPoint == 0
            && defaults.Energy == 0
            && defaults.IsTutorialTriggered.Length == (int)Tutorials.maxCount
            && Array.TrueForAll(defaults.IsTutorialTriggered, flag => !flag)
            && defaults.CurrentClock == Clock.Morning && defaults.AvailableBattlerData.Count == 0
            && defaults.TechUnlockStatus.Length == (int)TechType.maxCount && defaults.TechUnlockStatus[0],
            "Missing fields did not use defaults or the technology list did not expand.");

        VerifyTutorialCompatibility(slot);

        ExpectLoadFailure(slot, "{broken json");
        ExpectLoadFailure(slot, "{}");
        ExpectLoadFailure(slot, "{\"SchemaVersion\":999,\"PlayerProfile\":{}}");
        ExpectLoadFailure(slot, "{\"SchemaVersion\":1,\"PlayerProfile\":{\"CurrentClock\":\"UnknownClock\"}}");
        ExpectLoadFailure(slot, "{\"SchemaVersion\":1,\"PlayerProfile\":{\"AvailableBattlerData\":[null]}}");
        ExpectLoadFailure(slot, "{\"SchemaVersion\":1,\"PlayerProfile\":{\"TokaBodySpriteName\":\"missing-test-body\"}}");
        string beforeFailures = Snapshot();
        Check(!SaveLoad.Load(99) && Snapshot() == beforeFailures, "Missing slot changed the profile.");
        Check(!SaveLoad.Save(-1) && !SaveLoad.Load(-1) && Snapshot() == beforeFailures, "Invalid slot was accepted.");

        string validDirectory = SaveLoad.SaveDirectory;
        string blocker = Path.Combine(validDirectory, "not-a-directory");
        File.WriteAllText(blocker, "keep this file");
        SaveLoad.SaveDirectory = blocker;
        Check(!SaveLoad.Save(0) && File.ReadAllText(blocker) == "keep this file" && Snapshot() == beforeFailures,
            "Failed write damaged existing data.");
        SaveLoad.SaveDirectory = validDirectory;
    }

    private static void VerifyTutorialCompatibility(string slot)
    {
        // The first array simulates a save made before Battle (or later tutorials) existed.
        foreach (string flags in new[] { "[true]", "[]", "null", "[true,false,true]" })
        {
            PlayerProfile.SetTutorialTriggered(Tutorials.Battle, true);
            File.WriteAllText(slot, "{\"SchemaVersion\":1,\"PlayerProfile\":{\"IsTutorialTriggered\":" + flags + "}}");
            Check(SaveLoad.Load(0), SaveLoad.LastError);
            bool[] loaded = PlayerProfile.CaptureSaveData().IsTutorialTriggered;
            Check(loaded.Length == (int)Tutorials.maxCount, "Loaded tutorial array has the wrong length.");
            Check(PlayerProfile.GetTutorialTriggered(Tutorials.MainMenu) == flags.StartsWith("[true")
                && !PlayerProfile.GetTutorialTriggered(Tutorials.Battle),
                "Tutorial loading lost old flags or retained stale/new flags.");
        }

        PlayerProfile.SetTutorialTriggered(Tutorials.Battle, true);
        Check(PlayerProfile.GetTutorialTriggered(Tutorials.Battle), "Tutorial setter did not update the flag.");
        PlayerProfile.SetTutorialTriggered(Tutorials.Battle, false);
        Check(!PlayerProfile.GetTutorialTriggered(Tutorials.Battle), "Tutorial setter could not clear the flag.");
        foreach (Tutorials invalid in new[] { (Tutorials)(-1), Tutorials.maxCount, (Tutorials)int.MaxValue })
        {
            string before = Snapshot();
            bool getRejected = false;
            bool setRejected = false;
            try { PlayerProfile.GetTutorialTriggered(invalid); }
            catch (ArgumentOutOfRangeException) { getRejected = true; }
            try { PlayerProfile.SetTutorialTriggered(invalid, true); }
            catch (ArgumentOutOfRangeException) { setRejected = true; }
            Check(getRejected && setRejected && Snapshot() == before, "Invalid tutorial ID was accepted or changed the profile.");
        }
    }

    private static void VerifyMetadata()
    {
        string before = Snapshot();
        Check(SaveLoad.TryReadMetadata(0, out SaveSlotMetadata metadata, out string error), error);
        Check(metadata.SlotID == 0 && metadata.InGameDay == 7 && metadata.GameVersion == Application.version,
            "Preview values do not match the saved slot.");
        Check(Math.Abs((metadata.SavedAtUtc - DateTimeOffset.UtcNow).TotalMinutes) < 1
            && metadata.SavedAtLocal == metadata.SavedAtUtc, "Preview timestamp changed the saved instant.");

        string path = SaveLoad.GetSavePath(90);
        const string stamp = "2026-10-02T23:45:12.0000000Z";
        File.WriteAllText(path, "{\"SchemaVersion\":1,\"GameVersion\":\"old\",\"SavedAtUtc\":\"" + stamp
            + "\",\"PlayerProfile\":{\"CurrentDate\":31,\"TokaBodySpriteName\":\"missing-body\","
            + "\"AvailableBattlerData\":[null],\"Money\":\"not-an-integer\"}}");
        Check(SaveLoad.TryReadMetadata(90, out metadata, out error) && metadata.InGameDay == 31,
            "Preview tried to deserialize unrelated gameplay fields or resolve assets: " + error);
        Check(metadata.SavedAtUtc == new DateTimeOffset(2026, 10, 2, 23, 45, 12, TimeSpan.Zero),
            "Preview UTC parsing failed.");
        Check(metadata.SavedAtLocal == metadata.SavedAtUtc.ToLocalTime()
            && metadata.SavedAtLocal.Offset == TimeZoneInfo.Local.GetUtcOffset(metadata.SavedAtUtc),
            "Preview local time conversion failed.");
        Check(SaveLoad.Exists(90) && !SaveLoad.Exists(91) && !SaveLoad.Exists(-1), "Slot existence check failed.");
        Check(!SaveLoad.TryReadMetadata(91, out metadata, out error) && metadata == null && !string.IsNullOrEmpty(error),
            "Missing metadata slot should return an error.");
        Check(!SaveLoad.TryReadMetadata(-1, out metadata, out error), "Negative preview slot accepted.");
        foreach (string invalid in new[]
        {
            "{broken", "null", "[]", "{}",
            "{\"SchemaVersion\":999,\"PlayerProfile\":{}}",
            "{\"SchemaVersion\":1,\"PlayerProfile\":{}}",
            "{\"SchemaVersion\":1,\"SavedAtUtc\":\"invalid\",\"PlayerProfile\":{}}",
            "{\"SchemaVersion\":1,\"SavedAtUtc\":\"" + stamp + "\"}",
            "{\"SchemaVersion\":1,\"SavedAtUtc\":\"" + stamp + "\",\"PlayerProfile\":{}} trailing"
        })
        {
            File.WriteAllText(path, invalid);
            Check(SaveLoad.Exists(90), "Exists should test presence, not validity.");
            Check(!SaveLoad.TryReadMetadata(90, out metadata, out error) && metadata == null && !string.IsNullOrEmpty(error),
                "Invalid metadata accepted: " + invalid);
        }
        Check(Snapshot() == before, "Reading metadata changed the active profile.");
    }

    private static void ExpectLoadFailure(string path, string json)
    {
        string before = Snapshot();
        File.WriteAllText(path, json);
        Check(!SaveLoad.Load(0) && !string.IsNullOrEmpty(SaveLoad.LastError), "Invalid save was accepted.");
        Check(Snapshot() == before, "A failed load modified the profile.");
    }

    private static string Snapshot() => JsonConvert.SerializeObject(PlayerProfile.CaptureSaveData());

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message ?? "Save/load verification failed.");
    }
}
