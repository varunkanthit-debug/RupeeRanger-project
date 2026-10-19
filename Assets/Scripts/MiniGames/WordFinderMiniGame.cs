using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RupeeRanger.Core;
using RupeeRanger.Data;

namespace RupeeRanger.MiniGames
{
    public class WordFinderMiniGame : MiniGame
    {
        [Header("UI References")]
        public TMP_Text titleText;
        public TMP_Text subTitleText;
        public TMP_Text feedbackText;
        public Image feedbackTick;
        public RectTransform answerBoxesContainer;
        public RectTransform letterButtonsContainer;
        public TMP_Text currentWordDisplay;
        public Button submitButton;
        public Button clearButton;
        public Button shuffleButton;
        public GameObject victoryPanel;
        public TMP_Text victoryMessage;
        public Button finishButton;
        public Button closeButton;

        [Header("Puzzle Settings")]
        public TextAsset wordListsJson;
        [SerializeField] private List<string> availableLetters = new List<string> { "R", "A", "W" };
        [SerializeField] private List<string> targetWords = new List<string> { "RAW", "WAR" };
        private HashSet<string> solvedWords = new HashSet<string>();

        private string currentWord = "";
        private Dictionary<string, List<TextMeshProUGUI>> wordSlotMap = new Dictionary<string, List<TextMeshProUGUI>>();
        private List<Button> letterButtons = new List<Button>();

        private void Update()
        {
            if (!isGameActive) return;

#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                {
                    OnSubmitTapped();
                    return;
                }
                if (kb.backspaceKey.wasPressedThisFrame || kb.deleteKey.wasPressedThisFrame)
                {
                    OnClearTapped();
                    return;
                }

