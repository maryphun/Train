using System;
using UnityEngine;
using TMPro;
using Assets.SimpleLocalization.Scripts;

namespace Assets.SimpleLocalization
{
	/// <summary>
	/// Asset usage example.
	/// </summary>
	static class LocalizationSetting
	{
		/// <summary>
		/// Called on app start.
		/// </summary>
		static public void InitLocalization()
		{
			LocalizationManager.Read();

			switch (Application.systemLanguage)
			{
				case SystemLanguage.ChineseTraditional:
					Debug.Log("Game language into ChineseTraditional.");
					LocalizationManager.Language = "ChineseTraditional";
					break;
				case SystemLanguage.ChineseSimplified:
					Debug.Log("Game language into ChineseSimplified.");
					LocalizationManager.Language = "ChineseSimplified";
					break;
				case SystemLanguage.English:
					Debug.Log("Game language into English.");
					LocalizationManager.Language = "English";
					break;
				default:
					Debug.Log("Defaulted game language into Japanese.");
					LocalizationManager.Language = "Japanese";
					break;
			}

#if UNITY_EDITOR
			DebugLanguage debugLanguage = Resources.Load<DebugLanguage>("DebugLanguage");

        if (debugLanguage == null)
        {
            Debug.LogError( "DebugLanguage asset not found at " + "Assets/Resources/DebugLanguage.asset" );
            return;
        }

			LocalizationManager.Language = debugLanguage.Language.ToString();
#endif
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize()
		{
			TMP_Text.LocalizationResolver = ResolveText;
		}

		private static string ResolveText(string localizeId)
		{
			return LocalizationManager.Localize(localizeId);
		}
	}
}