using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RupeeRanger.Core;

namespace RupeeRanger.MiniGames
{
    public class MathsPuzzleMiniGame : MiniGame
    {
        [System.Serializable]
        public class SumCell
        {
            public int num1;
            public int num2;
            public string op;
            public int expectedAnswer;
            public bool isSolved;

            // UI references
            public GameObject cellRoot;
            public Image cellCardBg;
            public TMP_Text topNumText;
            public TMP_Text opNumText;
            public TMP_Text answerDisplay;
            public Button goButton;
            public Image goButtonBg;
            public TMP_Text goButtonText;
            public GameObject checkmarkObj;
        }

        [Header("UI Grid & Cells")]
        public RectTransform gridContainer;
        public List<SumCell> cells = new List<SumCell>();
        public int selectedCellIndex = 0;

        [Header("Target & Goal")]
        public int requiredSolves = 3;
        public int solvedCount = 0;
        public TMP_Text instructionBannerText;
        public TMP_Text progressText;

        [Header("Feedback")]
        public TMP_Text feedbackText;
        public Image feedbackTick;
        private Coroutine feedbackCoroutine;

        [Header("Modals")]
        public GameObject victoryPanel;
        public TMP_Text victoryMessage;
        public Button finishButton;
        public Button closeButton;

        [Header("Keypad")]
        public Button[] keypadButtons;
        public Button keypadBackspaceButton;

        [Header("Colors")]
        public Color normalCardColor = new Color(0.96f, 0.96f, 0.94f, 1f);
        public Color selectedCardColor = new Color(1f, 0.98f, 0.85f, 1f);
        public Color solvedCardColor = new Color(0.85f, 0.96f, 0.85f, 1f);
        public Color cardBorderNormal = new Color(0.8f, 0.8f, 0.8f, 1f);
        public Color cardBorderSelected = new Color(0.2f, 0.6f, 1f, 1f);

        private Dictionary<int, string> cellInputBuffers = new Dictionary<int, string>();

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

        public override void Setup(DifficultyBand band)
        {
            isSetupDone = true;
            difficultyBand = band;
            gameTitle = "Maths Grid Puzzle";

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
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (feedbackTick != null) feedbackTick.gameObject.SetActive(false);

            SetupKeypad();

            switch (band)
            {
                case DifficultyBand.Easy:
                    requiredSolves = 3;
                    break;
                case DifficultyBand.Medium:
                    requiredSolves = 6;
                    break;
                case DifficultyBand.Hard:
                    requiredSolves = 9;
                    break;
            }

            GenerateGridSums();
            UpdateInstructionBanner();
            UpdateProgressUI();
            SelectCell(0);
        }

        private void SetupKeypad()
        {
            if (keypadButtons != null)
            {
                for (int i = 0; i < keypadButtons.Length; i++)
                {
                    if (keypadButtons[i] != null)
                    {
                        var tmp = keypadButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                        string digit = tmp != null ? tmp.text.Trim() : (i % 10).ToString();
                        keypadButtons[i].onClick.RemoveAllListeners();
                        keypadButtons[i].onClick.AddListener(() => OnKeypadDigitPressed(digit));
                    }
                }
            }
            if (keypadBackspaceButton != null)
            {
                keypadBackspaceButton.onClick.RemoveAllListeners();
                keypadBackspaceButton.onClick.AddListener(OnKeypadBackspacePressed);
            }
        }

        public override void StartGame()
        {
            isGameActive = true;
            isCompleted = false;
            solvedCount = 0;

            if (cells.Count > 0)
            {
                SelectCell(0);
            }
        }

        private void Update()
        {
            if (!isGameActive || isCompleted) return;

            HandleKeyboardInput();
        }

        private void HandleKeyboardInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                // Number keys 0-9
                for (int i = 0; i <= 9; i++)
                {
                    var digitKey = (UnityEngine.InputSystem.Key)((int)UnityEngine.InputSystem.Key.Digit0 + i);
                    var numKey = (UnityEngine.InputSystem.Key)((int)UnityEngine.InputSystem.Key.Numpad0 + i);
                    if (kb[digitKey].wasPressedThisFrame || kb[numKey].wasPressedThisFrame)
                    {
                        OnKeypadDigitPressed(i.ToString());
                        return;
                    }
                }

