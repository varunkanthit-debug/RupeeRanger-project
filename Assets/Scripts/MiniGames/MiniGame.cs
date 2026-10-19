using System;
using UnityEngine;

namespace RupeeRanger.MiniGames
{
    public abstract class MiniGame : MonoBehaviour
    {
        public event Action<bool> OnFinished;

        [Header("MiniGame Base Settings")]
        public string miniGameId = "minigame";
        public string gameTitle = "Mini-Game";
        public DifficultyBand difficultyBand = DifficultyBand.Medium;

        [Header("State")]
        public bool isGameActive = false;
        public bool isCompleted = false;

        private bool hasFinishedInvoked = false;

        protected virtual void Awake()
        {
            // If running standalone (not additively into Town), create an EventSystem so clicks work
            if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            }
        }

        public abstract void Setup(DifficultyBand band);
        public abstract void StartGame();

        public virtual void Complete(bool success)
        {
            if (hasFinishedInvoked) return;
            hasFinishedInvoked = true;
            isCompleted = true;
            isGameActive = false;

            Debug.Log($"[MiniGame] {gameTitle} completed with success={success}");
            OnFinished?.Invoke(success);
        }

        public virtual void CloseGame()
        {
            Complete(false);
        }
    }
}
