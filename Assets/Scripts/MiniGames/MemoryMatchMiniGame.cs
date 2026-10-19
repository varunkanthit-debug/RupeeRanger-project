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
    public class MemoryMatchMiniGame : MiniGame
    {
        [System.Serializable]
        public class CardData
        {
            public string symbol;
            public string title;
            public Color32 themeColor;
        }

        [Header("UI References")]
        public TMP_Text titleText;
        public TMP_Text subTitleText;
        public TMP_Text statsText;
        public TMP_Text feedbackText;
        public RectTransform cardsGridContainer;
        public GameObject victoryPanel;
        public TMP_Text victoryMessage;
        public Button finishButton;
        public Button closeButton;

        private static readonly CardData[] ALL_SYMBOLS = new CardData[]
        {
            new CardData { symbol = "RS.", title = "Rupee Coin", themeColor = new Color32(0xFF, 0xD7, 0x00, 0xFF) },
            new CardData { symbol = "BANK", title = "Savings Bank", themeColor = new Color32(0x00, 0xE5, 0xFF, 0xFF) },
            new CardData { symbol = "PIGGY", title = "Emergency Fund", themeColor = new Color32(0xFF, 0x40, 0x81, 0xFF) },
            new CardData { symbol = "CHART", title = "Compound Growth", themeColor = new Color32(0x00, 0xE6, 0x76, 0xFF) },
            new CardData { symbol = "SHIELD", title = "Health Insurance", themeColor = new Color32(0x7C, 0x4D, 0xFF, 0xFF) },
            new CardData { symbol = "WALLET", title = "Monthly Budget", themeColor = new Color32(0xFF, 0x91, 0x00, 0xFF) },
            new CardData { symbol = "BOOK", title = "Financial Study", themeColor = new Color32(0x29, 0xB6, 0xF6, 0xFF) },
            new CardData { symbol = "TROPHY", title = "Debt Free Goal", themeColor = new Color32(0xFF, 0xC4, 0x00, 0xFF) },
            new CardData { symbol = "STORE", title = "Smart Grocery", themeColor = new Color32(0x26, 0xA6, 0x9A, 0xFF) },
            new CardData { symbol = "ASSET", title = "Future Wealth", themeColor = new Color32(0xAB, 0x47, 0xBC, 0xFF) }
        };

        private class CardInstance
        {
            public int cardId;
            public int pairIndex;
            public CardData data;
            public GameObject obj;
            public Button button;
            public Image bgImage;
            public TMP_Text symbolText;
            public TMP_Text titleText;
            public bool isFaceUp;
            public bool isMatched;
        }

        private List<CardInstance> cards = new List<CardInstance>();
        private CardInstance firstSelected = null;
        private CardInstance secondSelected = null;
        private bool isCheckingMatch = false;

        private int movesCount = 0;
        private int pairsMatched = 0;
        private int totalPairs = 6;
        private float timeRemaining = 90f;
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

        private void Update()
        {
            if (!isGameActive || isCompleted) return;

            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                UpdateStatsUI();
                OnTimeExpired();
                return;
            }

            UpdateStatsUI();
        }

        public override void Setup(DifficultyBand band)
        {
            isSetupDone = true;
            difficultyBand = band;
            gameTitle = "Memory Match Arcade";

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

            switch (band)
            {
                case DifficultyBand.Easy:
                    totalPairs = 6;  // 12 cards (4x3)
                    timeRemaining = 100f;
                    break;
                case DifficultyBand.Medium:
                    totalPairs = 8;  // 16 cards (4x4)
                    timeRemaining = 90f;
                    break;
                case DifficultyBand.Hard:
                    totalPairs = 8;  // 16 cards (4x4)
                    timeRemaining = 65f;
                    break;
            }

            if (titleText != null) titleText.text = "Financial Memory Match";
            if (subTitleText != null) subTitleText.text = $"Flip cards to match {totalPairs} financial pairs! ({DifficultyHelper.GetLabel(band)})";
        }

        public override void StartGame()
        {
            isGameActive = true;
            isCompleted = false;
            movesCount = 0;
            pairsMatched = 0;
            firstSelected = null;
            secondSelected = null;
            isCheckingMatch = false;

            if (feedbackText != null) feedbackText.text = "Click any card to flip it face up!";
            BuildCardsGrid();
            UpdateStatsUI();
        }

        private void BuildCardsGrid()
        {
            if (cardsGridContainer == null) return;

            foreach (Transform child in cardsGridContainer)
            {
                Destroy(child.gameObject);
            }
            cards.Clear();

            // Pick totalPairs unique symbols from ALL_SYMBOLS
            List<CardData> available = new List<CardData>(ALL_SYMBOLS);
            List<CardData> chosenSymbols = new List<CardData>();
            for (int i = 0; i < totalPairs; i++)
            {
                int r = UnityEngine.Random.Range(0, available.Count);
                chosenSymbols.Add(available[r]);
                available.RemoveAt(r);
            }

            // Create deck of pairs
            List<int> deckPairs = new List<int>();
            for (int i = 0; i < totalPairs; i++)
            {
                deckPairs.Add(i);
                deckPairs.Add(i);
            }

            // Shuffle deck thoroughly
            for (int i = 0; i < deckPairs.Count; i++)
            {
                int r = UnityEngine.Random.Range(i, deckPairs.Count);
                int tmp = deckPairs[i];
                deckPairs[i] = deckPairs[r];
                deckPairs[r] = tmp;
            }

            // Configure Grid Layout Group
            GridLayoutGroup glg = cardsGridContainer.GetComponent<GridLayoutGroup>();
            if (glg == null) glg = cardsGridContainer.gameObject.AddComponent<GridLayoutGroup>();
            glg.spacing = new Vector2(10f, 10f);
            glg.childAlignment = TextAnchor.MiddleCenter;

            if (deckPairs.Count <= 12)
            {
                glg.cellSize = new Vector2(100f, 110f);
                glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                glg.constraintCount = 4;
            }
            else
            {
                glg.cellSize = new Vector2(92f, 100f);
                glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                glg.constraintCount = 4;
            }

            for (int i = 0; i < deckPairs.Count; i++)
            {
                int pairIdx = deckPairs[i];
                CardData data = chosenSymbols[pairIdx];

                GameObject cardObj = new GameObject($"Card_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                cardObj.transform.SetParent(cardsGridContainer, false);

                Image bg = cardObj.GetComponent<Image>();
                bg.color = new Color32(0x13, 0x2A, 0x4A, 0xFF);

                // Symbol Text
                GameObject symObj = new GameObject("SymbolText", typeof(RectTransform), typeof(TextMeshProUGUI));
                symObj.transform.SetParent(cardObj.transform, false);
                RectTransform symRt = symObj.GetComponent<RectTransform>();
                symRt.anchorMin = new Vector2(0.05f, 0.35f);
                symRt.anchorMax = new Vector2(0.95f, 0.90f);
                symRt.sizeDelta = Vector2.zero;
                TextMeshProUGUI symTxt = symObj.GetComponent<TextMeshProUGUI>();
                symTxt.text = "?";
                symTxt.fontSize = 24;
                symTxt.fontStyle = FontStyles.Bold;
                symTxt.color = new Color32(0x4F, 0xC3, 0xF7, 0xFF);
                symTxt.alignment = TextAlignmentOptions.Center;

                // Title Text
                GameObject titObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
                titObj.transform.SetParent(cardObj.transform, false);
                RectTransform titRt = titObj.GetComponent<RectTransform>();
                titRt.anchorMin = new Vector2(0.05f, 0.05f);
                titRt.anchorMax = new Vector2(0.95f, 0.35f);
                titRt.sizeDelta = Vector2.zero;
                TextMeshProUGUI titTxt = titObj.GetComponent<TextMeshProUGUI>();
                titTxt.text = "FLIP";
                titTxt.fontSize = 11;
                titTxt.fontStyle = FontStyles.Normal;
                titTxt.color = new Color32(0x90, 0xCA, 0xF9, 0xFF);
                titTxt.alignment = TextAlignmentOptions.Center;

                CardInstance inst = new CardInstance
                {
                    cardId = i,
                    pairIndex = pairIdx,
                    data = data,
                    obj = cardObj,
                    button = cardObj.GetComponent<Button>(),
                    bgImage = bg,
                    symbolText = symTxt,
                    titleText = titTxt,
                    isFaceUp = false,
                    isMatched = false
                };

                int capturedId = i;
                inst.button.onClick.AddListener(() => OnCardClicked(capturedId));
                cards.Add(inst);
            }
        }

        private void OnCardClicked(int cardId)
        {
            if (!isGameActive || isCompleted || isCheckingMatch) return;
            if (cardId < 0 || cardId >= cards.Count) return;

            CardInstance card = cards[cardId];
            if (card.isFaceUp || card.isMatched) return;

            // Flip card face up
            FlipCard(card, true);

            if (firstSelected == null)
            {
                firstSelected = card;
            }
            else if (secondSelected == null && card != firstSelected)
            {
                secondSelected = card;
                movesCount++;
                UpdateStatsUI();
                StartCoroutine(CheckMatchRoutine());
            }
        }

        private void FlipCard(CardInstance card, bool faceUp)
        {
            card.isFaceUp = faceUp;
            if (faceUp)
            {
                card.bgImage.color = card.data.themeColor;
                card.symbolText.text = card.data.symbol;
                card.symbolText.color = Color.white;
                card.titleText.text = card.data.title.ToUpper();
                card.titleText.color = new Color32(0xFF, 0xFF, 0xFF, 0xEE);
            }
            else
            {
                card.bgImage.color = new Color32(0x13, 0x2A, 0x4A, 0xFF);
                card.symbolText.text = "?";
                card.symbolText.color = new Color32(0x4F, 0xC3, 0xF7, 0xFF);
                card.titleText.text = "FLIP";
                card.titleText.color = new Color32(0x90, 0xCA, 0xF9, 0xFF);
            }
        }

        private IEnumerator CheckMatchRoutine()
        {
            isCheckingMatch = true;

            if (firstSelected.pairIndex == secondSelected.pairIndex)
            {
                // Match!
                pairsMatched++;
                firstSelected.isMatched = true;
                secondSelected.isMatched = true;

                if (feedbackText != null)
                {
                    feedbackText.text = $"Great Match! \"{firstSelected.data.title}\" unlocked!";
                    feedbackText.color = new Color32(0x00, 0xE6, 0x76, 0xFF);
                }

                // Highlight matched cards in bright green
                firstSelected.bgImage.color = new Color32(0x00, 0xC8, 0x53, 0xFF);
                secondSelected.bgImage.color = new Color32(0x00, 0xC8, 0x53, 0xFF);

                yield return new WaitForSeconds(0.4f);

                firstSelected = null;
                secondSelected = null;
                isCheckingMatch = false;

                if (pairsMatched >= totalPairs)
                {
                    TriggerVictory();
                }
            }
            else
            {
                // Mismatch
                if (feedbackText != null)
                {
                    feedbackText.text = "Try again! Memorize the positions.";
                    feedbackText.color = new Color32(0xFF, 0x52, 0x52, 0xFF);
                }

                yield return new WaitForSeconds(0.7f);

                if (firstSelected != null) FlipCard(firstSelected, false);
                if (secondSelected != null) FlipCard(secondSelected, false);

                firstSelected = null;
                secondSelected = null;
                isCheckingMatch = false;
            }
        }

        private void UpdateStatsUI()
        {
            if (statsText != null)
            {
                statsText.text = $"Pairs: {pairsMatched}/{totalPairs}   |   Moves: {movesCount}   |   Time: {Mathf.CeilToInt(timeRemaining)}s";
            }
        }

        private void OnTimeExpired()
        {
            isGameActive = false;
            if (feedbackText != null)
            {
                feedbackText.text = "Time is up! Re-enter the Arcade to try again!";
                feedbackText.color = new Color32(0xFF, 0x52, 0x52, 0xFF);
            }
        }

        private void TriggerVictory()
        {
            isGameActive = false;
            isCompleted = true;

            if (victoryPanel != null)
            {
                if (victoryMessage != null)
                {
                    victoryMessage.text = $"Outstanding Memory!\nMatched all {totalPairs} financial pairs in {movesCount} moves!\n+500 Savings Reward & Task Completed!";
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
