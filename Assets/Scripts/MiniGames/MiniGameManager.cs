using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RupeeRanger.Core;
using RupeeRanger.Data;
using RupeeRanger.Town;

namespace RupeeRanger.MiniGames
{
    public class MiniGameManager : MonoBehaviour
    {
        public static MiniGameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<MiniGameManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("MiniGameManager");
                        _instance = go.AddComponent<MiniGameManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }
        private static MiniGameManager _instance;

        public const string SCENE_WORD_SEARCH = "MiniGame_WordSearch";
        public const string SCENE_WORD_FINDER = "MiniGame_WordFinder";
        public const string SCENE_MATH_SPEED = "MiniGame_MathSpeed";
        public const string SCENE_MATH_PUZZLE = "MiniGame_MathPuzzle";
        public const string SCENE_MEMORY_MATCH = "MiniGame_MemoryMatch";
        public const string SCENE_QUIZ_MASTER = "MiniGame_QuizMaster";

        [Header("State")]
        public bool isMiniGameActive = false;
        public string currentLoadedScene = null;
        public MiniGame currentActiveGame = null;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public string GetSceneForBuilding(BuildingType bType)
        {
            switch (bType)
            {
                case BuildingType.WordSearchHouse:
                    return SCENE_WORD_SEARCH;
                case BuildingType.WordFinderHouse:
                    return SCENE_WORD_FINDER;
                case BuildingType.MathsSpeedHouse:
                    return SCENE_MATH_SPEED;
                case BuildingType.MathsPuzzleHouse:
                    return SCENE_MATH_PUZZLE;
                case BuildingType.GameCentre:
                    return SCENE_MEMORY_MATCH;
                case BuildingType.Library:
                    return SCENE_QUIZ_MASTER;
                default:
                    return null;
            }
        }

        public string GetSceneForTask(TaskData task, TownBuilding building)
        {
            if (building != null)
            {
                string scene = GetSceneForBuilding(building.buildingType);
                if (scene != null) return scene;
            }

            if (task != null)
            {
                string scene = GetSceneForBuilding(task.targetBuilding);
                if (scene != null) return scene;

                if (!string.IsNullOrEmpty(task.miniGameId))
                {
                    string id = task.miniGameId.ToLowerInvariant();
                    if (id.Contains("search")) return SCENE_WORD_SEARCH;
                    if (id.Contains("finder") || id.Contains("scramble")) return SCENE_WORD_FINDER;
                    if (id.Contains("speed")) return SCENE_MATH_SPEED;
                    if (id.Contains("puzzle")) return SCENE_MATH_PUZZLE;
                    if (id.Contains("memory") || id.Contains("card") || id.Contains("arcade") || id.Contains("match")) return SCENE_MEMORY_MATCH;
                    if (id.Contains("quiz") || id.Contains("trivia") || id.Contains("library")) return SCENE_QUIZ_MASTER;
                }
            }

            return null;
        }

        public void LaunchGameForTask(TaskData task, TownBuilding building)
        {
            string sceneName = GetSceneForTask(task, building);
            LaunchMiniGame(sceneName, (success) =>
            {
                if (success)
                {
                    if (DayManager.Instance != null)
                    {
                        // Completing any mini-game challenge successfully completes the day's task and enables Next Day!
                        DayManager.Instance.CompleteCurrentTask();

                        if (TownHUD.Instance != null)
                        {
                            TownHUD.Instance.EnableNextDayButton(true);
                        }
                    }
                    else if (GameManager.Instance != null)
                    {
                        GameManager.Instance.score += 50;
                    }
                }
                else
                {
                    Debug.Log("[MiniGameManager] MiniGame exited without completion.");
                }
            });
        }

        public void LaunchMiniGame(string sceneName, Action<bool> onFinished = null)
        {
            if (isMiniGameActive)
            {
                Debug.LogWarning("[MiniGameManager] A minigame is already active!");
                return;
            }

            StartCoroutine(LoadMiniGameRoutine(sceneName, onFinished));
        }

        private IEnumerator LoadMiniGameRoutine(string sceneName, Action<bool> onFinished)
        {
            isMiniGameActive = true;
            currentLoadedScene = sceneName;

            Debug.Log($"[MiniGameManager] Loading {sceneName} additively...");
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!op.isDone)
            {
                yield return null;
            }

            Scene loadedScene = SceneManager.GetSceneByName(sceneName);

            // Ensure only one EventSystem is active in the scene hierarchy
            var allEventSystems = UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>();
            if (allEventSystems.Length > 1)
            {
                for (int i = 0; i < allEventSystems.Length; i++)
                {
                    if (allEventSystems[i] != null && allEventSystems[i].gameObject.scene == loadedScene)
                    {
                        Destroy(allEventSystems[i].gameObject);
                        Debug.Log($"[MiniGameManager] Cleaned up duplicate EventSystem from {sceneName}");
                    }
                }
            }

            MiniGame miniGame = null;

            foreach (var root in loadedScene.GetRootGameObjects())
            {
                miniGame = root.GetComponentInChildren<MiniGame>(true);
                if (miniGame != null) break;
            }

            if (miniGame != null)
            {
                currentActiveGame = miniGame;

                // Determine difficulty band from player age
                int playerAge = 8;
                if (GameManager.Instance != null && GameManager.Instance.playerAge >= 5)
                {
                    playerAge = GameManager.Instance.playerAge;
                }
                DifficultyBand band = DifficultyHelper.FromAge(playerAge);

                Debug.Log($"[MiniGameManager] Initializing {miniGame.GetType().Name} with band: {band} (Age: {playerAge})");
                miniGame.Setup(band);
                miniGame.StartGame();

                miniGame.OnFinished += (success) =>
                {
                    Debug.Log($"[MiniGameManager] MiniGame finished with success={success}");
                    onFinished?.Invoke(success);
                    StartCoroutine(UnloadMiniGameRoutine(sceneName));
                };
            }
            else
            {
                Debug.LogError($"[MiniGameManager] Could not find MiniGame component in scene {sceneName}!");
                StartCoroutine(UnloadMiniGameRoutine(sceneName));
                onFinished?.Invoke(false);
            }
        }

        private IEnumerator UnloadMiniGameRoutine(string sceneName)
        {
            Debug.Log($"[MiniGameManager] Unloading {sceneName} additively...");
            currentActiveGame = null;

            AsyncOperation op = SceneManager.UnloadSceneAsync(sceneName);
            if (op != null)
            {
                while (!op.isDone)
                {
                    yield return null;
                }
            }

            currentLoadedScene = null;
            isMiniGameActive = false;
            Debug.Log($"[MiniGameManager] Returned to Town safely.");

            // Reinforce Town HUD Next Day status and financials after returning to town
            if (DayManager.Instance != null && (DayManager.Instance.isTaskCompleted || DayManager.Instance.isEventResolved))
            {
                if (TownHUD.Instance != null)
                {
                    TownHUD.Instance.UpdateFinancials();
                    TownHUD.Instance.UpdateTransactionFeed();
                    TownHUD.Instance.EnableNextDayButton(true);
                }
            }
        }
    }
}