                // Check letter keys A-Z
                for (int i = 0; i < 26; i++)
                {
                    var key = (UnityEngine.InputSystem.Key)((int)UnityEngine.InputSystem.Key.A + i);
                    if (kb[key].wasPressedThisFrame)
                    {
                        char c = (char)('A' + i);
                        OnLetterTapped(c.ToString());
                        break;
                    }
                }
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                foreach (char c in Input.inputString)
                {
                    char upper = char.ToUpper(c);
                    if (char.IsLetter(upper))
                    {
                        OnLetterTapped(upper.ToString());
                    }
                    else if (c == '\b')
                    {
                        OnClearTapped();
                    }
                    else if (c == '\r' || c == '\n')
                    {
                        OnSubmitTapped();
                    }
                }
            }
            catch {}
#endif
        }

        private bool isSetupDone = false;
        private float lastLetterTime = 0f;

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
            gameTitle = "Word Finder";

            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(OnSubmitTapped);
            }
            if (clearButton != null)
            {
                clearButton.onClick.RemoveAllListeners();
                clearButton.onClick.AddListener(OnClearTapped);
            }
            if (shuffleButton != null)
            {
                shuffleButton.onClick.RemoveAllListeners();
                shuffleButton.onClick.AddListener(OnShuffleTapped);
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

            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (feedbackTick != null) feedbackTick.gameObject.SetActive(false);

            LoadPuzzleData(band);
        }

        public override void StartGame()
        {
            isGameActive = true;
            isCompleted = false;
            solvedWords.Clear();
            currentWord = "";
            UpdateCurrentWordDisplay();

            BuildAnswerBoxesUI();
            BuildLetterButtonsUI();

            if (feedbackText != null) feedbackText.text = "Tap or drag letters to form valid words!";
        }

        private static HashSet<string> playedPuzzles = new HashSet<string>();

        private void LoadPuzzleData(DifficultyBand band)
        {
            string json = (wordListsJson != null) ? wordListsJson.text : null;
            if (string.IsNullOrEmpty(json))
            {
                TextAsset loadedAsset = Resources.Load<TextAsset>("WordLists");
                if (loadedAsset != null) json = loadedAsset.text;
            }

            List<WordFinderPuzzleData> pool = new List<WordFinderPuzzleData>();

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    WordDatabase db = JsonUtility.FromJson<WordDatabase>(json);
                    if (db != null && db.wordFinderPuzzles != null)
                    {
                        string bandStr = band.ToString();
                        pool = db.wordFinderPuzzles.FindAll(p => p.difficulty.Equals(bandStr, StringComparison.OrdinalIgnoreCase));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WordFinder] Error reading JSON: {ex.Message}");
                }
            }

            // Rich fallbacks if JSON fails
            if (pool.Count == 0)
            {
                if (band == DifficultyBand.Easy)
                {
                    pool.Add(new WordFinderPuzzleData { difficulty = "Easy", letters = new List<string> { "R", "A", "W" }, targetWords = new List<string> { "RAW", "WAR" } });
                    pool.Add(new WordFinderPuzzleData { difficulty = "Easy", letters = new List<string> { "T", "E", "A" }, targetWords = new List<string> { "TEA", "EAT", "ATE" } });
                    pool.Add(new WordFinderPuzzleData { difficulty = "Easy", letters = new List<string> { "C", "A", "T" }, targetWords = new List<string> { "CAT", "ACT" } });
                    pool.Add(new WordFinderPuzzleData { difficulty = "Easy", letters = new List<string> { "P", "O", "T" }, targetWords = new List<string> { "POT", "TOP" } });
                    pool.Add(new WordFinderPuzzleData { difficulty = "Easy", letters = new List<string> { "B", "A", "T" }, targetWords = new List<string> { "BAT", "TAB" } });
                    pool.Add(new WordFinderPuzzleData { difficulty = "Easy", letters = new List<string> { "T", "A", "P" }, targetWords = new List<string> { "TAP", "PAT", "APT" } });
                    pool.Add(new WordFinderPuzzleData { difficulty = "Easy", letters = new List<string> { "N", "E", "T" }, targetWords = new List<string> { "NET", "TEN" } });
                }
                else if (band == DifficultyBand.Medium)
                {
                    pool.Add(new WordFinderPuzzleData { difficulty = "Medium", letters = new List<string> { "S", "T", "A", "R" }, targetWords = new List<string> { "STAR", "RATS", "ARTS", "TARS" } });
                    pool.Add(new WordFinderPuzzleData { difficulty = "Medium", letters = new List<string> { "S", "P", "O", "T" }, targetWords = new List<string> { "SPOT", "STOP", "POTS", "TOPS", "POST" } });
                    pool.Add(new WordFinderPuzzleData { difficulty = "Medium", letters = new List<string> { "S", "A", "V", "E" }, targetWords = new List<string> { "SAVE", "VASE" } });
                    pool.Add(new WordFinderPuzzleData { difficulty = "Medium", letters = new List<string> { "C", "O", "I", "N" }, targetWords = new List<string> { "COIN", "ICON" } });
                    pool.Add(new WordFinderPuzzleData { difficulty = "Medium", letters = new List<string> { "E", "A", "R", "N" }, targetWords = new List<string> { "EARN", "NEAR" } });
                    pool.Add(new WordFinderPuzzleData { difficulty = "Medium", letters = new List<string> { "C", "A", "R", "E" }, targetWords = new List<string> { "CARE", "RACE", "ACRE" } });
                    pool.Add(new WordFinderPuzzleData { difficulty = "Medium", letters = new List<string> { "T", "E", "A", "M" }, targetWords = new List<string> { "TEAM", "MATE", "MEAT", "TAME" } });
                }
                else
                {
                    pool.Add(new WordFinderPuzzleData { difficulty = "Hard", letters = new List<string> { "S", "M", "A", "R", "T" }, targetWords = new List<string> { "SMART", "MART", "STAR", "ARMS", "TRAMS" } });
                    pool.Add(new WordFinderPuzzleData { difficulty = "Hard", letters = new List<string> { "S", "H", "A", "R", "E" }, targetWords = new List<string> { "SHARE", "HEARS", "HARE", "EARS", "HEAR" } });
                    pool.Add(new WordFinderPuzzleData { difficulty = "Hard", letters = new List<string> { "P", "R", "I", "C", "E" }, targetWords = new List<string> { "PRICE", "RIPE", "PIER", "RICE" } });
                    pool.Add(new WordFinderPuzzleData { difficulty = "Hard", letters = new List<string> { "A", "S", "S", "E", "T" }, targetWords = new List<string> { "ASSET", "SEAT", "EAST", "SETS" } });
                    pool.Add(new WordFinderPuzzleData { difficulty = "Hard", letters = new List<string> { "E", "A", "R", "T", "H" }, targetWords = new List<string> { "EARTH", "HEART", "HATER", "RATE", "HEAR", "TEAR" } });
                    pool.Add(new WordFinderPuzzleData { difficulty = "Hard", letters = new List<string> { "S", "T", "O", "R", "E" }, targetWords = new List<string> { "STORE", "REST", "SORE", "TORE", "ROSE" } });
                }
            }

            // Exclude already played puzzles
            List<WordFinderPuzzleData> unplayed = pool.FindAll(p => !playedPuzzles.Contains(string.Join("", p.letters)));
            if (unplayed.Count == 0)
            {
                // All played once, reset set so player can cycle afresh with no lockouts
                playedPuzzles.Clear();
                unplayed = pool;
            }

            var chosen = unplayed[UnityEngine.Random.Range(0, unplayed.Count)];
            playedPuzzles.Add(string.Join("", chosen.letters));

            availableLetters = new List<string>(chosen.letters);
            targetWords = new List<string>(chosen.targetWords);

            if (subTitleText != null) 
                subTitleText.text = $"Find all {targetWords.Count} words! ({DifficultyHelper.GetLabel(band)})";
        }

        private void BuildAnswerBoxesUI()
        {
            if (answerBoxesContainer == null) return;

            wordSlotMap.Clear();
            foreach (Transform child in answerBoxesContainer)
            {
                Destroy(child.gameObject);
            }

            VerticalLayoutGroup vlg = answerBoxesContainer.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = answerBoxesContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 14f;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;

            foreach (var word in targetWords)
            {
                GameObject rowObj = new GameObject($"Row_{word}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                rowObj.transform.SetParent(answerBoxesContainer, false);

                HorizontalLayoutGroup hlg = rowObj.GetComponent<HorizontalLayoutGroup>();
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.spacing = 8f;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;

                RectTransform rowRt = rowObj.GetComponent<RectTransform>();
                rowRt.sizeDelta = new Vector2(word.Length * 56f, 52f);

                List<TextMeshProUGUI> boxTexts = new List<TextMeshProUGUI>();

                for (int i = 0; i < word.Length; i++)
                {
                    GameObject boxObj = new GameObject($"Box_{i}", typeof(RectTransform), typeof(Image));
                    boxObj.transform.SetParent(rowObj.transform, false);

                    RectTransform boxRt = boxObj.GetComponent<RectTransform>();
                    boxRt.sizeDelta = new Vector2(48f, 48f);

                    Image boxImg = boxObj.GetComponent<Image>();
                    boxImg.color = new Color32(0xFF, 0xFF, 0xFF, 0xEE);

                    GameObject txtObj = new GameObject("CharText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    txtObj.transform.SetParent(boxObj.transform, false);

                    RectTransform txtRt = txtObj.GetComponent<RectTransform>();
                    txtRt.anchorMin = Vector2.zero;
                    txtRt.anchorMax = Vector2.one;
                    txtRt.sizeDelta = Vector2.zero;

                    TextMeshProUGUI boxTxt = txtObj.GetComponent<TextMeshProUGUI>();
                    boxTxt.text = "";
                    boxTxt.fontSize = 26;
                    boxTxt.fontStyle = FontStyles.Bold;
                    boxTxt.color = new Color32(0x11, 0x55, 0xCC, 0xFF);
                    boxTxt.alignment = TextAlignmentOptions.Center;

                    boxTexts.Add(boxTxt);
                }

                wordSlotMap[word] = boxTexts;
            }
        }

        private void BuildLetterButtonsUI()
        {
            if (letterButtonsContainer == null) return;

            letterButtons.Clear();
            foreach (Transform child in letterButtonsContainer)
            {
                Destroy(child.gameObject);
            }

            int count = availableLetters.Count;
            float radius = 90f;

            for (int i = 0; i < count; i++)
            {
                string letter = availableLetters[i];
                float angle = (i * (360f / count) + 90f) * Mathf.Deg2Rad;
                Vector2 pos = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);

                GameObject btnObj = new GameObject($"Btn_{letter}", typeof(RectTransform), typeof(Image), typeof(Button));
                btnObj.transform.SetParent(letterButtonsContainer, false);

                RectTransform btnRt = btnObj.GetComponent<RectTransform>();
                btnRt.sizeDelta = new Vector2(64f, 64f);
                btnRt.anchoredPosition = pos;

                Image btnImg = btnObj.GetComponent<Image>();
                btnImg.color = new Color32(0x00, 0x88, 0xEA, 0xFF);

                GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtObj.transform.SetParent(btnObj.transform, false);
                RectTransform txtRt = txtObj.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.sizeDelta = Vector2.zero;

                TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>();
                txt.text = letter;
                txt.fontSize = 30;
                txt.fontStyle = FontStyles.Bold;
                txt.color = Color.white;
                txt.alignment = TextAlignmentOptions.Center;

                Button btn = btnObj.GetComponent<Button>();
                string capturedLetter = letter;
                btn.onClick.AddListener(() => OnLetterTapped(capturedLetter));

                letterButtons.Add(btn);
            }
        }

        private void OnLetterTapped(string letter)
        {
            if (!isGameActive || isCompleted) return;

            if (Time.unscaledTime - lastLetterTime < 0.08f) return;
            lastLetterTime = Time.unscaledTime;

            // Count frequency in available vs current
            int availCount = 0;
            foreach (var l in availableLetters) if (l.Equals(letter, StringComparison.OrdinalIgnoreCase)) availCount++;

            int currCount = 0;
            foreach (char c in currentWord) if (c.ToString().Equals(letter, StringComparison.OrdinalIgnoreCase)) currCount++;

            if (currCount < availCount)
            {
                currentWord += letter.ToUpper();
                UpdateCurrentWordDisplay();

                // Auto-check if word length matches
                if (targetWords.Contains(currentWord) && !solvedWords.Contains(currentWord))
                {
                    OnSubmitTapped();
                }
            }
        }

        private void OnSubmitTapped()
        {
            if (!isGameActive || string.IsNullOrEmpty(currentWord)) return;

            string candidate = currentWord.ToUpper();

            if (targetWords.Contains(candidate))
            {
                if (solvedWords.Contains(candidate))
                {
                    ShowFeedback($"Already solved \"{candidate}\"!", false);
                    StartCoroutine(ShakeDisplay());
                }
                else
                {
                    // Success!
                    solvedWords.Add(candidate);
                    FillWordSlot(candidate);
                    ShowFeedback($"Awesome! \"{candidate}\" is correct!", true);

                    currentWord = "";
                    UpdateCurrentWordDisplay();

                    if (solvedWords.Count >= targetWords.Count)
                    {
                        TriggerVictory();
                    }
                }
            }
            else
            {
                ShowFeedback("Not in the word list! Try again.", false);
                StartCoroutine(ShakeDisplay());
                currentWord = "";
                UpdateCurrentWordDisplay();
            }
        }

        private void OnClearTapped()
        {
            currentWord = "";
            UpdateCurrentWordDisplay();
        }

        private void OnShuffleTapped()
        {
            for (int i = 0; i < availableLetters.Count; i++)
            {
                int r = UnityEngine.Random.Range(i, availableLetters.Count);
                string temp = availableLetters[i];
                availableLetters[i] = availableLetters[r];
                availableLetters[r] = temp;
            }
            BuildLetterButtonsUI();
        }

        private void FillWordSlot(string word)
        {
            if (wordSlotMap.TryGetValue(word, out List<TextMeshProUGUI> texts))
            {
                for (int i = 0; i < word.Length; i++)
                {
                    texts[i].text = word[i].ToString();
                    Image boxImg = texts[i].transform.parent.GetComponent<Image>();
                    if (boxImg != null)
                    {
                        boxImg.color = new Color32(0x00, 0x88, 0xEA, 0xFF);
                    }
                    texts[i].color = Color.white;
                }
            }
        }

        private void UpdateCurrentWordDisplay()
        {
            if (currentWordDisplay != null)
            {
                currentWordDisplay.text = string.IsNullOrEmpty(currentWord) ? "..." : currentWord;
            }
        }

        private IEnumerator ShakeDisplay()
        {
            if (currentWordDisplay == null) yield break;
            Vector3 orig = currentWordDisplay.rectTransform.anchoredPosition;
            for (int i = 0; i < 6; i++)
            {
                float offset = (i % 2 == 0) ? 8f : -8f;
                currentWordDisplay.rectTransform.anchoredPosition = new Vector2(orig.x + offset, orig.y);
                yield return new WaitForSeconds(0.04f);
            }
            currentWordDisplay.rectTransform.anchoredPosition = orig;
        }

        private void ShowFeedback(string text, bool isSuccess)
        {
            if (feedbackText != null)
            {
                feedbackText.text = text;
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
            yield return new WaitForSeconds(1.5f);
            if (feedbackTick != null) feedbackTick.gameObject.SetActive(false);
        }

        private void TriggerVictory()
        {
            isGameActive = false;

            if (victoryPanel != null)
            {
                if (victoryMessage != null)
                {
                    victoryMessage.text = $"Splendid!\nYou found all {targetWords.Count} words!\n+500 Reward & Task Completed!";
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
