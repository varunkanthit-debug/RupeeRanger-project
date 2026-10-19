using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using RupeeRanger.Core;
using RupeeRanger.Data;

namespace RupeeRanger.MiniGames
{
    public class WordPuzzleMiniGame : MiniGame
    {
        [Header("UI References")]
        public TMP_Text titleText;
        public TMP_Text categoryText;
        public TMP_Text progressText;
        public TMP_Text feedbackText;
        public Image feedbackTick;
        public RectTransform gridContainer;
        public RectTransform wordListContainer;
        public GameObject victoryPanel;
        public TMP_Text victoryMessage;
        public Button finishButton;
        public Button closeButton;

        [Header("Word Search Data")]
        public TextAsset wordListsJson;
        [SerializeField] private int gridSize = 8;
        [SerializeField] private List<string> targetWords = new List<string>();
        private HashSet<string> foundWords = new HashSet<string>();

        private char[,] letterMatrix;
        private WordSearchCell[,] cellMatrix;
        private List<WordSearchCell> currentSelection = new List<WordSearchCell>();
        private WordSearchCell startCell = null;
        private bool isDragging = false;

        private static readonly Color[] HighlightColors = new Color[]
        {
            new Color32(0xFF, 0x8A, 0x80, 0xEE), // Pink
            new Color32(0x80, 0xCB, 0xC4, 0xEE), // Teal
            new Color32(0xA5, 0xD6, 0xA7, 0xEE), // Green
            new Color32(0xFF, 0xCC, 0x80, 0xEE), // Orange
            new Color32(0xCE, 0x93, 0xD8, 0xEE), // Purple
            new Color32(0x90, 0xCA, 0xF9, 0xEE), // Blue
            new Color32(0xFF, 0xF5, 0x9D, 0xEE), // Yellow
            new Color32(0xB0, 0xBE, 0xC5, 0xEE)  // Slate
        };
        private int colorIndex = 0;

        private Dictionary<string, TMP_Text> wordListLabels = new Dictionary<string, TMP_Text>();

        private bool isSetupDone = false;

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
            gameTitle = "Word Search Puzzle";

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

            LoadWordsForDifficulty(band);
        }

        public override void StartGame()
        {
            isGameActive = true;
            isCompleted = false;
            foundWords.Clear();
            colorIndex = 0;

            GenerateGrid();
            BuildUIWordList();
            UpdateProgress();

            if (feedbackText != null) feedbackText.text = "Drag across letters to find hidden words!";
        }

        private static HashSet<string> playedWordSearchCategories = new HashSet<string>();

