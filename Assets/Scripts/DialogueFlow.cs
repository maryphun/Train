using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;

/// <summary>Loads the Dialogue scene, runs a Yarn node, then loads the caller's return scene.</summary>
public sealed class DialogueFlow : MonoBehaviour
{
    public const string ScenePath = "Assets/Scenes/Dialogue.unity";

    private static DialogueFlow instance;
    private bool busy;
    private Task activePlayback;
    private PreparedDialogue preparedDialogue;

    private sealed class PreparedDialogue
    {
        public string NodeName;
        public string ReturnScene;
        public float ReturnFadeTime;
        public YarnProject Project;
    }

    /// <summary>True while setup is waiting for entry, loading, playing or returning.</summary>
    public static bool IsRunning => instance != null && instance.busy;

    /// <summary>
    /// Call on Unity's main thread in Play Mode. Returns whether the request was accepted.
    /// If project is omitted, uses the Dialogue scene runner's assigned Yarn Project.
    /// The scene runner must have Auto Start disabled and its presenters configured.
    /// </summary>
    public static bool Start(string nodeName, string returnScene,
        float fadeTime = SceneTransitionManager.DefaultFadeTime, YarnProject project = null)
    {
        if (!TryReserveRequest(nodeName, returnScene, fadeTime, project))
            return false;

        if (!SceneTransitionManager.Load(ScenePath, fadeTime))
        {
            instance.ReleaseRequest();
            return false;
        }

        instance.StartCoroutine(instance.Run(nodeName, returnScene, fadeTime, project));
        return true;
    }

    /// <summary>
    /// Prepares the next entry to Dialogue without loading it. Call before your own transition.
    /// Playback begins after scene startup and any SceneTransitionManager fade have finished.
    /// returnFadeTime applies only to the automatic return, not the caller's entry transition.
    /// </summary>
    public static bool Setup(string nodeName, string returnScene,
        float returnFadeTime = SceneTransitionManager.DefaultFadeTime, YarnProject project = null)
    {
        if (!TryReserveRequest(nodeName, returnScene, returnFadeTime, project))
            return false;

        instance.preparedDialogue = new PreparedDialogue
        {
            NodeName = nodeName,
            ReturnScene = returnScene,
            ReturnFadeTime = returnFadeTime,
            Project = project
        };
        SceneManager.sceneLoaded += instance.OnSceneLoaded;
        return true;
    }

    /// <summary>Cancels a Setup waiting for scene entry; does not stop active playback or loading.</summary>
    public static bool CancelSetup()
    {
        if (instance == null || instance.preparedDialogue == null)
            return false;
        instance.ReleaseRequest();
        return true;
    }

    private static bool TryReserveRequest(string nodeName, string returnScene, float fadeTime, YarnProject project)
    {
        if (!Application.isPlaying || IsRunning || SceneTransitionManager.IsTransitioning)
            return false;

        if (string.IsNullOrWhiteSpace(nodeName))
        {
            Debug.LogWarning("DialogueFlow requires a Node name from the imported dialogue content.");
            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(ScenePath)
            || string.IsNullOrWhiteSpace(returnScene)
            || !Application.CanStreamedLevelBeLoaded(returnScene))
        {
            Debug.LogWarning("DialogueFlow requires Dialogue and the return scene in the enabled Build Profiles scene list.");
            return false;
        }

        if (float.IsNaN(fadeTime) || float.IsInfinity(fadeTime) || fadeTime < 0f)
        {
            Debug.LogWarning("DialogueFlow fade time must be finite and greater than or equal to zero.");
            return false;
        }

        // An explicitly supplied project can be checked before leaving the caller's scene.
        if (project != null && !ValidateNode(project, nodeName))
            return false;

        if (instance == null)
        {
            var owner = new GameObject(nameof(DialogueFlow));
            instance = owner.AddComponent<DialogueFlow>();
            DontDestroyOnLoad(owner);
        }

        if (!instance.isActiveAndEnabled)
            return false;

        instance.busy = true;
        return true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.path != ScenePath || preparedDialogue == null)
            return;

