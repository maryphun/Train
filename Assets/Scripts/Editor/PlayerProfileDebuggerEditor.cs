using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerProfileDebugger))]
public sealed class PlayerProfileDebuggerEditor : Editor
{
    private int moneyToAdd;
    private int researchToAdd;
    private int battleToAdd;
    private BattlerData battlerToAdd;
    private int battlerLevel;
    private bool showBattlers = true;

    public override bool RequiresConstantRepaint() => Application.isPlaying;

    public override void OnInspectorGUI()
    {
        var debugger = (PlayerProfileDebugger)target;
        EditorGUILayout.HelpBox(
            "Edits change the live PlayerProfile only during Play Mode. Nothing is applied on startup " +
            "or saved to disk automatically. Confirm numeric edits with Enter or by leaving the field.", MessageType.Info);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode, then select PlayerProfileDebugger in the DontDestroyOnLoad scene.", MessageType.Info);
            return;
        }
        if (!debugger.CanEdit)
        {
            EditorGUILayout.HelpBox("This is not the active, enabled debugger singleton.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Live resources", EditorStyles.boldLabel);
        DrawResource("Money", PlayerProfile.Money, debugger.SetMoney, ref moneyToAdd, debugger.AddMoney);
        DrawResource("Research points", PlayerProfile.ResearchPoint, debugger.SetResearchPoint, ref researchToAdd, debugger.AddResearchPoint);
        DrawResource("Battle points", PlayerProfile.BattlePoint, debugger.SetBattlePoint, ref battleToAdd, debugger.AddBattlePoint);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Date and time", EditorStyles.boldLabel);
        DrawInt("Current date", PlayerProfile.CurrentDate, debugger.SetCurrentDate);
        EditorGUI.BeginChangeCheck();
        Clock time = (Clock)EditorGUILayout.EnumPopup("Current time", PlayerProfile.CurrentClock);
        if (EditorGUI.EndChangeCheck())
            debugger.SetCurrentTime(time);
        EditorGUILayout.HelpBox("Date is the existing raw day counter (new game starts at 0). Setting date/time does not simulate turns or trigger calendar events.", MessageType.None);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Unlock / update battler", EditorStyles.boldLabel);
        battlerToAdd = (BattlerData)EditorGUILayout.ObjectField("BattlerData", battlerToAdd, typeof(BattlerData), false);
        battlerLevel = EditorGUILayout.IntField("Level to assign", battlerLevel);
        using (new EditorGUI.DisabledScope(battlerToAdd == null))
        {
            if (GUILayout.Button("Add / Update Battler"))
                debugger.AddOrUpdateBattler(battlerToAdd, battlerLevel);
        }
        EditorGUILayout.HelpBox("Stores only BattlerID and level; does not hire or spend points. Existing IDs have their level replaced. Select the level explicitly; no gameplay level limits are imposed.", MessageType.None);
        if (battlerToAdd != null && !AssetDatabase.GetAssetPath(battlerToAdd).Contains("/Resources/BattlerData/"))
            EditorGUILayout.HelpBox("The current hire panel loads assets from Resources/BattlerData. It may not display this asset from its current location.", MessageType.Warning);

        showBattlers = EditorGUILayout.Foldout(showBattlers, $"Available battlers ({PlayerProfile.AvailableBattlerData.Count})", true);
        if (showBattlers)
        {
            EditorGUI.indentLevel++;
            foreach (AvailableBattlerRecord record in PlayerProfile.AvailableBattlerData)
            {
                if (record == null)
                    EditorGUILayout.HelpBox("Null battler record in profile.", MessageType.Warning);
                else
                    DrawInt(string.IsNullOrEmpty(record.BattlerID) ? "(empty ID)" : record.BattlerID,
                        record.BattlerCurrentLevel, value => debugger.SetBattlerLevel(record.BattlerID, value));
            }
            EditorGUI.indentLevel--;
        }

        if (!string.IsNullOrEmpty(debugger.LastMessage))
            EditorGUILayout.HelpBox(debugger.LastMessage, debugger.LastCommandFailed ? MessageType.Error : MessageType.Info);
    }

    private static void DrawResource(string label, int current, Func<int, bool> set, ref int amount, Func<int, bool> add)
    {
        DrawInt(label, current, set);
        using (new EditorGUILayout.HorizontalScope())
        {
            amount = EditorGUILayout.IntField("Amount to add", amount);
            if (GUILayout.Button("Add", GUILayout.Width(55)))
                add(amount);
        }
    }

    private static void DrawInt(string label, int current, Func<int, bool> set)
    {
        EditorGUI.BeginChangeCheck();
        int value = EditorGUILayout.DelayedIntField(label, current);
        if (EditorGUI.EndChangeCheck())
            set(value);
    }
}
