using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RupeeRanger.Core;

namespace RupeeRanger.MiniGames
{
    public class MathsGameMiniGame : MiniGame
    {
        private const string PREFS_HIGH_SCORE = "RupeeRanger_MathsHighScore";

        [Header("UI Header")]
        public TMP_Text scoreText;
        public TMP_Text highScoreText;
        public TMP_Text timerText;
        public TMP_Text progressText;

        [Header("Question & Input")]
        public TMP_Text questionText;
        public TMP_InputField answerInput;
        public Button submitButton;
        public TMP_Text feedbackText;
        public Image feedbackTick;

        [Header("On-Screen Keypad")]
        public Button[] numpadButtons;
        public Button numpadBackspaceButton;
        public Button numpadEnterButton;

        [Header("Modals")]
        public GameObject victoryPanel;
        public TMP_Text victoryMessage;
        public Button finishButton;
        public Button closeButton;

        [Header("Game Parameters")]
        public float timeRemaining = 90f;
        public int requiredCorrect = 5;
        public int currentScore = 0;
        public int highScore = 0;

        private int correctCount = 0;
        private int currentExpectedAnswer = 0;
        private Coroutine timerCoroutine;
        private bool isSetupDone = false;
        private float lastDigitTime = 0f;

        private void Start()
        {
            if (!isSetupDone && (MiniGameManager.Instance == null || !MiniGameManager.Instance.isMiniGameActive))
            {
                isSetupDone = true;
                int age = (GameManager.Instance != null) ? GameManager.Instance.playerAge : 10;
                Setup(DifficultyHelper.FromAge(age));
                StartGame();
            }
        }