        PreparedDialogue prepared = preparedDialogue;
        preparedDialogue = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StartCoroutine(Run(prepared.NodeName, prepared.ReturnScene, prepared.ReturnFadeTime,
            prepared.Project, waitForStart: true));
    }

    private IEnumerator Run(string nodeName, string returnScene, float fadeTime, YarnProject project,
        bool waitForStart = false)
    {
        try
        {
            // sceneLoaded is raised before Start, including when the caller loads without our fade manager.
            if (waitForStart)
                yield return null;

            // Wait until the scene has activated, Start methods have run, and the fade is clear.
            while (SceneTransitionManager.IsTransitioning)
                yield return null;

            if (!IsDialogueSceneActive())
            {
                Debug.LogWarning("DialogueFlow could not enter the Dialogue scene. Playback was not started.");
                yield break;
            }

            DialogueRunner runner = FindSceneRunner();
            if (runner == null)
            {
                Debug.LogError("Add an active DialogueRunner with line/option presenters to the Dialogue scene.");
                yield break;
            }

            if (runner.IsDialogueRunning)
            {
                Debug.LogError("DialogueFlow found a running dialogue. Disable Auto Start on the Dialogue scene runner.");
                yield break;
            }

            YarnProject selectedProject = project != null ? project : runner.YarnProject;
            if (!ValidateNode(selectedProject, nodeName))
                yield break;

            Task playback = activePlayback = Play(runner, selectedProject, nodeName);
            while (!playback.IsCompleted)
            {
                if (runner == null || !IsDialogueSceneActive())
                {
                    Debug.LogWarning("DialogueFlow was interrupted by leaving the Dialogue scene.");
                    yield break;
                }
                yield return null;
            }

            if (playback.IsFaulted)
            {
                Debug.LogException(playback.Exception.GetBaseException());
                yield break;
            }
            if (playback.IsCanceled)
                yield break;

            // Do not redirect an unrelated scene if another system took over during playback.
            while (SceneTransitionManager.IsTransitioning)
                yield return null;
            if (!IsDialogueSceneActive())
                yield break;

            if (!SceneTransitionManager.Load(returnScene, fadeTime))
            {
                Debug.LogWarning($"DialogueFlow could not return to '{returnScene}'.");
                yield break;
            }
            while (SceneTransitionManager.IsTransitioning)
                yield return null;
        }
        finally
        {
            ReleaseRequest();
        }
    }

    private static async Task Play(DialogueRunner runner, YarnProject project, string nodeName)
    {
        if (runner.YarnProject != project)
            runner.SetProject(project);
        await runner.StartDialogue(nodeName);
        // StartDialogue only starts playback; DialogueTask waits for lines, choices and presenter cleanup.
        await runner.DialogueTask;
    }

    private static bool ValidateNode(YarnProject project, string nodeName)
    {
        try
        {
            if (project == null || project.Program == null)
            {
                Debug.LogError("DialogueFlow requires a compiled Yarn Project. Assign Project.yarnproject to the runner or pass it to Start.");
                return false;
            }
            if (Array.IndexOf(project.NodeNames, nodeName) < 0)
            {
                Debug.LogError($"DialogueFlow cannot find Node '{nodeName}' in '{project.name}'. Use the creator's exact Node name.");
                return false;
            }
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            return false;
        }
    }

    private static bool IsDialogueSceneActive() => SceneManager.GetActiveScene().path == ScenePath;

    private static DialogueRunner FindSceneRunner()
    {
        DialogueRunner result = null;
        foreach (DialogueRunner candidate in FindObjectsByType<DialogueRunner>(FindObjectsSortMode.None))
        {
            if (!candidate.isActiveAndEnabled || candidate.gameObject.scene.path != ScenePath)
                continue;
            if (result != null)
            {
                Debug.LogError("DialogueFlow requires exactly one active DialogueRunner in the Dialogue scene.");
                return null;
            }
            result = candidate;
        }
        return result;
    }

    private static async void ObserveFailure(Task task)
    {
        try { await task; }
        catch (OperationCanceledException) { }
        catch (Exception exception) { Debug.LogException(exception); }
    }

    private void ReleaseRequest()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        preparedDialogue = null;
        if (activePlayback != null && !activePlayback.IsCompleted)
            ObserveFailure(activePlayback);
        activePlayback = null;
        busy = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        if (instance != null)
        {
            instance.StopAllCoroutines();
            instance.ReleaseRequest();
        }
        instance = null;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        ReleaseRequest();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
