using UnityEngine;

[CreateAssetMenu(fileName = "BattlerData", menuName = "Scriptable Objects/BattlerData")]
public class BattlerData : ScriptableObject
{
    public string BattlerID;
    public string BattlerNameID;
    public Sprite BattlerSprite;
    [Min(0)] public int BattlerRequiredPoint;

#if UNITY_EDITOR
    [System.NonSerialized] private bool renameScheduled;

    private void OnValidate()
    {
        if (renameScheduled)
            return;

        renameScheduled = true;
        UnityEditor.EditorApplication.delayCall += RenameAssetIfNeeded;
    }

    private void RenameAssetIfNeeded()
    {
        renameScheduled = false;

        if (this == null || string.IsNullOrWhiteSpace(BattlerID) || string.IsNullOrWhiteSpace(BattlerNameID))
            return;

        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
        if (string.IsNullOrEmpty(assetPath))
            return;

        const string namePrefix = "Enemy.";
        string displayName = BattlerNameID.StartsWith(namePrefix, System.StringComparison.Ordinal)
            ? BattlerNameID.Substring(namePrefix.Length)
            : BattlerNameID;
        displayName = displayName.Trim();
        if (displayName.Length == 0)
            return;

        string desiredName = $"{BattlerID.Trim()}. {displayName}";
        if (System.IO.Path.GetFileNameWithoutExtension(assetPath) == desiredName)
            return;

        string error = UnityEditor.AssetDatabase.RenameAsset(assetPath, desiredName);
        if (!string.IsNullOrEmpty(error))
            Debug.LogWarning($"Could not rename BattlerData asset to '{desiredName}': {error}", this);
    }
#endif
}
