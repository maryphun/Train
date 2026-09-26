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
                + "copy isolation, new-game reset, invalid slot, missing file, malformed JSON, future schema, "
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
            TokaBodySpriteName = testBody != null ? testBody.name : null
        };
        profile.TechUnlockStatus[0] = true;
        profile.AvailableBattlerData.Add(new AvailableBattlerRecord { BattlerID = "テスト怪人", BattlerCurrentLevel = 4 });
        PlayerProfile.ApplySaveData(profile);
        string expectedProfile = Snapshot();

        // Neither capture nor apply may share mutable collections with the running profile.
        profile.AvailableBattlerData[0].BattlerCurrentLevel = 999;
        profile.TechUnlockStatus[0] = false;
        PlayerProfileSaveData captured = PlayerProfile.CaptureSaveData();
        captured.AvailableBattlerData.Clear();
        captured.TechUnlockStatus[0] = false;
        Check(Snapshot() == expectedProfile, "Profile snapshots share mutable data.");

        Check(SaveLoad.Save(0), SaveLoad.LastError);
        Check(SaveLoad.Exists(0), "Slot 0 was not created.");
        Check(SaveLoad.TryRead(0, out SaveData saved, out string error), error);
        Check(saved.GameVersion == Application.version, "Game version was not recorded.");
        Check(saved.SchemaVersion == SaveData.CurrentSchemaVersion, "Schema version was not recorded.");
        Check(DateTime.TryParse(saved.SavedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime timestamp)
            && timestamp.Kind == DateTimeKind.Utc && Math.Abs((DateTime.UtcNow - timestamp).TotalMinutes) < 1,
            "Save timestamp was not recorded as current UTC.");
        PlayerProfile.Initialization();
        Check(PlayerProfile.BattlePoint == 0, "New game retained battle points.");
        Check(SaveLoad.Load(0), SaveLoad.LastError);
        Check(Snapshot() == expectedProfile, "Profile did not round-trip.");

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
        File.WriteAllText(slot, edited.ToString());
        Check(SaveLoad.Load(0) && PlayerProfile.Money == -123 && PlayerProfile.ResearchPoint == 1000000,
            "Hand-edited values were rejected or clamped.");

        // Simulates an older save missing fields added later.
        File.WriteAllText(slot, "{\"SchemaVersion\":1,\"PlayerProfile\":{\"Money\":42,\"TechUnlockStatus\":[true]}}");
        Check(SaveLoad.Load(0), SaveLoad.LastError);
        PlayerProfileSaveData defaults = PlayerProfile.CaptureSaveData();
        Check(defaults.Money == 42 && defaults.BattlePoint == 0 && defaults.ResearchPoint == 0
            && defaults.CurrentClock == Clock.Morning && defaults.AvailableBattlerData.Count == 0
            && defaults.TechUnlockStatus.Length == (int)TechType.maxCount && defaults.TechUnlockStatus[0],
            "Missing fields did not use defaults or the technology list did not expand.");

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
