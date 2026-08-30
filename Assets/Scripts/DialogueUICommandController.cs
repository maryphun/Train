using System.Collections;
using System.Globalization;
using UnityEngine;
using Yarn.Unity;

public class DialogueUICommandController : MonoBehaviour
{
    private static CanvasGroup dialogueCanvasGroup;

    [YarnCommand("dialogue")]
    public static IEnumerator DialogueCommand(params string[] args)
    {
        if (args == null || args.Length == 0)
        {
            Debug.LogWarning("Dialogue command needs an action: show, hide, or toggle.");
            yield break;
        }

        CanvasGroup canvasGroup = FindDialogueCanvasGroup();
        if (canvasGroup == null)
        {
            Debug.LogWarning("Dialogue command could not find the Yarn dialogue canvas.");
            yield break;
        }

        string action = args[0].Trim().ToLowerInvariant();
        float duration = ParseDuration(GetArg(args, 1, "instant"), 0f);

        switch (action)
        {
            case "show":
                yield return SetDialogueVisible(canvasGroup, true, duration);
                break;
            case "hide":
                yield return SetDialogueVisible(canvasGroup, false, duration);
                break;
            case "toggle":
                yield return SetDialogueVisible(canvasGroup, canvasGroup.alpha <= 0.5f, duration);
                break;
            default:
                Debug.LogWarning($"Unknown dialogue command action '{args[0]}'.");
                break;
        }
    }

    private static CanvasGroup FindDialogueCanvasGroup()
    {
        if (dialogueCanvasGroup != null)
        {
            return dialogueCanvasGroup;
        }

        Transform targetTransform = FindDialogueCanvasTransform();
        if (targetTransform == null)
        {
            return null;
        }

        dialogueCanvasGroup = targetTransform.GetComponent<CanvasGroup>();
        if (dialogueCanvasGroup == null)
        {
            dialogueCanvasGroup = targetTransform.gameObject.AddComponent<CanvasGroup>();
            dialogueCanvasGroup.alpha = 1f;
            dialogueCanvasGroup.interactable = true;
            dialogueCanvasGroup.blocksRaycasts = true;
        }

        return dialogueCanvasGroup;
    }

    private static Transform FindDialogueCanvasTransform()
    {
        DialogueRunner runner = Object.FindFirstObjectByType<DialogueRunner>();
        if (runner != null)
        {
            Transform directCanvas = runner.transform.Find("Canvas");
            if (directCanvas != null)
            {
                return directCanvas;
            }

            Canvas childCanvas = runner.GetComponentInChildren<Canvas>(true);
            if (childCanvas != null)
            {
                return childCanvas.transform;
            }
        }

        GameObject dialogueSystem = GameObject.Find("Dialogue System");
        if (dialogueSystem == null)
        {
            return null;
        }

        Transform canvas = dialogueSystem.transform.Find("Canvas");
        return canvas != null ? canvas : dialogueSystem.transform;
    }

    private static IEnumerator SetDialogueVisible(CanvasGroup canvasGroup, bool visible, float duration)
    {
        float targetAlpha = visible ? 1f : 0f;
        float startAlpha = canvasGroup.alpha;

        if (visible)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
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

        Debug.LogWarning($"Invalid dialogue UI duration '{value}'. Using {fallback}.");
        return fallback;
    }

    private static string GetArg(string[] args, int index, string fallback)
    {
        return args != null && index >= 0 && index < args.Length
            ? args[index]
            : fallback;
    }
}