        private void LoadWordsForDifficulty(DifficultyBand band)
        {
            targetWords.Clear();

            string json = (wordListsJson != null) ? wordListsJson.text : null;
            if (string.IsNullOrEmpty(json))
            {
                TextAsset loadedAsset = Resources.Load<TextAsset>("WordLists");
                if (loadedAsset != null) json = loadedAsset.text;
            }

            List<WordSearchPuzzleData> pool = new List<WordSearchPuzzleData>();

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    WordDatabase db = JsonUtility.FromJson<WordDatabase>(json);
                    if (db != null && db.wordSearchPuzzles != null)
                    {
                        string bandStr = band.ToString();
                        pool = db.wordSearchPuzzles.FindAll(p => p.difficulty.Equals(bandStr, StringComparison.OrdinalIgnoreCase));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WordPuzzle] Error reading JSON: {ex.Message}");
                }
            }

            if (pool.Count == 0)
            {
                if (band == DifficultyBand.Easy)
                {
                    pool.Add(new WordSearchPuzzleData { difficulty = "Easy", category = "Rupee Savings", gridSize = 8, words = new List<string> { "COIN", "CASH", "SAVE", "BANK", "BUY" } });
                    pool.Add(new WordSearchPuzzleData { difficulty = "Easy", category = "Pocket Money", gridSize = 8, words = new List<string> { "RUPEE", "NOTE", "SAFE", "COST", "FUND" } });
                    pool.Add(new WordSearchPuzzleData { difficulty = "Easy", category = "Fruits Market", gridSize = 8, words = new List<string> { "APPLE", "MANGO", "BERRY", "PEAR", "PLUM" } });
                    pool.Add(new WordSearchPuzzleData { difficulty = "Easy", category = "Friendly Pets", gridSize = 8, words = new List<string> { "FISH", "BEAR", "CROW", "PONY", "PUPPY" } });
                }
                else if (band == DifficultyBand.Medium)
                {
                    pool.Add(new WordSearchPuzzleData { difficulty = "Medium", category = "Financial Town", gridSize = 10, words = new List<string> { "MONEY", "RUPEE", "PRICE", "SPEND", "STORE", "SMART" } });
                    pool.Add(new WordSearchPuzzleData { difficulty = "Medium", category = "Banking Basics", gridSize = 10, words = new List<string> { "DEPOSIT", "CHEQUE", "VAULT", "ASSET", "CREDIT", "AUDIT" } });
                    pool.Add(new WordSearchPuzzleData { difficulty = "Medium", category = "Smart Shopping", gridSize = 10, words = new List<string> { "MARKET", "BARGAIN", "CART", "RECEIPT", "COUPON", "VALUE" } });
                    pool.Add(new WordSearchPuzzleData { difficulty = "Medium", category = "Tech & Digital", gridSize = 10, words = new List<string> { "ROBOT", "CODING", "SCREEN", "DIGITAL", "SYSTEM", "SIGNAL" } });
                }
                else
                {
                    pool.Add(new WordSearchPuzzleData { difficulty = "Hard", category = "Financial Literacy", gridSize = 12, words = new List<string> { "BUDGET", "PROFIT", "CREDIT", "INVEST", "WALLET", "BALANCE", "ACCOUNT" } });
                    pool.Add(new WordSearchPuzzleData { difficulty = "Hard", category = "Wealth & Economy", gridSize = 12, words = new List<string> { "CURRENCY", "CAPITAL", "DIVIDEND", "INTEREST", "COMPOUND", "PORTFOLIO", "STOCKS" } });
                    pool.Add(new WordSearchPuzzleData { difficulty = "Hard", category = "Career & Success", gridSize = 12, words = new List<string> { "DEGREE", "COLLEGE", "SALARY", "MANAGER", "PROJECT", "VENTURE", "FOUNDER" } });
                    pool.Add(new WordSearchPuzzleData { difficulty = "Hard", category = "Kingdom Wildlife", gridSize = 12, words = new List<string> { "CHEETAH", "OSTRICH", "DOLPHIN", "PANTHER", "FALCON", "GIRAFFE", "LEOPARD" } });
                }
            }

            // Exclude already played categories
            List<WordSearchPuzzleData> unplayed = pool.FindAll(p => !playedWordSearchCategories.Contains(p.category));
            if (unplayed.Count == 0)
            {
                playedWordSearchCategories.Clear();
                unplayed = pool;
            }

            var chosen = unplayed[UnityEngine.Random.Range(0, unplayed.Count)];
            playedWordSearchCategories.Add(chosen.category);

            gridSize = chosen.gridSize;
            targetWords.AddRange(chosen.words);
            if (categoryText != null) categoryText.text = $"Theme: {chosen.category} ({DifficultyHelper.GetLabel(band)})";
        }

        private void GenerateGrid()
        {
            letterMatrix = new char[gridSize, gridSize];
            for (int r = 0; r < gridSize; r++)
                for (int c = 0; c < gridSize; c++)
                    letterMatrix[r, c] = ' ';

            // Place words
            Vector2Int[] directions = (difficultyBand == DifficultyBand.Easy)
                ? new[] { new Vector2Int(0, 1), new Vector2Int(1, 0) } // Horizontal & Vertical
                : new[] { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(-1, 1) }; // + Diagonals

            foreach (var word in targetWords)
            {
                PlaceWord(word.ToUpper(), directions);
            }

            // Fill empty cells
            for (int r = 0; r < gridSize; r++)
            {
                for (int c = 0; c < gridSize; c++)
                {
                    if (letterMatrix[r, c] == ' ')
                    {
                        letterMatrix[r, c] = (char)('A' + UnityEngine.Random.Range(0, 26));
                    }
                }
            }

            RenderGridUI();
        }

        private void PlaceWord(string word, Vector2Int[] directions)
        {
            int attempts = 0;
            while (attempts < 200)
            {
                attempts++;
                Vector2Int dir = directions[UnityEngine.Random.Range(0, directions.Length)];
                int startR = UnityEngine.Random.Range(0, gridSize);
                int startC = UnityEngine.Random.Range(0, gridSize);

                int endR = startR + dir.x * (word.Length - 1);
                int endC = startC + dir.y * (word.Length - 1);

                if (endR >= 0 && endR < gridSize && endC >= 0 && endC < gridSize)
                {
                    bool canPlace = true;
                    for (int i = 0; i < word.Length; i++)
                    {
                        int r = startR + dir.x * i;
                        int c = startC + dir.y * i;
                        if (letterMatrix[r, c] != ' ' && letterMatrix[r, c] != word[i])
                        {
                            canPlace = false;
                            break;
                        }
                    }

                    if (canPlace)
                    {
                        for (int i = 0; i < word.Length; i++)
                        {
                            int r = startR + dir.x * i;
                            int c = startC + dir.y * i;
                            letterMatrix[r, c] = word[i];
                        }
                        return;
                    }
                }
            }
        }

        private void RenderGridUI()
        {
            if (gridContainer == null) return;

            // Clear previous cells
            foreach (Transform child in gridContainer)
            {
                Destroy(child.gameObject);
            }

            GridLayoutGroup glg = gridContainer.GetComponent<GridLayoutGroup>();
            if (glg == null)
            {
                glg = gridContainer.gameObject.AddComponent<GridLayoutGroup>();
            }

            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = gridSize;
            glg.childAlignment = TextAnchor.MiddleCenter;

            float containerW = (gridContainer.rect.width > 100f) ? gridContainer.rect.width : 660f;
            float containerH = (gridContainer.rect.height > 100f) ? gridContainer.rect.height : 660f;
            float containerSize = Mathf.Min(containerW, containerH);
            float spacing = (gridSize <= 8) ? 6f : (gridSize <= 10) ? 4f : 3f;
            float paddingTotal = spacing * (gridSize + 1);
            float cellSize = (containerSize - paddingTotal) / gridSize;
            glg.cellSize = new Vector2(cellSize, cellSize);
            glg.spacing = new Vector2(spacing, spacing);
            glg.padding = new RectOffset((int)spacing, (int)spacing, (int)spacing, (int)spacing);

            cellMatrix = new WordSearchCell[gridSize, gridSize];

            for (int r = 0; r < gridSize; r++)
            {
                for (int c = 0; c < gridSize; c++)
                {
                    GameObject cellObj = new GameObject($"Cell_{r}_{c}", typeof(RectTransform), typeof(Image), typeof(WordSearchCell));
                    cellObj.transform.SetParent(gridContainer, false);

                    RectTransform cellRt = cellObj.GetComponent<RectTransform>();
                    cellRt.sizeDelta = new Vector2(cellSize, cellSize);

                    Image img = cellObj.GetComponent<Image>();
                    img.color = new Color32(0xFA, 0xFA, 0xFC, 0xFF); // Crisp off-white cell

                    WordSearchCell cell = cellObj.GetComponent<WordSearchCell>();
                    cell.row = r;
                    cell.col = c;
                    cell.letter = letterMatrix[r, c];
                    cell.game = this;

                    GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    txtObj.transform.SetParent(cellObj.transform, false);
                    TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>();
                    txt.text = cell.letter.ToString();
                    txt.fontSize = (gridSize <= 8) ? 32 : (gridSize <= 10) ? 26 : 20;
                    txt.fontStyle = FontStyles.Bold;
                    txt.color = new Color32(0x18, 0x22, 0x36, 0xFF); // Dark navy/charcoal for max contrast
                    txt.alignment = TextAlignmentOptions.Center;
                    txt.raycastTarget = false;

                    RectTransform txtRt = txtObj.GetComponent<RectTransform>();
                    txtRt.anchorMin = Vector2.zero;
                    txtRt.anchorMax = Vector2.one;
                    txtRt.sizeDelta = Vector2.zero;

                    cell.label = txt;
                    cellMatrix[r, c] = cell;
                }
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridContainer);
        }

        private void BuildUIWordList()
        {
            if (wordListContainer == null) return;

            wordListLabels.Clear();
            foreach (Transform child in wordListContainer)
            {
                Destroy(child.gameObject);
            }

            VerticalLayoutGroup vlg = wordListContainer.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
            {
                vlg = wordListContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 10f;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.childAlignment = TextAnchor.UpperLeft;

            foreach (var word in targetWords)
            {
                GameObject itemObj = new GameObject($"Word_{word}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                itemObj.transform.SetParent(wordListContainer, false);

                Image itemBg = itemObj.GetComponent<Image>();
                itemBg.color = new Color32(0x1E, 0x2C, 0x46, 0xEE); // Card pill background

                LayoutElement le = itemObj.GetComponent<LayoutElement>();
                le.minHeight = 44f;
                le.preferredHeight = 44f;

                GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtObj.transform.SetParent(itemObj.transform, false);

                RectTransform txtRt = txtObj.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = new Vector2(16f, 0f);
                txtRt.offsetMax = new Vector2(-16f, 0f);

                TextMeshProUGUI itemText = txtObj.GetComponent<TextMeshProUGUI>();
                itemText.text = $"•  {word}";
                itemText.fontSize = 22;
                itemText.fontStyle = FontStyles.Bold;
                itemText.color = new Color32(0xFF, 0xD7, 0x00, 0xFF); // Bright Gold on dark pill
                itemText.alignment = TextAlignmentOptions.MidlineLeft;

                wordListLabels[word] = itemText;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(wordListContainer);
        }

        public void OnCellPointerDown(WordSearchCell cell)
        {
            if (!isGameActive) return;
            isDragging = true;
            startCell = cell;
            currentSelection.Clear();
            currentSelection.Add(cell);
            HighlightSelection();
        }

        public void OnCellPointerEnter(WordSearchCell cell)
        {
            if (!isDragging || startCell == null) return;

            int dr = cell.row - startCell.row;
            int dc = cell.col - startCell.col;

            // Straight line check (horizontal, vertical, diagonal)
            int stepR = (dr == 0) ? 0 : (dr > 0 ? 1 : -1);
            int stepC = (dc == 0) ? 0 : (dc > 0 ? 1 : -1);

            bool isValidLine = (dr == 0 || dc == 0 || Mathf.Abs(dr) == Mathf.Abs(dc));
            if (!isValidLine) return;

            int steps = Mathf.Max(Mathf.Abs(dr), Mathf.Abs(dc));
            currentSelection.Clear();
            for (int i = 0; i <= steps; i++)
            {
                int r = startCell.row + stepR * i;
                int c = startCell.col + stepC * i;
                if (r >= 0 && r < gridSize && c >= 0 && c < gridSize)
                {
                    currentSelection.Add(cellMatrix[r, c]);
                }
            }

            HighlightSelection();
        }

        public void OnCellPointerUp()
        {
            if (!isDragging) return;
            isDragging = false;

            if (currentSelection.Count > 1)
            {
                string word = "";
                foreach (var cell in currentSelection) word += cell.letter;
                string revWord = "";
                for (int i = word.Length - 1; i >= 0; i--) revWord += word[i];

                string matched = null;
                if (targetWords.Contains(word) && !foundWords.Contains(word)) matched = word;
                else if (targetWords.Contains(revWord) && !foundWords.Contains(revWord)) matched = revWord;

                if (matched != null)
                {
                    // Success!
                    foundWords.Add(matched);
                    Color highlightCol = HighlightColors[colorIndex % HighlightColors.Length];
                    colorIndex++;

                    foreach (var cell in currentSelection)
                    {
                        cell.SetPermanentHighlight(highlightCol);
                    }

                    if (wordListLabels.TryGetValue(matched, out TMP_Text lbl))
                    {
                        lbl.text = $"<s>[OK] {matched}</s>";
                        lbl.color = new Color32(0x00, 0xC8, 0x53, 0xFF);
                    }

                    ShowFeedback($"Found \"{matched}\"!", true);
                    UpdateProgress();

                    if (foundWords.Count >= targetWords.Count)
                    {
                        TriggerVictory();
                    }
                }
                else
                {
                    // Invalid word: shake red
                    StartCoroutine(FlashRedAndClear(new List<WordSearchCell>(currentSelection)));
                }
            }
            else
            {
                ClearSelectionHighlight();
            }

            startCell = null;
        }

        private void HighlightSelection()
        {
            for (int r = 0; r < gridSize; r++)
            {
                for (int c = 0; c < gridSize; c++)
                {
                    cellMatrix[r, c].SetTemporaryHighlight(currentSelection.Contains(cellMatrix[r, c]));
                }
            }
        }

        private void ClearSelectionHighlight()
        {
            for (int r = 0; r < gridSize; r++)
            {
                for (int c = 0; c < gridSize; c++)
                {
                    cellMatrix[r, c].SetTemporaryHighlight(false);
                }
            }
        }

        private IEnumerator FlashRedAndClear(List<WordSearchCell> cells)
        {
            foreach (var c in cells) c.FlashRed();
            ShowFeedback("Try again!", false);
            yield return new WaitForSeconds(0.35f);
            ClearSelectionHighlight();
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

        private void UpdateProgress()
        {
            if (progressText != null)
            {
                progressText.text = $"Words Found: {foundWords.Count} / {targetWords.Count}";
            }
        }

        private void TriggerVictory()
        {
            isGameActive = false;

            if (victoryPanel != null)
            {
                if (victoryMessage != null)
                {
                    victoryMessage.text = $"Incredible Job!\nYou found all {targetWords.Count} words!\n+500 Reward & Task Completed!";
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

    public class WordSearchCell : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
    {
        public int row;
        public int col;
        public char letter;
        public WordPuzzleMiniGame game;
        public TextMeshProUGUI label;

        private Image img;
        private Color defaultColor = new Color32(0xFF, 0xFF, 0xFF, 0xF0);
        private Color currentPermanentColor = Color.clear;
        private bool isPermanentlyFound = false;

        private void Awake()
        {
            img = GetComponent<Image>();
        }

        public void SetTemporaryHighlight(bool highlight)
        {
            if (isPermanentlyFound) return;
            if (img != null)
            {
                img.color = highlight ? new Color32(0x80, 0xD8, 0xFF, 0xFA) : defaultColor;
            }
        }

        public void SetPermanentHighlight(Color color)
        {
            isPermanentlyFound = true;
            currentPermanentColor = color;
            if (img != null) img.color = color;
            if (label != null) label.color = new Color32(0x10, 0x1E, 0x30, 0xFF);
        }

        public void FlashRed()
        {
            if (isPermanentlyFound) return;
            if (img != null) img.color = new Color32(0xFF, 0xCD, 0xD2, 0xFA);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (game != null) game.OnCellPointerDown(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (game != null) game.OnCellPointerEnter(this);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (game != null) game.OnCellPointerUp();
        }
    }
}