                if (kb.backspaceKey.wasPressedThisFrame || kb.deleteKey.wasPressedThisFrame)
                {
                    OnKeypadBackspacePressed();
                }
                else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                {
                    if (selectedCellIndex >= 0 && selectedCellIndex < cells.Count)
                    {
                        CheckCellAnswer(selectedCellIndex);
                    }
                }
                else if (kb.tabKey.wasPressedThisFrame)
                {
                    SelectNextUnsolvedCell();
                }
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                // Number keys 0-9
                for (int i = 0; i <= 9; i++)
                {
                    KeyCode numAlpha = KeyCode.Alpha0 + i;
                    KeyCode numKeypad = KeyCode.Keypad0 + i;
                    if (Input.GetKeyDown(numAlpha) || Input.GetKeyDown(numKeypad))
                    {
                        OnKeypadDigitPressed(i.ToString());
                        return;
                    }
                }

                if (Input.GetKeyDown(KeyCode.Backspace))
                {
                    OnKeypadBackspacePressed();
                }
                else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    if (selectedCellIndex >= 0 && selectedCellIndex < cells.Count)
                    {
                        CheckCellAnswer(selectedCellIndex);
                    }
                }
                else if (Input.GetKeyDown(KeyCode.Tab))
                {
                    SelectNextUnsolvedCell();
                }
            }
            catch {}
