using System.Collections;
using System.Globalization;
using UnityEngine;
using Yarn.Unity;

public class DialogueAudioCommandController : MonoBehaviour
{
    [YarnCommand("bgm")]
    public static IEnumerator BgmCommand(params string[] args)
    {
        if (args == null || args.Length == 0)
        {
            Debug.LogWarning("BGM command needs an action: play, crossfade, stop, pause, resume, or volume.");
            yield break;
        }

        AudioManager audioManager = AudioManager.Instance;
        string action = args[0].Trim().ToLowerInvariant();

        switch (action)
        {
            case "play":
                if (!TryReadArg(args, 1, "bgm play", "clip name", out string playClip))
                {
                    yield break;
                }

                float playFadeTime = ParseDuration(GetArg(args, 2, "instant"), 0f);
                if (playFadeTime <= 0f)
                {
                    audioManager.PlayMusic(playClip);
                }
                else
                {
                    audioManager.PlayMusicWithFade(playClip, playFadeTime);
                }
                break;

            case "crossfade":
            case "cross":
                if (!TryReadArg(args, 1, "bgm crossfade", "clip name", out string crossFadeClip))
                {
                    yield break;
                }

                audioManager.PlayMusicWithCrossFade(crossFadeClip, ParseDuration(GetArg(args, 2, "1"), 1f));
                break;

            case "stop":
                float stopFadeTime = ParseDuration(GetArg(args, 1, "1"), 1f);
                if (stopFadeTime <= 0f)
                {
                    audioManager.DestroyCurrentMusicSource();
                }
                else
                {
                    audioManager.StopMusicWithFade(stopFadeTime);
                }
                break;

            case "pause":
                audioManager.PauseMusic();
                break;

            case "resume":
            case "unpause":
                audioManager.UnpauseMusic();
                break;

            case "volume":
                if (!TryReadArg(args, 1, "bgm volume", "volume value", out string volumeValue))
                {
                    yield break;
                }

                audioManager.SetMusicVolume(ParseVolume(volumeValue));
                break;

            default:
                Debug.LogWarning($"Unknown BGM command action '{args[0]}'.");
                break;
        }
    }

    [YarnCommand("se")]
    public static void SoundEffectCommand(params string[] args)
    {
        if (args == null || args.Length == 0)
        {
            Debug.LogWarning("SE command needs an action: play or volume.");
            return;
        }

        AudioManager audioManager = AudioManager.Instance;
        string action = args[0].Trim().ToLowerInvariant();

        switch (action)
        {
            case "play":
                if (!TryReadArg(args, 1, "se play", "clip name", out string clipName))
                {
                    return;
                }

                string volumeArg = GetArg(args, 2, string.Empty);
                if (string.IsNullOrWhiteSpace(volumeArg))
                {
                    audioManager.PlaySFX(clipName);
                }
                else
                {
                    audioManager.PlaySFX(clipName, ParseVolume(volumeArg));
                }
                break;

            case "volume":
                if (!TryReadArg(args, 1, "se volume", "volume value", out string volumeValue))
                {
                    return;
                }

                audioManager.SetSEMasterVolume(ParseVolume(volumeValue));
                break;

            default:
                Debug.LogWarning($"Unknown SE command action '{args[0]}'.");
                break;
        }
    }

    private static float ParseDuration(string value, float fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        string normalized = value.Trim().ToLowerInvariant();
        if (normalized == "instant" || normalized == "none")
        {
            return 0f;
        }

        if (float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out float duration))
        {
            return Mathf.Max(0f, duration);
        }

        Debug.LogWarning($"Invalid audio duration '{value}'. Using {fallback}.");
        return fallback;
    }

    private static float ParseVolume(string value)
    {
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float volume))
        {
            return Mathf.Clamp01(volume);
        }

        Debug.LogWarning($"Invalid audio volume '{value}'. Using 1.");
        return 1f;
    }

    private static string GetArg(string[] args, int index, string fallback)
    {
        return args != null && index >= 0 && index < args.Length
            ? args[index]
            : fallback;
    }

    private static bool TryReadArg(string[] args, int index, string commandName, string label, out string value)
    {
        value = GetArg(args, index, string.Empty);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        Debug.LogWarning($"{commandName} command needs a {label}.");
        return false;
    }
}
