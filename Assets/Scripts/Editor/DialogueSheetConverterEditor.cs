#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DialogueSheetConverterAsset))]
public class DialogueSheetConverterAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(12);

        DialogueSheetConverterAsset converter = (DialogueSheetConverterAsset)target;

        GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);

        if (GUILayout.Button("Download & Generate Yarn", GUILayout.Height(36)))
        {
            converter.DownloadAndGenerateYarn();
        }

        GUI.backgroundColor = Color.white;
    }
}
#endif