#endif
        }

        public void GenerateGridSums()
        {
            cells.Clear();
            cellInputBuffers.Clear();
            System.Random rnd = new System.Random(Guid.NewGuid().GetHashCode());
            HashSet<string> usedInThisGrid = new HashSet<string>();

            for (int i = 0; i < 9; i++)
            {
                SumCell cell = new SumCell();
                int a = 0, b = 0;
                string op = "+";
                int ans = 0;
                string eqSignature = "";
                int attempts = 0;

                do
                {
                    attempts++;
                    if (difficultyBand == DifficultyBand.Easy)
                    {
                        // Numbers from 2 to 20
                        bool isSub = rnd.Next(0, 2) == 0;
                        if (isSub)
                        {
                            op = "-";
                            a = rnd.Next(4, 20);
                            b = rnd.Next(1, a);
                            ans = a - b;
                        }
                        else
                        {
                            op = "+";
                            a = rnd.Next(2, 12);
                            b = rnd.Next(1, 10);
                            ans = a + b;
                        }
                    }
                    else if (difficultyBand == DifficultyBand.Medium)
                    {
                        int type = rnd.Next(0, 3);
                        if (type == 0) // Addition 10-60
                        {
                            op = "+";
                            a = rnd.Next(8, 45);
                            b = rnd.Next(5, 35);
                            ans = a + b;
                        }
                        else if (type == 1) // Subtraction
                        {
                            op = "-";
                            a = rnd.Next(20, 75);
                            b = rnd.Next(4, a - 3);
                            ans = a - b;
                        }
                        else // Multiplication
                        {
                            op = "x";
                            a = rnd.Next(2, 10);
                            b = rnd.Next(2, 10);
                            ans = a * b;
                        }
                    }
                    else // Hard
                    {
                        int type = rnd.Next(0, 4);
                        if (type == 0) // Multiplication up to 13
                        {
                            op = "x";
                            a = rnd.Next(3, 14);
                            b = rnd.Next(3, 13);
                            ans = a * b;
                        }
                        else if (type == 1) // 2-digit Add
                        {
                            op = "+";
                            a = rnd.Next(20, 95);
                            b = rnd.Next(15, 80);
                            ans = a + b;
                        }
                        else if (type == 2) // 2-digit Sub
                        {
                            op = "-";
                            a = rnd.Next(35, 120);
                            b = rnd.Next(12, a - 5);
                            ans = a - b;
                        }
                        else // Division
                        {
                            op = "÷";
                            b = rnd.Next(2, 12);
                            int quot = rnd.Next(2, 12);
                            a = b * quot;
                            ans = quot;
                        }
                    }

                    eqSignature = $"{a}{op}{b}";
                } while (usedInThisGrid.Contains(eqSignature) && attempts < 25);

                usedInThisGrid.Add(eqSignature);

                cell.num1 = a;
                cell.num2 = b;
                cell.op = op;
                cell.expectedAnswer = ans;
                cell.isSolved = false;

                cells.Add(cell);
                cellInputBuffers[i] = "";
            }

            SyncUIWithCells();
        }

        public void SyncUIWithCells()
        {
            if (gridContainer == null) return;

            for (int i = 0; i < cells.Count && i < gridContainer.childCount; i++)
            {
                int index = i;
                Transform cellTrans = gridContainer.GetChild(i);
                SumCell cell = cells[i];
                cell.cellRoot = cellTrans.gameObject;

                // Bind Card BG
                cell.cellCardBg = cellTrans.GetComponent<Image>();

                // Find text elements
                var texts = cellTrans.GetComponentsInChildren<TMP_Text>(true);
                foreach (var t in texts)
                {
                    if (t.name.Contains("TopNum")) cell.topNumText = t;
                    else if (t.name.Contains("OpNum")) cell.opNumText = t;
                    else if (t.name.Contains("Answer")) cell.answerDisplay = t;
                    else if (t.name.Contains("GoText")) cell.goButtonText = t;
                }

                if (cell.topNumText != null) cell.topNumText.text = cell.num1.ToString();
                if (cell.opNumText != null) cell.opNumText.text = $"{cell.op} {cell.num2}";
                if (cell.answerDisplay != null) cell.answerDisplay.text = cellInputBuffers[index];

                // Checkmark
                Transform check = cellTrans.Find("Checkmark");
                if (check != null)
                {
                    cell.checkmarkObj = check.gameObject;
                    cell.checkmarkObj.SetActive(false);
                }

                // Go Button
                Transform goBtnTrans = cellTrans.Find("GoButton");
                if (goBtnTrans != null)
                {
                    cell.goButton = goBtnTrans.GetComponent<Button>();
                    cell.goButtonBg = goBtnTrans.GetComponent<Image>();
                    if (cell.goButton != null)
                    {
                        cell.goButton.onClick.RemoveAllListeners();
                        cell.goButton.onClick.AddListener(() =>
                        {
                            SelectCell(index);
                            CheckCellAnswer(index);
                        });
                    }
                }

                // Cell card click listener
                Button cardBtn = cellTrans.GetComponent<Button>();
                if (cardBtn == null) cardBtn = cellTrans.gameObject.AddComponent<Button>();
                cardBtn.onClick.RemoveAllListeners();
                cardBtn.onClick.AddListener(() => SelectCell(index));

                UpdateCellVisual(index);
            }
        }

        public void SelectCell(int index)
        {
            if (index < 0 || index >= cells.Count) return;
            selectedCellIndex = index;

            for (int i = 0; i < cells.Count; i++)
            {
                UpdateCellVisual(i);
            }
        }

        public void SelectNextUnsolvedCell()
        {
            for (int step = 1; step <= cells.Count; step++)
            {
                int next = (selectedCellIndex + step) % cells.Count;
                if (!cells[next].isSolved)
                {
                    SelectCell(next);
                    return;
                }
            }
        }

        private void UpdateCellVisual(int index)
        {
            if (index < 0 || index >= cells.Count) return;
            SumCell cell = cells[index];

            if (cell.cellCardBg != null)
            {
                if (cell.isSolved)
                {
                    cell.cellCardBg.color = solvedCardColor;
                }
                else if (index == selectedCellIndex)
                {
                    cell.cellCardBg.color = selectedCardColor;
                }
                else
                {
                    cell.cellCardBg.color = normalCardColor;
                }
            }

            if (cell.checkmarkObj != null)
            {
                cell.checkmarkObj.SetActive(cell.isSolved);
            }

            if (cell.goButton != null)
            {
                cell.goButton.interactable = !cell.isSolved;
            }

            if (cell.answerDisplay != null)
            {
                if (cell.isSolved)
                {
                    cell.answerDisplay.text = cell.expectedAnswer.ToString();
                    cell.answerDisplay.color = new Color(0.1f, 0.6f, 0.2f, 1f);
                }
                else
                {
                    cell.answerDisplay.text = cellInputBuffers[index];
                    cell.answerDisplay.color = Color.black;
                }
            }
        }

        public void OnKeypadDigitPressed(string digit)
        {
            if (!isGameActive || isCompleted) return;
            if (selectedCellIndex < 0 || selectedCellIndex >= cells.Count) return;

            if (Time.unscaledTime - lastDigitTime < 0.08f) return;
            lastDigitTime = Time.unscaledTime;

            SumCell cell = cells[selectedCellIndex];
            if (cell.isSolved)
            {
                SelectNextUnsolvedCell();
                return;
            }

            string current = cellInputBuffers[selectedCellIndex];
            if (current.Length < 4) // Max 4 digits
            {
                cellInputBuffers[selectedCellIndex] = current + digit;
                if (cell.answerDisplay != null)
                {
                    cell.answerDisplay.text = cellInputBuffers[selectedCellIndex];
                }
            }
        }

        public void OnKeypadBackspacePressed()
        {
            if (!isGameActive || isCompleted) return;
            if (selectedCellIndex < 0 || selectedCellIndex >= cells.Count) return;

            SumCell cell = cells[selectedCellIndex];
            if (cell.isSolved) return;

            string current = cellInputBuffers[selectedCellIndex];
            if (current.Length > 0)
            {
                cellInputBuffers[selectedCellIndex] = current.Substring(0, current.Length - 1);
                if (cell.answerDisplay != null)
                {
                    cell.answerDisplay.text = cellInputBuffers[selectedCellIndex];
                }
            }
        }

        public void CheckCellAnswer(int index)
        {
            if (index < 0 || index >= cells.Count) return;
            SumCell cell = cells[index];
            if (cell.isSolved) return;

            string enteredStr = cellInputBuffers[index];
            if (string.IsNullOrEmpty(enteredStr))
            {
                ShowFeedback("Enter an answer first! 💡", Color.yellow);
                return;
            }

            if (int.TryParse(enteredStr, out int userAns) && userAns == cell.expectedAnswer)
            {
                // Correct!
                cell.isSolved = true;
                solvedCount++;
                UpdateCellVisual(index);
                UpdateProgressUI();

                if (feedbackTick != null)
                {
                    feedbackTick.gameObject.SetActive(true);
                    StartCoroutine(HideTickRoutine());
                }

                ShowFeedback("Awesome! Correct! 🌟", new Color(0.2f, 0.9f, 0.3f, 1f));
                StartCoroutine(PulseCellAnimation(cell.cellRoot));

                if (solvedCount >= requiredSolves)
                {
                    TriggerVictory();
                }
                else
                {
                    SelectNextUnsolvedCell();
                }
            }
            else
            {
                // Incorrect
                ShowFeedback("Not quite right! Try again! 💪", new Color(1f, 0.3f, 0.3f, 1f));
                StartCoroutine(ShakeCellAnimation(cell.cellRoot));
                cellInputBuffers[index] = "";
                if (cell.answerDisplay != null) cell.answerDisplay.text = "";
            }
        }

        private IEnumerator HideTickRoutine()
        {
            yield return new WaitForSeconds(1.2f);
            if (feedbackTick != null) feedbackTick.gameObject.SetActive(false);
        }

        private void TriggerVictory()
        {
            isGameActive = false;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.score += 50;
            }

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
                if (victoryMessage != null)
                {
                    string pName = (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.playerName))
                        ? GameManager.Instance.playerName
                        : "Pixel";
                    victoryMessage.text = $"Outstanding work, {pName}!\nYou solved all {requiredSolves} required sums!\n+50 Score Earned!";
                }
            }

            if (finishButton != null)
            {
                finishButton.gameObject.SetActive(true);
                finishButton.interactable = true;
                finishButton.onClick.RemoveAllListeners();
                finishButton.onClick.AddListener(() => Complete(true));
            }
        }

        private void UpdateInstructionBanner()
        {
            string pName = "Pixel";
            if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.playerName))
            {
                pName = GameManager.Instance.playerName;
            }

            if (instructionBannerText != null)
            {
                instructionBannerText.text = $"{pName}, solve {requiredSolves} to make your mark!";
            }
        }

        private void UpdateProgressUI()
        {
            if (progressText != null)
            {
                progressText.text = $"Solved: {solvedCount} / {requiredSolves}";
            }
        }

        private void ShowFeedback(string msg, Color col)
        {
            if (feedbackText == null) return;
            feedbackText.text = msg;
            feedbackText.color = col;

            if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
            feedbackCoroutine = StartCoroutine(ClearFeedbackRoutine());
        }

        private IEnumerator ClearFeedbackRoutine()
        {
            yield return new WaitForSeconds(3f);
            if (feedbackText != null) feedbackText.text = "";
        }

        private IEnumerator ShakeCellAnimation(GameObject obj)
        {
            if (obj == null) yield break;
            Vector3 original = obj.transform.localPosition;
            float duration = 0.35f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float x = Mathf.Sin(elapsed * 45f) * 10f;
                obj.transform.localPosition = original + new Vector3(x, 0, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }

            obj.transform.localPosition = original;
        }

        private IEnumerator PulseCellAnimation(GameObject obj)
        {
            if (obj == null) yield break;
            Vector3 original = Vector3.one;
            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                float scale = 1f + Mathf.Sin((elapsed / duration) * Mathf.PI) * 0.12f;
                obj.transform.localScale = original * scale;
                elapsed += Time.deltaTime;
                yield return null;
            }

            obj.transform.localScale = original;
        }
    }
}
