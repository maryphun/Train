using System.Threading.Tasks;
using UnityEngine;
using Yarn.Unity;

public class DialogueTestManager : MonoBehaviour
{
    [Header("Setting")]
    [SerializeField] string targetNodeName;

    [Header("References")]
    [SerializeField] Yarn.Unity.DialogueRunner dialogueRunner;
    [SerializeField] GameObject startDebugBtn;

    private void Start()
    {

    }

    public async void OnClickStartDebug()
    {
        startDebugBtn.SetActive(false);

        await PlayDialogueAsync(targetNodeName);

        startDebugBtn.SetActive(true);
    }

    public async Task PlayDialogueAsync(string nodeName)
    {
        if (dialogueRunner.IsDialogueRunning)
            return;

        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

        void OnDialogueComplete()
        {
            dialogueRunner.onDialogueComplete.RemoveListener(OnDialogueComplete);
            tcs.TrySetResult(true);
        }

        dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);

        dialogueRunner.StartDialogue(nodeName);

        await tcs.Task;
    }
}
