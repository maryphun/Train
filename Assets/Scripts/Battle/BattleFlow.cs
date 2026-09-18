using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Train.Battle
{
    // The only entry/exit point another game system needs to call.
    public static class BattleFlow
    {
        public const string SceneName = "Battle";

        private static BattleSetup pendingSetup;
        private static Action<BattleResult> completionCallback;
        private static string returnSceneName;

        public static BattleResult LastResult { get; private set; }

        public static void Enter(BattleSetup setup, Action<BattleResult> onCompleted = null)
        {
            if (setup == null) throw new ArgumentNullException(nameof(setup));
            if (pendingSetup != null)
                throw new InvalidOperationException("A battle is already being prepared.");
            if (!Application.CanStreamedLevelBeLoaded(SceneName))
                throw new InvalidOperationException("Add Battle.unity to the build scene list before entering combat.");

            setup.Validate();
            string destination = string.IsNullOrWhiteSpace(setup.returnSceneName)
                ? SceneManager.GetActiveScene().name
                : setup.returnSceneName;
            if (destination == SceneName || !Application.CanStreamedLevelBeLoaded(destination))
                throw new ArgumentException("BattleSetup needs a loadable return scene other than Battle.");

            pendingSetup = setup.Copy();
            returnSceneName = destination;
            completionCallback = onCompleted;
            LastResult = null;
            SceneManager.LoadScene(SceneName);
        }

        internal static bool TryTakeSetup(out BattleSetup setup)
        {
            setup = pendingSetup;
            pendingSetup = null;
            return setup != null;
        }

        internal static void Complete(BattleResult result, string fallbackReturnScene)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            LastResult = result;
            string destination = !string.IsNullOrWhiteSpace(returnSceneName)
                ? returnSceneName
                : fallbackReturnScene;
            Action<BattleResult> callback = completionCallback;
            returnSceneName = null;
            completionCallback = null;

            if (string.IsNullOrWhiteSpace(destination) ||
                destination == SceneName || !Application.CanStreamedLevelBeLoaded(destination))
            {
                Debug.LogWarning("Battle ended, but no valid return scene was configured. Result is available in BattleFlow.LastResult.");
                callback?.Invoke(result);
                return;
            }

            if (callback != null)
            {
                void OnSceneLoaded(Scene scene, LoadSceneMode mode)
                {
                    if (scene.name != destination) return;
                    SceneManager.sceneLoaded -= OnSceneLoaded;
                    callback(result);
                }

                SceneManager.sceneLoaded += OnSceneLoaded;
            }

            SceneManager.LoadScene(destination);
        }
    }
}
