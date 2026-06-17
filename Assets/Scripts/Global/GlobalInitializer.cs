using UnityEngine;

public static class GlobalInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        Debug.Log("Global initialization");

        // Initialize managers, settings, services, etc.
        Assets.SimpleLocalization.LocalizationSetting.InitLocalization();
    }
}