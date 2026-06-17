using UnityEngine;

public enum GameLanguage
{
    English,
    Japanese,
    ChineseTraditional,
    ChineseSimplified
}

[CreateAssetMenu(fileName = "DebugLanguage", menuName = "Scriptable Objects/DebugLanguage")]
public class DebugLanguage : ScriptableObject
{
    [SerializeField]
    private GameLanguage language = GameLanguage.English;

    public GameLanguage Language => language;
}