        private void Update()
        {
            if (!isGameActive) return;

#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            {
                OnSubmitAnswer();
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    OnSubmitAnswer();
                }
            }
            catch {}
#endif
        }

        public override void Setup(DifficultyBand band)
        {
            isSetupDone = true;
            difficultyBand = band;
            gameTitle = "Maths Challenge";

            switch (band)
            {
                case DifficultyBand.Easy:
                    requiredCorrect = 5;
                    timeRemaining = 120f;
                    break;
                case DifficultyBand.Medium:
                    requiredCorrect = 7;
                    timeRemaining = 90f;
                    break;
                case DifficultyBand.Hard:
                    requiredCorrect = 10;
                    timeRemaining = 75f;
                    break;
            }

            highScore = PlayerPrefs.GetInt(PREFS_HIGH_SCORE, 0);

            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(OnSubmitAnswer);
            }
            if (finishButton != null)
            {
                finishButton.onClick.RemoveAllListeners();
                finishButton.onClick.AddListener(() => Complete(true));
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseGame);
            }

            SetupNumpad();

            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (feedbackTick != null) feedbackTick.gameObject.SetActive(false);
        }

        public override void StartGame()
        {
            isGameActive = true;
            isCompleted = false;
            correctCount = 0;
            currentScore = 0;

            UpdateScoreUI();
            UpdateProgressUI();

            GenerateNewProblem();

            if (timerCoroutine != null) StopCoroutine(timerCoroutine);
            timerCoroutine = StartCoroutine(TimerLoop());

            if (answerInput != null)
            {
                answerInput.ActivateInputField();
            }
        }

        private void SetupNumpad()
        {
            if (numpadButtons != null)
            {
                for (int i = 0; i < numpadButtons.Length; i++)
                {
                    if (numpadButtons[i] != null)
                    {
                        string digit = i.ToString();
                        numpadButtons[i].onClick.RemoveAllListeners();
                        numpadButtons[i].onClick.AddListener(() => OnNumpadDigit(digit));
                    }
                }
            }

            if (numpadBackspaceButton != null)
            {
                numpadBackspaceButton.onClick.RemoveAllListeners();
                numpadBackspaceButton.onClick.AddListener(OnNumpadBackspace);
            }

            if (numpadEnterButton != null)
            {
                numpadEnterButton.onClick.RemoveAllListeners();
                numpadEnterButton.onClick.AddListener(OnSubmitAnswer);
            }
        }

        private void OnNumpadDigit(string digit)
        {
            if (Time.unscaledTime - lastDigitTime < 0.08f) return;
            lastDigitTime = Time.unscaledTime;

            if (answerInput != null && answerInput.text.Length < 4)
            {
                answerInput.text += digit;
            }
        }

        private void OnNumpadBackspace()
        {
            if (answerInput != null && answerInput.text.Length > 0)
            {
                answerInput.text = answerInput.text.Substring(0, answerInput.text.Length - 1);
            }
        }

        private static System.Collections.Generic.Queue<string> recentProblems = new System.Collections.Generic.Queue<string>();

        private void GenerateNewProblem()
        {
            int numA = 0;
            int numB = 0;
            char op = '+';
            string problemKey = "";
            int attempts = 0;

            do
            {
                attempts++;
                switch (difficultyBand)
                {
                    case DifficultyBand.Easy:
                        // Add / subtract numbers up to 25
                        bool isAdd = UnityEngine.Random.value > 0.45f;
                        if (isAdd)
                        {
                            numA = UnityEngine.Random.Range(2, 18);
                            numB = UnityEngine.Random.Range(2, 18);
                            op = '+';
                            currentExpectedAnswer = numA + numB;
                        }
                        else
                        {
                            numA = UnityEngine.Random.Range(5, 25);
                            numB = UnityEngine.Random.Range(1, numA);
                            op = '-';
                            currentExpectedAnswer = numA - numB;
                        }
                        break;

                    case DifficultyBand.Medium:
                        // Addition up to 75, Subtraction up to 75, Multiplication 2 to 12
                        int modeMed = UnityEngine.Random.Range(0, 3);
                        if (modeMed == 0) // Addition
                        {
                            numA = UnityEngine.Random.Range(12, 55);
                            numB = UnityEngine.Random.Range(8, 45);
                            op = '+';
                            currentExpectedAnswer = numA + numB;
                        }
                        else if (modeMed == 1) // Subtraction
                        {
                            numA = UnityEngine.Random.Range(25, 85);
                            numB = UnityEngine.Random.Range(6, numA - 3);
                            op = '-';
                            currentExpectedAnswer = numA - numB;
                        }
                        else // Multiplication
                        {
                            numA = UnityEngine.Random.Range(2, 10);
                            numB = UnityEngine.Random.Range(3, 11);
                            op = '×';
                            currentExpectedAnswer = numA * numB;
                        }
                        break;

                    case DifficultyBand.Hard:
                        // Mult up to 15, Division, or 2-digit arithmetic
                        int modeHard = UnityEngine.Random.Range(0, 3);
                        if (modeHard == 0) // Mult
                        {
                            numA = UnityEngine.Random.Range(4, 15);
                            numB = UnityEngine.Random.Range(4, 14);
                            op = '×';
                            currentExpectedAnswer = numA * numB;
                        }
                        else if (modeHard == 1) // Division
                        {
                            numB = UnityEngine.Random.Range(3, 13);
                            int quot = UnityEngine.Random.Range(3, 15);
                            numA = numB * quot;
                            op = '÷';
                            currentExpectedAnswer = quot;
                        }
                        else // 2-digit mixed
                        {
                            numA = UnityEngine.Random.Range(35, 120);
                            numB = UnityEngine.Random.Range(15, 65);
                            op = (UnityEngine.Random.value > 0.5f) ? '+' : '-';
                            currentExpectedAnswer = (op == '+') ? (numA + numB) : (numA - numB);
                        }
                        break;
                }

                problemKey = $"{numA}{op}{numB}";
            } while (recentProblems.Contains(problemKey) && attempts < 15);

            recentProblems.Enqueue(problemKey);
            if (recentProblems.Count > 20) recentProblems.Dequeue();

            if (questionText != null)
            {
                questionText.text = $"{numA} {op} {numB} = ";
            }

            if (answerInput != null)
            {
                answerInput.text = "";
                answerInput.ActivateInputField();
            }
        }

        public void OnSubmitAnswer()
        {
            if (!isGameActive || answerInput == null) return;

            string raw = answerInput.text.Trim();
            if (string.IsNullOrEmpty(raw)) return;

            if (int.TryParse(raw, out int userAnswer))
            {
                if (userAnswer == currentExpectedAnswer)
                {
                    // Correct!
                    correctCount++;
                    currentScore += 10;
                    if (currentScore > highScore)
                    {
                        highScore = currentScore;
                        PlayerPrefs.SetInt(PREFS_HIGH_SCORE, highScore);
                        PlayerPrefs.Save();
                    }

                    UpdateScoreUI();
                    UpdateProgressUI();
                    ShowFeedback("Correct! +10 Points", true);

                    if (correctCount >= requiredCorrect)
                    {
                        TriggerVictory();
                    }
                    else
                    {
                        GenerateNewProblem();
                    }
                }
                else
                {
                    // Incorrect
                    ShowFeedback($"Oops! {questionText.text} {currentExpectedAnswer}", false);
                    StartCoroutine(ShakeInput());
                    GenerateNewProblem();
                }
            }
        }

        private IEnumerator TimerLoop()
        {
            while (timeRemaining > 0f && isGameActive)
            {
                timeRemaining -= Time.deltaTime;
                int mins = Mathf.FloorToInt(timeRemaining / 60f);
                int secs = Mathf.FloorToInt(timeRemaining % 60f);
                if (timerText != null)
                {
                    timerText.text = $"{mins:00}:{secs:00}";
                }
                yield return null;
            }

            if (isGameActive && !isCompleted)
            {
                // Time up! If at least 1 correct, award win, else retry
                if (correctCount >= Mathf.Max(1, requiredCorrect / 2))
                {
                    TriggerVictory();
                }
                else
                {
                    ShowFeedback("Time's up! Let's try again!", false);
                    timeRemaining = 60f;
                    GenerateNewProblem();
                    StartCoroutine(TimerLoop());
                }
            }
        }

        private void UpdateScoreUI()
        {
            if (scoreText != null) scoreText.text = currentScore.ToString();
            if (highScoreText != null) highScoreText.text = highScore.ToString();
        }

        private void UpdateProgressUI()
        {
            if (progressText != null)
            {
                progressText.text = $"Progress: {correctCount} / {requiredCorrect} Solved";
            }
        }

        private IEnumerator ShakeInput()
        {
            if (answerInput == null) yield break;
            Vector3 orig = answerInput.transform.localPosition;
            for (int i = 0; i < 6; i++)
            {
                float offset = (i % 2 == 0) ? 8f : -8f;
                answerInput.transform.localPosition = new Vector3(orig.x + offset, orig.y, orig.z);
                yield return new WaitForSeconds(0.04f);
            }
            answerInput.transform.localPosition = orig;
        }

        private void ShowFeedback(string msg, bool isSuccess)
        {
            if (feedbackText != null)
            {
                feedbackText.text = msg;
                feedbackText.color = isSuccess ? new Color32(0x00, 0xE6, 0x76, 0xFF) : new Color32(0xFF, 0x52, 0x52, 0xFF);
            }

            if (feedbackTick != null)
            {
                feedbackTick.gameObject.SetActive(isSuccess);
                if (isSuccess)
                {
                    StopCoroutine(nameof(HideTick));
                    StartCoroutine(nameof(HideTick));
                }
            }
        }

        private IEnumerator HideTick()
        {
            yield return new WaitForSeconds(1.2f);
            if (feedbackTick != null) feedbackTick.gameObject.SetActive(false);
        }

        private void TriggerVictory()
        {
            isGameActive = false;
            if (timerCoroutine != null) StopCoroutine(timerCoroutine);

            if (victoryPanel != null)
            {
                if (victoryMessage != null)
                {
                    victoryMessage.text = $"Brilliant Mathematical Mind!\nSolved {correctCount} problems! Final Score: {currentScore}\n+500 Reward & Task Completed!";
                }
                victoryPanel.SetActive(true);
            }

            if (finishButton != null)
            {
                finishButton.gameObject.SetActive(true);
                finishButton.interactable = true;
                finishButton.onClick.RemoveAllListeners();
                finishButton.onClick.AddListener(() => Complete(true));
            }
        }
    }
}
