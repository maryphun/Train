using System.Collections;
using System.Globalization;
using UnityEngine;
using Yarn.Unity;

public class DialogueShakeController : MonoBehaviour
{
    [YarnCommand("shake")]
    public static IEnumerator ShakeCommand(params string[] args)
    {
        float duration = ParseDuration(GetArg(args, 0, "0.25"), 0.25f);
        float strength = ParseStrength(GetArg(args, 1, "8"));

        if (duration <= 0f || strength <= 0f)
        {
            yield break;
        }

        RectTransform uiTarget = FindShakeRectTarget();
        if (uiTarget != null)
        {
            yield return ShakeRectTransform(uiTarget, duration, strength);
            yield break;
        }

        Transform cameraTarget = FindShakeCameraTarget();
        if (cameraTarget == null)
        {
            Debug.LogWarning("Shake command could not find a UI stage or camera to shake.");
            yield break;
        }

        yield return ShakeTransform(cameraTarget, duration, strength * 0.01f);
    }

    private static RectTransform FindShakeRectTarget()
    {
        DialogueBackgroundController backgroundController = Object.FindFirstObjectByType<DialogueBackgroundController>();
        if (backgroundController != null && backgroundController.transform.parent is RectTransform backgroundParent)
        {
            return backgroundParent;
        }

        DialogueCharacterController characterController = Object.FindFirstObjectByType<DialogueCharacterController>();
        if (characterController != null && characterController.transform is RectTransform characterRect)
        {
            return characterRect;
        }

        GameObject canvasObject = GameObject.Find("Canvas");
        return canvasObject != null ? canvasObject.transform as RectTransform : null;
    }

    private static Transform FindShakeCameraTarget()
    {
        Camera camera = Camera.main != null
            ? Camera.main
            : Object.FindFirstObjectByType<Camera>();

        return camera != null ? camera.transform : null;
    }

    private static IEnumerator ShakeRectTransform(RectTransform target, float duration, float strength)
    {
        Vector2 originalPosition = target.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            target.anchoredPosition = originalPosition + Random.insideUnitCircle * strength;
            yield return null;
        }

        target.anchoredPosition = originalPosition;
    }

    private static IEnumerator ShakeTransform(Transform target, float duration, float strength)
    {
        Vector3 originalPosition = target.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector2 offset = Random.insideUnitCircle * strength;
            target.localPosition = originalPosition + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        target.localPosition = originalPosition;
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

        Debug.LogWarning($"Invalid shake duration '{value}'. Using {fallback}.");
        return fallback;
    }

    private static float ParseStrength(string value)
    {
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float strength))
        {
            return Mathf.Max(0f, strength);
        }

        Debug.LogWarning($"Invalid shake strength '{value}'. Using 8.");
        return 8f;
    }

    private static string GetArg(string[] args, int index, string fallback)
    {
        return args != null && index >= 0 && index < args.Length
            ? args[index]
            : fallback;
    }
}
