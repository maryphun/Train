using UnityEngine;
using Assets.SimpleLocalization.Scripts;
using TMPro;

[System.Serializable]
struct SpecificSetting
{
    [SerializeField] public GameLanguage targetLanguage;
    [SerializeField] public int fontSize;
}

[RequireComponent(typeof(TMP_Text))]
public class LanguageSpecificSetting : MonoBehaviour
{
    [SerializeField] SpecificSetting[] specificSettings;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (SpecificSetting setting in specificSettings)
        {
            if (setting.targetLanguage.ToString() == LocalizationManager.Language)
            {
                TMP_Text textCmoponent = GetComponent<TMP_Text>();

                if (setting.fontSize != 0)
                {
                    textCmoponent.fontSize = setting.fontSize;
                }
            }
        }
    }
}
