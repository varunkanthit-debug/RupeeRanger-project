using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using RupeeRanger.Core;
using RupeeRanger.Data;
using RupeeRanger.Result;

namespace RupeeRanger.UI
{
    public class ResultUI : MonoBehaviour
    {
        [Header("Top Navigation Bar")]
        public TMP_Text logoText;
        public Button themeToggleButton;
        public Image themeToggleIcon;
        public Sprite sunIcon;
        public Sprite moonIcon;

        [Header("Hero Header")]
        public Image starIcon;
        public TMP_Text gameOverTitle;
        public TMP_Text congratulationsText;

        [Header("Final Score Card")]
        public TMP_Text scoreLabelText;
        public TMP_Text finalScoreValueText;
        public TMP_Text scoreFormulaText;

        [Header("Character Stage")]
        public Image characterImage;
        public TMP_Text characterNameText;
        public TMP_Text characterEmotionBadgeText;
        public GameObject celebrationAura;
        public RectTransform confettiContainer;

        [Header("AI Financial Advice Section (Prominent)")]
        public TMP_Text adviceSectionTitle;
        public Transform adviceCardsContainer;
        public GameObject adviceCardPrefab;

        [Header("Financial Summary Panel")]
        public TMP_Text summaryTitleText;
        public TMP_Text totalIncomeValueText;
        public TMP_Text totalExpensesValueText;
        public TMP_Text finalSavingsValueText;
        public TMP_Text totalLoanValueText;

        [Header("Player Info Panel")]
        public TMP_Text playerInfoTitleText;
        public TMP_Text playerNameValueText;
        public TMP_Text lifestyleValueText;
        public TMP_Text monthlyIncomeValueText;
        public TMP_Text daysPlayedValueText;
        public TMP_Text randomEventsCountText;

        [Header("Events Encountered Panel")]
        public TMP_Text eventsTitleText;
        public Transform eventsContainer;

        [Header("Action Buttons")]
        public Button playAgainButton;
        public Button mainMenuButton;

        [Header("Celebration & Unlock Modals")]
        public GameObject celebrationModal;
        public TMP_Text celebrationTitleText;
        public TMP_Text celebrationMessageText;
        public Button celebrationCloseButton;

        public GameObject characterUnlockModal;
        public Image unlockedCharacterImage;
        public TMP_Text unlockedCharacterNameText;
        public TMP_Text unlockedCharacterDescText;
        public Button unlockModalCloseButton;

        private int targetFinalScore = 0;
        private Coroutine scoreCountRoutine;
        private Coroutine characterBounceRoutine;

        private void Awake()
        {
            AutoBindElementsIfUnassigned();
        }

        private void Start()
        {
            EnsureValidSessionData();
            SetupTopBar();
            CalculateAndDisplayResults();
            SetupActionButtons();
            CheckCharacterUnlocks();
        }

        private void AutoBindElementsIfUnassigned()
        {
            if (finalScoreValueText != null) return;

            Transform root = transform;

            // Clean up any old stub UI if present
            Transform stub = root.Find("StubCard");
            if (stub != null)
            {
                Destroy(stub.gameObject);
            }

            logoText = root.Find("TopBar/LogoText")?.GetComponent<TMP_Text>();
            themeToggleButton = root.Find("TopBar/ThemeToggleBtn")?.GetComponent<Button>();
            themeToggleIcon = root.Find("TopBar/ThemeToggleBtn/Icon")?.GetComponent<Image>();

            starIcon = root.Find("ScrollView/Viewport/Content/HeroSection/StarIcon")?.GetComponent<Image>();
            gameOverTitle = root.Find("ScrollView/Viewport/Content/HeroSection/GameOverTitle")?.GetComponent<TMP_Text>();
            congratulationsText = root.Find("ScrollView/Viewport/Content/HeroSection/Congratulations")?.GetComponent<TMP_Text>();

            scoreLabelText = root.Find("ScrollView/Viewport/Content/FinalScoreCard/ScoreLabel")?.GetComponent<TMP_Text>();
            finalScoreValueText = root.Find("ScrollView/Viewport/Content/FinalScoreCard/ScoreValue")?.GetComponent<TMP_Text>();
            scoreFormulaText = root.Find("ScrollView/Viewport/Content/FinalScoreCard/Formula")?.GetComponent<TMP_Text>();

            characterImage = root.Find("ScrollView/Viewport/Content/CharacterStage/CharacterPortrait")?.GetComponent<Image>();
            characterNameText = root.Find("ScrollView/Viewport/Content/CharacterStage/CharName")?.GetComponent<TMP_Text>();
            characterEmotionBadgeText = root.Find("ScrollView/Viewport/Content/CharacterStage/EmotionBadge")?.GetComponent<TMP_Text>();
            celebrationAura = root.Find("ScrollView/Viewport/Content/CharacterStage/AuraGlow")?.gameObject;
            confettiContainer = root.Find("ConfettiContainer")?.GetComponent<RectTransform>();

            adviceSectionTitle = root.Find("ScrollView/Viewport/Content/AIAdviceSection/AdviceTitle")?.GetComponent<TMP_Text>();
            adviceCardsContainer = root.Find("ScrollView/Viewport/Content/AIAdviceSection/AdviceCardsContainer");

            summaryTitleText = root.Find("ScrollView/Viewport/Content/SummaryRow/FinancialSummaryCard/Title")?.GetComponent<TMP_Text>();
            totalIncomeValueText = root.Find("ScrollView/Viewport/Content/SummaryRow/FinancialSummaryCard/IncomeVal")?.GetComponent<TMP_Text>();
            totalExpensesValueText = root.Find("ScrollView/Viewport/Content/SummaryRow/FinancialSummaryCard/ExpVal")?.GetComponent<TMP_Text>();
            finalSavingsValueText = root.Find("ScrollView/Viewport/Content/SummaryRow/FinancialSummaryCard/SavVal")?.GetComponent<TMP_Text>();
            totalLoanValueText = root.Find("ScrollView/Viewport/Content/SummaryRow/FinancialSummaryCard/LoanVal")?.GetComponent<TMP_Text>();

            playerInfoTitleText = root.Find("ScrollView/Viewport/Content/SummaryRow/PlayerInfoCard/Title")?.GetComponent<TMP_Text>();
            playerNameValueText = root.Find("ScrollView/Viewport/Content/SummaryRow/PlayerInfoCard/NameVal")?.GetComponent<TMP_Text>();
            lifestyleValueText = root.Find("ScrollView/Viewport/Content/SummaryRow/PlayerInfoCard/LifeVal")?.GetComponent<TMP_Text>();
            monthlyIncomeValueText = root.Find("ScrollView/Viewport/Content/SummaryRow/PlayerInfoCard/IncVal")?.GetComponent<TMP_Text>();
            daysPlayedValueText = root.Find("ScrollView/Viewport/Content/SummaryRow/PlayerInfoCard/DaysVal")?.GetComponent<TMP_Text>();
            randomEventsCountText = root.Find("ScrollView/Viewport/Content/SummaryRow/PlayerInfoCard/EvVal")?.GetComponent<TMP_Text>();

            eventsTitleText = root.Find("ScrollView/Viewport/Content/EventsEncounteredCard/EventsTitle")?.GetComponent<TMP_Text>();
            eventsContainer = root.Find("ScrollView/Viewport/Content/EventsEncounteredCard/EventsGrid");

            playAgainButton = root.Find("ScrollView/Viewport/Content/ActionButtonsRow/PlayAgainButton")?.GetComponent<Button>();
            mainMenuButton = root.Find("ScrollView/Viewport/Content/ActionButtonsRow/MainMenuButton")?.GetComponent<Button>();

            celebrationModal = root.Find("CelebrationModal")?.gameObject;
            celebrationTitleText = root.Find("CelebrationModal/Card/Title")?.GetComponent<TMP_Text>();
            celebrationMessageText = root.Find("CelebrationModal/Card/Message")?.GetComponent<TMP_Text>();
            celebrationCloseButton = root.Find("CelebrationModal/Card/CloseButton")?.GetComponent<Button>();

            characterUnlockModal = root.Find("CharacterUnlockModal")?.gameObject;
            unlockedCharacterImage = root.Find("CharacterUnlockModal/Card/UnlockedCharacterImage")?.GetComponent<Image>();
            unlockedCharacterNameText = root.Find("CharacterUnlockModal/Card/Name")?.GetComponent<TMP_Text>();
            unlockedCharacterDescText = root.Find("CharacterUnlockModal/Card/Desc")?.GetComponent<TMP_Text>();
            unlockModalCloseButton = root.Find("CharacterUnlockModal/Card/ClaimButton")?.GetComponent<Button>();

            if (finalScoreValueText == null)
            {
                BuildRuntimeUIHierarchy();
            }
        }

        private void BuildRuntimeUIHierarchy()
        {
            Transform root = transform;

            // Remove any old placeholder if present
            Transform oldStub = root.Find("StubCard");
            if (oldStub != null) Destroy(oldStub.gameObject);

            // Background
            Transform existingBg = root.Find("Background");
            if (existingBg == null)
            {
                Image bg = CreateImage("Background", root, null, new Color32(0x0C, 0x10, 0x1F, 0xFF));
                SetStretch(bg.rectTransform);
            }

            // Top Bar
            GameObject topBar = CreateUIObject("TopBar", root);
            RectTransform topBarRt = topBar.GetComponent<RectTransform>();
            topBarRt.anchorMin = new Vector2(0, 1);
            topBarRt.anchorMax = new Vector2(1, 1);
            topBarRt.pivot = new Vector2(0.5f, 1);
            topBarRt.sizeDelta = new Vector2(0, 64);
            topBarRt.anchoredPosition = Vector2.zero;

            logoText = CreateText("LogoText", topBar.transform, "<color=#FF4081>₹</color> RupeeRanger", 26, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            SetPositionAndSize(logoText.rectTransform, new Vector2(-460, 0), new Vector2(280, 50));

            GameObject themeBtnObj = CreateUIObject("ThemeToggleBtn", topBar.transform);
            Image themeBtnBg = themeBtnObj.AddComponent<Image>();
            themeBtnBg.color = new Color32(0x1F, 0x27, 0x47, 0xFF);
            SetPositionAndSize(themeBtnObj.GetComponent<RectTransform>(), new Vector2(460, 0), new Vector2(48, 48));
            themeToggleButton = themeBtnObj.AddComponent<Button>();

            themeToggleIcon = CreateImage("Icon", themeBtnObj.transform, sunIcon, Color.white);
            SetPositionAndSize(themeToggleIcon.rectTransform, Vector2.zero, new Vector2(28, 28));

            // ScrollView Container
            GameObject scrollView = CreateUIObject("ScrollView", root);
            RectTransform scrollRt = scrollView.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(0, 0);
            scrollRt.offsetMax = new Vector2(0, -64);

            ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewport = CreateUIObject("Viewport", scrollView.transform);
            RectTransform vpRt = viewport.GetComponent<RectTransform>();
            SetStretch(vpRt);
            viewport.AddComponent<RectMask2D>();

            GameObject content = CreateUIObject("Content", viewport.transform);
            RectTransform contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 1);
            contentRt.anchorMax = new Vector2(0.5f, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.sizeDelta = new Vector2(1040, 1600);
            contentRt.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(30, 30, 20, 50);
            vlg.spacing = 22;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRt;
            scroll.content = contentRt;

            // 1. Hero
            GameObject heroObj = CreateUIObject("HeroSection", content.transform);
            heroObj.AddComponent<LayoutElement>().preferredHeight = 150;
            starIcon = CreateImage("StarIcon", heroObj.transform, null, new Color32(0xFF, 0xD7, 0x00, 0xFF));
            SetPositionAndSize(starIcon.rectTransform, new Vector2(0, 45), new Vector2(48, 48));

            gameOverTitle = CreateText("GameOverTitle", heroObj.transform, "Game Over!", 44, FontStyles.Bold, TextAlignmentOptions.Center);
            gameOverTitle.color = Color.white;
            SetPositionAndSize(gameOverTitle.rectTransform, new Vector2(0, -2), new Vector2(980, 50));

            congratulationsText = CreateText("Congratulations", heroObj.transform, "Congratulations Adventurer! You completed 30 days of financial survival.", 20, FontStyles.Normal, TextAlignmentOptions.Center);
            congratulationsText.color = new Color32(0x94, 0xA3, 0xB8, 0xFF);
            SetPositionAndSize(congratulationsText.rectTransform, new Vector2(0, -42), new Vector2(980, 36));

            // 2. Final Score Card
            GameObject scoreCard = CreateUIObject("FinalScoreCard", content.transform);
            scoreCard.AddComponent<LayoutElement>().preferredHeight = 160;
            Image scoreCardBg = scoreCard.AddComponent<Image>();
            scoreCardBg.color = new Color32(0x15, 0x1B, 0x33, 0xF0);

            scoreLabelText = CreateText("ScoreLabel", scoreCard.transform, "Your Final Score", 17, FontStyles.Bold, TextAlignmentOptions.Center);
            scoreLabelText.color = new Color32(0x81, 0x8C, 0xF8, 0xFF);
            SetPositionAndSize(scoreLabelText.rectTransform, new Vector2(0, 46), new Vector2(900, 24));

            finalScoreValueText = CreateText("ScoreValue", scoreCard.transform, "0", 62, FontStyles.Bold, TextAlignmentOptions.Center);
            finalScoreValueText.color = new Color32(0xFA, 0xCC, 0x15, 0xFF);
            SetPositionAndSize(finalScoreValueText.rectTransform, new Vector2(0, 0), new Vector2(900, 68));

            scoreFormulaText = CreateText("Formula", scoreCard.transform, "Score = (Savings × 2) - (Loan × 1.5)", 16, FontStyles.Normal, TextAlignmentOptions.Center);
            scoreFormulaText.color = new Color32(0x94, 0xA3, 0xB8, 0xFF);
            SetPositionAndSize(scoreFormulaText.rectTransform, new Vector2(0, -48), new Vector2(900, 24));

            // 3. Character Stage
            GameObject charStage = CreateUIObject("CharacterStage", content.transform);
            charStage.AddComponent<LayoutElement>().preferredHeight = 150;
            Image charStageBg = charStage.AddComponent<Image>();
            charStageBg.color = new Color32(0x17, 0x1F, 0x3D, 0xE0);

            characterImage = CreateImage("CharacterPortrait", charStage.transform, null, Color.white);
            SetPositionAndSize(characterImage.rectTransform, new Vector2(-360, 0), new Vector2(110, 110));

            celebrationAura = CreateUIObject("AuraGlow", charStage.transform);
            Image auraImg = celebrationAura.AddComponent<Image>();
            auraImg.color = new Color32(0xFF, 0xD7, 0x00, 0x25);
            SetPositionAndSize(celebrationAura.GetComponent<RectTransform>(), new Vector2(-360, 0), new Vector2(130, 130));

            characterNameText = CreateText("CharName", charStage.transform, "Penny Ranger", 24, FontStyles.Bold, TextAlignmentOptions.Left);
            characterNameText.color = Color.white;
            SetPositionAndSize(characterNameText.rectTransform, new Vector2(60, 25), new Vector2(650, 32));

            characterEmotionBadgeText = CreateText("EmotionBadge", charStage.transform, "🌟 OUTSTANDING FINANCIAL RANGER!", 18, FontStyles.Bold, TextAlignmentOptions.Left);
            characterEmotionBadgeText.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(characterEmotionBadgeText.rectTransform, new Vector2(60, -15), new Vector2(650, 30));

            GameObject confettiObj = CreateUIObject("ConfettiContainer", root);
            confettiContainer = confettiObj.GetComponent<RectTransform>();
            SetStretch(confettiContainer);

            // 4. AI Advice Section (Prominent at top)
            GameObject adviceCard = CreateUIObject("AIAdviceSection", content.transform);
            Image adviceBg = adviceCard.AddComponent<Image>();
            adviceBg.color = new Color32(0x15, 0x1B, 0x33, 0xF0);

            VerticalLayoutGroup adviceVlg = adviceCard.AddComponent<VerticalLayoutGroup>();
            adviceVlg.padding = new RectOffset(24, 24, 20, 24);
            adviceVlg.spacing = 14;
            adviceVlg.childAlignment = TextAnchor.UpperLeft;
            adviceVlg.childControlWidth = true;
            adviceVlg.childControlHeight = false;
            adviceVlg.childForceExpandWidth = false;
            adviceVlg.childForceExpandHeight = false;

            ContentSizeFitter adviceCsf = adviceCard.AddComponent<ContentSizeFitter>();
            adviceCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            adviceSectionTitle = CreateText("AdviceTitle", adviceCard.transform, "🤖 AI Financial Advice", 23, FontStyles.Bold, TextAlignmentOptions.Left);
            adviceSectionTitle.color = Color.white;
            adviceSectionTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 36;

            GameObject adviceCardsBox = CreateUIObject("AdviceCardsContainer", adviceCard.transform);
            VerticalLayoutGroup acVlg = adviceCardsBox.AddComponent<VerticalLayoutGroup>();
            acVlg.spacing = 12;
            acVlg.childControlWidth = true;
            acVlg.childControlHeight = false;
            acVlg.childForceExpandWidth = false;
            acVlg.childForceExpandHeight = false;

            ContentSizeFitter acCsf = adviceCardsBox.AddComponent<ContentSizeFitter>();
            acCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            adviceCardsContainer = adviceCardsBox.transform;

            // 5. Summary Row
            GameObject summaryRow = CreateUIObject("SummaryRow", content.transform);
            summaryRow.AddComponent<LayoutElement>().preferredHeight = 250;

            HorizontalLayoutGroup rowHlg = summaryRow.AddComponent<HorizontalLayoutGroup>();
            rowHlg.spacing = 20;
            rowHlg.childAlignment = TextAnchor.MiddleCenter;
            rowHlg.childControlWidth = true;
            rowHlg.childControlHeight = true;
            rowHlg.childForceExpandWidth = true;
            rowHlg.childForceExpandHeight = true;

            // 5A Financial Summary Card
            GameObject finSummaryCard = CreateUIObject("FinancialSummaryCard", summaryRow.transform);
            Image finBg = finSummaryCard.AddComponent<Image>();
            finBg.color = new Color32(0x15, 0x1B, 0x33, 0xF0);

            summaryTitleText = CreateText("Title", finSummaryCard.transform, "Financial Summary", 21, FontStyles.Bold, TextAlignmentOptions.Left);
            summaryTitleText.color = Color.white;
            SetPositionAndSize(summaryTitleText.rectTransform, new Vector2(0, 85), new Vector2(410, 30));

            CreateText("IncomeLabel", finSummaryCard.transform, "Total Income (30 days)", 16, FontStyles.Normal, TextAlignmentOptions.Left).color = new Color32(0x94, 0xA3, 0xB8, 0xFF);
            SetPositionAndSize(finSummaryCard.transform.Find("IncomeLabel").GetComponent<RectTransform>(), new Vector2(-95, 45), new Vector2(220, 26));
            totalIncomeValueText = CreateText("IncomeVal", finSummaryCard.transform, "₹450,000", 17, FontStyles.Bold, TextAlignmentOptions.Right);
            totalIncomeValueText.color = new Color32(0x00, 0xF0, 0xA0, 0xFF);
            SetPositionAndSize(totalIncomeValueText.rectTransform, new Vector2(105, 45), new Vector2(200, 26));

            CreateText("ExpLabel", finSummaryCard.transform, "Total Expenses (30 days)", 16, FontStyles.Normal, TextAlignmentOptions.Left).color = new Color32(0x94, 0xA3, 0xB8, 0xFF);
            SetPositionAndSize(finSummaryCard.transform.Find("ExpLabel").GetComponent<RectTransform>(), new Vector2(-95, 10), new Vector2(220, 26));
            totalExpensesValueText = CreateText("ExpVal", finSummaryCard.transform, "-₹195,000", 17, FontStyles.Bold, TextAlignmentOptions.Right);
            totalExpensesValueText.color = new Color32(0xFF, 0x4E, 0x78, 0xFF);
            SetPositionAndSize(totalExpensesValueText.rectTransform, new Vector2(105, 10), new Vector2(200, 26));

            CreateText("SavLabel", finSummaryCard.transform, "Final Savings", 17, FontStyles.Bold, TextAlignmentOptions.Left).color = Color.white;
            SetPositionAndSize(finSummaryCard.transform.Find("SavLabel").GetComponent<RectTransform>(), new Vector2(-95, -30), new Vector2(220, 26));
            finalSavingsValueText = CreateText("SavVal", finSummaryCard.transform, "₹8,716.67", 19, FontStyles.Bold, TextAlignmentOptions.Right);
            finalSavingsValueText.color = new Color32(0x00, 0xF0, 0xA0, 0xFF);
            SetPositionAndSize(finalSavingsValueText.rectTransform, new Vector2(105, -30), new Vector2(200, 26));

            CreateText("LoanLabel", finSummaryCard.transform, "Total Loan Taken", 16, FontStyles.Normal, TextAlignmentOptions.Left).color = new Color32(0x94, 0xA3, 0xB8, 0xFF);
            SetPositionAndSize(finSummaryCard.transform.Find("LoanLabel").GetComponent<RectTransform>(), new Vector2(-95, -68), new Vector2(220, 26));
            totalLoanValueText = CreateText("LoanVal", finSummaryCard.transform, "₹0", 17, FontStyles.Bold, TextAlignmentOptions.Right);
            totalLoanValueText.color = new Color32(0xFF, 0xC1, 0x07, 0xFF);
            SetPositionAndSize(totalLoanValueText.rectTransform, new Vector2(105, -68), new Vector2(200, 26));

            // 5B Player Info Card
            GameObject playerInfoCard = CreateUIObject("PlayerInfoCard", summaryRow.transform);
            Image piBg = playerInfoCard.AddComponent<Image>();
            piBg.color = new Color32(0x15, 0x1B, 0x33, 0xF0);

            playerInfoTitleText = CreateText("Title", playerInfoCard.transform, "Player Info", 21, FontStyles.Bold, TextAlignmentOptions.Left);
            playerInfoTitleText.color = Color.white;
            SetPositionAndSize(playerInfoTitleText.rectTransform, new Vector2(0, 85), new Vector2(410, 30));

            CreateText("NameLabel", playerInfoCard.transform, "Player Name", 16, FontStyles.Normal, TextAlignmentOptions.Left).color = new Color32(0x94, 0xA3, 0xB8, 0xFF);
            SetPositionAndSize(playerInfoCard.transform.Find("NameLabel").GetComponent<RectTransform>(), new Vector2(-95, 52), new Vector2(220, 24));
            playerNameValueText = CreateText("NameVal", playerInfoCard.transform, "dds", 17, FontStyles.Bold, TextAlignmentOptions.Right);
            playerNameValueText.color = Color.white;
            SetPositionAndSize(playerNameValueText.rectTransform, new Vector2(105, 52), new Vector2(200, 24));

            CreateText("LifeLabel", playerInfoCard.transform, "Lifestyle", 16, FontStyles.Normal, TextAlignmentOptions.Left).color = new Color32(0x94, 0xA3, 0xB8, 0xFF);
            SetPositionAndSize(playerInfoCard.transform.Find("LifeLabel").GetComponent<RectTransform>(), new Vector2(-95, 22), new Vector2(220, 24));
            lifestyleValueText = CreateText("LifeVal", playerInfoCard.transform, "Middle", 17, FontStyles.Bold, TextAlignmentOptions.Right);
            lifestyleValueText.color = new Color32(0xFF, 0x4E, 0x78, 0xFF);
            SetPositionAndSize(lifestyleValueText.rectTransform, new Vector2(105, 22), new Vector2(200, 24));

            CreateText("IncLabel", playerInfoCard.transform, "Monthly Income", 16, FontStyles.Normal, TextAlignmentOptions.Left).color = new Color32(0x94, 0xA3, 0xB8, 0xFF);
            SetPositionAndSize(playerInfoCard.transform.Find("IncLabel").GetComponent<RectTransform>(), new Vector2(-95, -8), new Vector2(220, 24));
            monthlyIncomeValueText = CreateText("IncVal", playerInfoCard.transform, "₹15,000", 17, FontStyles.Bold, TextAlignmentOptions.Right);
            monthlyIncomeValueText.color = new Color32(0x00, 0xF0, 0xA0, 0xFF);
            SetPositionAndSize(monthlyIncomeValueText.rectTransform, new Vector2(105, -8), new Vector2(200, 24));

            CreateText("DaysLabel", playerInfoCard.transform, "Days Played", 16, FontStyles.Normal, TextAlignmentOptions.Left).color = new Color32(0x94, 0xA3, 0xB8, 0xFF);
            SetPositionAndSize(playerInfoCard.transform.Find("DaysLabel").GetComponent<RectTransform>(), new Vector2(-95, -38), new Vector2(220, 24));
            daysPlayedValueText = CreateText("DaysVal", playerInfoCard.transform, "30 days", 17, FontStyles.Bold, TextAlignmentOptions.Right);
            daysPlayedValueText.color = Color.white;
            SetPositionAndSize(daysPlayedValueText.rectTransform, new Vector2(105, -38), new Vector2(200, 24));

            CreateText("EvLabel", playerInfoCard.transform, "Random Events", 16, FontStyles.Normal, TextAlignmentOptions.Left).color = new Color32(0x94, 0xA3, 0xB8, 0xFF);
            SetPositionAndSize(playerInfoCard.transform.Find("EvLabel").GetComponent<RectTransform>(), new Vector2(-95, -68), new Vector2(220, 24));
            randomEventsCountText = CreateText("EvVal", playerInfoCard.transform, "7", 17, FontStyles.Bold, TextAlignmentOptions.Right);
            randomEventsCountText.color = new Color32(0xFF, 0xC1, 0x07, 0xFF);
            SetPositionAndSize(randomEventsCountText.rectTransform, new Vector2(105, -68), new Vector2(200, 24));

            // 6. Events Encountered Card (No duplicates)
            GameObject eventsCard = CreateUIObject("EventsEncounteredCard", content.transform);
            Image evBg = eventsCard.AddComponent<Image>();
            evBg.color = new Color32(0x15, 0x1B, 0x33, 0xF0);

            VerticalLayoutGroup evVlg = eventsCard.AddComponent<VerticalLayoutGroup>();
            evVlg.padding = new RectOffset(24, 24, 20, 24);
            evVlg.spacing = 16;
            evVlg.childAlignment = TextAnchor.UpperLeft;
            evVlg.childControlWidth = true;
            evVlg.childControlHeight = false;
            evVlg.childForceExpandWidth = false;
            evVlg.childForceExpandHeight = false;

            ContentSizeFitter evCsf = eventsCard.AddComponent<ContentSizeFitter>();
            evCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            eventsTitleText = CreateText("EventsTitle", eventsCard.transform, "Events Encountered", 22, FontStyles.Bold, TextAlignmentOptions.Left);
            eventsTitleText.color = Color.white;
            eventsTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 32;

            GameObject evGrid = CreateUIObject("EventsGrid", eventsCard.transform);
            GridLayoutGroup glg = evGrid.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(435, 46);
            glg.spacing = new Vector2(16, 12);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 2;

            ContentSizeFitter gridCsf = evGrid.AddComponent<ContentSizeFitter>();
            gridCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            eventsContainer = evGrid.transform;

            // 7. Action Buttons
            GameObject btnRow = CreateUIObject("ActionButtonsRow", content.transform);
            btnRow.AddComponent<LayoutElement>().preferredHeight = 75;

            HorizontalLayoutGroup btnHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            btnHlg.spacing = 30;
            btnHlg.childAlignment = TextAnchor.MiddleCenter;
            btnHlg.childControlWidth = false;
            btnHlg.childControlHeight = false;
            btnHlg.childForceExpandWidth = false;
            btnHlg.childForceExpandHeight = false;

            GameObject playAgainObj = CreateUIObject("PlayAgainButton", btnRow.transform);
            Image paBg = playAgainObj.AddComponent<Image>();
            paBg.color = new Color32(0x00, 0xC8, 0x53, 0xFF);
            SetPositionAndSize(playAgainObj.GetComponent<RectTransform>(), Vector2.zero, new Vector2(280, 58));

            playAgainButton = playAgainObj.AddComponent<Button>();
            TextMeshProUGUI paTxt = CreateText("Text", playAgainObj.transform, "↺ Play Again", 22, FontStyles.Bold, TextAlignmentOptions.Center);
            paTxt.color = Color.white;
            SetStretch(paTxt.rectTransform);

            GameObject menuObj = CreateUIObject("MainMenuButton", btnRow.transform);
            Image menuBg = menuObj.AddComponent<Image>();
            menuBg.color = new Color32(0x4F, 0x46, 0xE5, 0xFF);
            SetPositionAndSize(menuObj.GetComponent<RectTransform>(), Vector2.zero, new Vector2(280, 58));

            mainMenuButton = menuObj.AddComponent<Button>();
            TextMeshProUGUI menuTxt = CreateText("Text", menuObj.transform, "🏠 Main Menu", 22, FontStyles.Bold, TextAlignmentOptions.Center);
            menuTxt.color = Color.white;
            SetStretch(menuTxt.rectTransform);

            // 8. Celebration Modal
            celebrationModal = CreateUIObject("CelebrationModal", root);
            Image celebBg = celebrationModal.AddComponent<Image>();
            celebBg.color = new Color32(0x0A, 0x0E, 0x1A, 0xDD);
            SetStretch(celebrationModal.GetComponent<RectTransform>());

            GameObject celebCard = CreateUIObject("Card", celebrationModal.transform);
            Image cCardBg = celebCard.AddComponent<Image>();
            cCardBg.color = new Color32(0x1B, 0x24, 0x47, 0xFF);
            SetPositionAndSize(celebCard.GetComponent<RectTransform>(), Vector2.zero, new Vector2(580, 320));

            celebrationTitleText = CreateText("Title", celebCard.transform, "🎉 Outstanding 30-Day Survival!", 26, FontStyles.Bold, TextAlignmentOptions.Center);
            celebrationTitleText.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(celebrationTitleText.rectTransform, new Vector2(0, 95), new Vector2(520, 45));

            celebrationMessageText = CreateText("Message", celebCard.transform, "Incredible financial discipline! You survived 30 days and unlocked maximum rewards.", 18, FontStyles.Normal, TextAlignmentOptions.Center);
            celebrationMessageText.color = new Color32(0xDC, 0xE7, 0xFA, 0xFF);
            SetPositionAndSize(celebrationMessageText.rectTransform, new Vector2(0, 15), new Vector2(500, 90));

            GameObject cBtnObj = CreateUIObject("CloseButton", celebCard.transform);
            Image cBtnBg = cBtnObj.AddComponent<Image>();
            cBtnBg.color = new Color32(0x00, 0xC8, 0x53, 0xFF);
            SetPositionAndSize(cBtnObj.GetComponent<RectTransform>(), new Vector2(0, -95), new Vector2(220, 50));

            celebrationCloseButton = cBtnObj.AddComponent<Button>();
            TextMeshProUGUI cBtnTxt = CreateText("Text", cBtnObj.transform, "Awesome!", 20, FontStyles.Bold, TextAlignmentOptions.Center);
            cBtnTxt.color = Color.white;
            SetStretch(cBtnTxt.rectTransform);
            celebrationModal.SetActive(false);

            // 9. Character Unlock Modal
            characterUnlockModal = CreateUIObject("CharacterUnlockModal", root);
            Image unBg = characterUnlockModal.AddComponent<Image>();
            unBg.color = new Color32(0x0A, 0x0E, 0x1A, 0xEE);
            SetStretch(characterUnlockModal.GetComponent<RectTransform>());

            GameObject unCard = CreateUIObject("Card", characterUnlockModal.transform);
            Image unCardBg = unCard.AddComponent<Image>();
            unCardBg.color = new Color32(0x22, 0x1C, 0x48, 0xFF);
            SetPositionAndSize(unCard.GetComponent<RectTransform>(), Vector2.zero, new Vector2(560, 400));

            unlockedCharacterImage = CreateImage("UnlockedCharacterImage", unCard.transform, null, Color.white);
            SetPositionAndSize(unlockedCharacterImage.rectTransform, new Vector2(0, 85), new Vector2(130, 130));

            unlockedCharacterNameText = CreateText("Name", unCard.transform, "🎉 New Character Unlocked!", 24, FontStyles.Bold, TextAlignmentOptions.Center);
            unlockedCharacterNameText.color = new Color32(0xFF, 0xD7, 0x00, 0xFF);
            SetPositionAndSize(unlockedCharacterNameText.rectTransform, new Vector2(0, -10), new Vector2(500, 36));

            unlockedCharacterDescText = CreateText("Desc", unCard.transform, "You have reached the required career score and unlocked a new Ranger!", 17, FontStyles.Normal, TextAlignmentOptions.Center);
            unlockedCharacterDescText.color = new Color32(0xC7, 0xC3, 0xE8, 0xFF);
            SetPositionAndSize(unlockedCharacterDescText.rectTransform, new Vector2(0, -65), new Vector2(480, 60));

            GameObject unBtnObj = CreateUIObject("ClaimButton", unCard.transform);
            Image unBtnBg = unBtnObj.AddComponent<Image>();
            unBtnBg.color = new Color32(0xFF, 0x40, 0x81, 0xFF);
            SetPositionAndSize(unBtnObj.GetComponent<RectTransform>(), new Vector2(0, -145), new Vector2(220, 50));

            unlockModalCloseButton = unBtnObj.AddComponent<Button>();
            TextMeshProUGUI unBtnTxt = CreateText("Text", unBtnObj.transform, "Claim & Continue", 19, FontStyles.Bold, TextAlignmentOptions.Center);
            unBtnTxt.color = Color.white;
            SetStretch(unBtnTxt.rectTransform);
            characterUnlockModal.SetActive(false);
        }

        #region Runtime UI Creation Helpers

        private GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            GameObject obj = CreateUIObject(name, parent);
            Image img = obj.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            return img;
        }

        private TextMeshProUGUI CreateText(string name, Transform parent, string content, float fontSize, FontStyles style, TextAlignmentOptions align)
        {
            GameObject obj = CreateUIObject(name, parent);
            TextMeshProUGUI txt = obj.AddComponent<TextMeshProUGUI>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = align;
            txt.raycastTarget = false;
            return txt;
        }

        private void SetPositionAndSize(RectTransform rt, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }

        private void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        #endregion

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnThemeChanged -= OnThemeChanged;
            }
        }

        /// <summary>
        /// Supplies sensible demo defaults if Result scene is tested directly in Editor.
        /// </summary>
        private void EnsureValidSessionData()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (string.IsNullOrWhiteSpace(gm.playerName) || gm.playerName == "Adventurer")
            {
                gm.playerName = "dds";
            }

            if (gm.monthlyIncome <= 0)
            {
                gm.monthlyIncome = 15000;
                gm.rentExpense = 5000;
                gm.foodExpense = 3500;
                gm.transportExpense = 1500;
                gm.entertainmentExpense = 1500;
                gm.initialSavings = 3500;
            }

            // If tested directly with 0 savings, supply screenshot matching demo values
            if (gm.savings == 0 && gm.currentDay <= 1)
            {
                gm.savings = 8717;
                gm.debt = 0;
                gm.totalLoanTaken = 0;
                gm.totalInterestPaid = 0;
            }

            // Populate sample encountered events if none were recorded during a direct jump
            if (gm.encounteredEvents == null || gm.encounteredEvents.Count == 0)
            {
                gm.RecordEncounteredEvent("Phone screen broken", -2000, "Emergency");
                gm.RecordEncounteredEvent("Viral Fever & Clinic Visit", -1200, "Emergency");
                gm.RecordEncounteredEvent("Hostel TV Short-Circuit Repair", -650, "Emergency");
                gm.RecordEncounteredEvent("Party & Batchmate Birthday", -1000, "Emergency");
                gm.RecordEncounteredEvent("Second-Hand Textbook purchase", -500, "Emergency");
                gm.RecordEncounteredEvent("Freelance Graphic Design Gig", 2500, "Windfall");
                gm.RecordEncounteredEvent("Extra Tutoring Session Bonus", 800, "Windfall");
            }
        }

        private void SetupTopBar()
        {
            if (logoText != null)
            {
                logoText.text = "<color=#FF4081>₹</color> RupeeRanger";
            }

            if (themeToggleButton != null)
            {
                themeToggleButton.onClick.AddListener(() =>
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.ToggleTheme();
                    }
                });
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnThemeChanged += OnThemeChanged;
                UpdateThemeIcons(GameManager.Instance.isDarkMode);
            }
        }

        private void OnThemeChanged(bool isDark)
        {
            UpdateThemeIcons(isDark);
        }

        private void UpdateThemeIcons(bool isDark)
        {
            if (themeToggleIcon != null)
            {
                themeToggleIcon.sprite = isDark ? sunIcon : moonIcon;
            }
        }

        private void CalculateAndDisplayResults()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            // 1. Header
            string name = string.IsNullOrWhiteSpace(gm.playerName) ? "Adventurer" : gm.playerName;
            if (gameOverTitle != null) gameOverTitle.text = "Game Over!";
            if (congratulationsText != null)
            {
                congratulationsText.text = $"Congratulations {name}! You completed 30 days of financial survival.";
            }

            // 2. Final Score Calculation: (Savings * 2) - (Loan * 1.5)
            int savings = gm.savings;
            int debt = gm.debt;
            targetFinalScore = Mathf.RoundToInt((savings * 2f) - (debt * 1.5f));

            if (scoreFormulaText != null)
            {
                scoreFormulaText.text = "Score = (Savings × 2) - (Loan × 1.5)";
            }

            if (finalScoreValueText != null)
            {
                if (scoreCountRoutine != null) StopCoroutine(scoreCountRoutine);
                scoreCountRoutine = StartCoroutine(CountUpScoreRoutine(targetFinalScore));
            }

            // 3. Financial Summary
            int totalUpfrontExpenses = gm.TotalExpenses;
            int totalEventExpenses = gm.totalEventExpenses;
            int totalInterest = gm.totalInterestPaid;
            int totalAllExpenses = totalUpfrontExpenses + totalEventExpenses + totalInterest;

            if (totalIncomeValueText != null)
            {
                // ONE month's income
                totalIncomeValueText.text = $"₹{gm.monthlyIncome:N0}";
            }
            if (totalExpensesValueText != null)
            {
                totalExpensesValueText.text = $"-₹{totalAllExpenses:N0}";
            }
            if (finalSavingsValueText != null)
            {
                string colorHex = savings >= 0 ? "#00F0A0" : "#FF5252";
                finalSavingsValueText.text = $"<color={colorHex}>₹{savings:N0}</color>";
            }
            if (totalLoanValueText != null)
            {
                totalLoanValueText.text = $"₹{gm.totalLoanTaken:N0}";
            }

            // 4. Player Info
            if (playerNameValueText != null) playerNameValueText.text = name;
            if (lifestyleValueText != null) lifestyleValueText.text = gm.selectedLifestyle.ToString();
            if (monthlyIncomeValueText != null) monthlyIncomeValueText.text = $"₹{gm.monthlyIncome:N0}";
            if (daysPlayedValueText != null) daysPlayedValueText.text = "30 days";
            if (randomEventsCountText != null)
            {
                int eventCount = gm.encounteredEvents != null ? gm.encounteredEvents.Count : 0;
                randomEventsCountText.text = eventCount.ToString();
            }

            // 5. Character Emotion & Particles
            SetupCharacterPresentation(targetFinalScore, gm.selectedLifestyle);

            // 6. AdviceEngine (Prominent top section)
            PopulateAdviceCards();

            // 7. Events Encountered List (no duplicates)
            PopulateEventsEncounteredList();
        }

        private IEnumerator CountUpScoreRoutine(int targetScore)
        {
            float duration = 1.6f;
            float elapsed = 0f;
            finalScoreValueText.text = "0";

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                // Ease out cubic
                float ease = 1f - Mathf.Pow(1f - progress, 3f);
                int current = Mathf.RoundToInt(Mathf.Lerp(0, targetScore, ease));
                finalScoreValueText.text = $"{current:N0}";
                yield return null;
            }

            finalScoreValueText.text = $"{targetScore:N0}";
        }

        private void SetupCharacterPresentation(int score, LifestyleType lifestyle)
        {
            var gm = GameManager.Instance;
            if (characterImage != null && gm != null && gm.selectedCharacter != null && gm.selectedCharacter.sprite != null)
            {
                characterImage.sprite = gm.selectedCharacter.sprite;
            }

            if (characterNameText != null && gm != null && gm.selectedCharacter != null)
            {
                characterNameText.text = gm.selectedCharacter.characterName;
            }

            GetScoreThresholds(lifestyle, out int lowThreshold, out int highThreshold);

            if (score >= highThreshold)
            {
                // HAPPY STATE
                if (characterEmotionBadgeText != null)
                {
                    characterEmotionBadgeText.text = "🌟 OUTSTANDING FINANCIAL RANGER!";
                    characterEmotionBadgeText.color = new Color32(255, 215, 0, 255);
                }
                if (celebrationAura != null) celebrationAura.SetActive(true);

                // Confetti particles
                StartCoroutine(SpawnConfettiRoutine());

                // Cheerful bouncing animation
                if (characterBounceRoutine != null) StopCoroutine(characterBounceRoutine);
                characterBounceRoutine = StartCoroutine(CharacterHappyBounceRoutine());

                // Show High Score Celebration Popup
                if (celebrationModal != null)
                {
                    if (celebrationTitleText != null) celebrationTitleText.text = "🎉 Outstanding 30-Day Survival!";
                    if (celebrationMessageText != null)
                    {
                        celebrationMessageText.text = $"Incredible job, {gm.playerName}! You finished with ₹{gm.savings:N0} in savings and achieved a stellar score of {score:N0}!";
                    }
                    celebrationModal.SetActive(true);
                }
            }
            else if (score < lowThreshold)
            {
                // SAD STATE
                if (characterEmotionBadgeText != null)
                {
                    characterEmotionBadgeText.text = "🌧️ BUDGET DEFICIT — NEEDS IMPROVEMENT";
                    characterEmotionBadgeText.color = new Color32(255, 107, 107, 255);
                }
                if (celebrationAura != null) celebrationAura.SetActive(false);

                if (characterBounceRoutine != null) StopCoroutine(characterBounceRoutine);
                characterBounceRoutine = StartCoroutine(CharacterSadSighRoutine());
            }
            else
            {
                // NEUTRAL STATE
                if (characterEmotionBadgeText != null)
                {
                    characterEmotionBadgeText.text = "⚖️ STABLE FINANCIAL SURVIVOR";
                    characterEmotionBadgeText.color = new Color32(100, 255, 218, 255);
                }
                if (celebrationAura != null) celebrationAura.SetActive(false);

                if (characterBounceRoutine != null) StopCoroutine(characterBounceRoutine);
                characterBounceRoutine = StartCoroutine(CharacterNeutralIdleRoutine());
            }
        }

        private void GetScoreThresholds(LifestyleType lifestyle, out int lowThreshold, out int highThreshold)
        {
            switch (lifestyle)
            {
                case LifestyleType.Basic:
                    lowThreshold = 4000;
                    highThreshold = 9000;
                    break;
                case LifestyleType.Middle:
                    lowThreshold = 8000;
                    highThreshold = 15000;
                    break;
                case LifestyleType.High:
                    lowThreshold = 14000;
                    highThreshold = 25000;
                    break;
                default:
                    lowThreshold = 8000;
                    highThreshold = 15000;
                    break;
            }
        }

        private IEnumerator CharacterHappyBounceRoutine()
        {
            if (characterImage == null) yield break;
            RectTransform rt = characterImage.rectTransform;
            Vector2 origin = rt.anchoredPosition;

            while (true)
            {
                float time = Time.time * 4f;
                float yOffset = Mathf.Abs(Mathf.Sin(time)) * 22f;
                float squish = 1f + (Mathf.Sin(time * 2f) * 0.06f);
                rt.anchoredPosition = new Vector2(origin.x, origin.y + yOffset);
                rt.localScale = new Vector3(2f - squish, squish, 1f);
                yield return null;
            }
        }

        private IEnumerator CharacterSadSighRoutine()
        {
            if (characterImage == null) yield break;
            RectTransform rt = characterImage.rectTransform;
            Vector2 origin = rt.anchoredPosition;
            characterImage.color = new Color32(180, 200, 255, 255); // slight cool gloom tint

            while (true)
            {
                float time = Time.time * 1.5f;
                float yOffset = Mathf.Sin(time) * 4f - 6f; // lower posture
                rt.anchoredPosition = new Vector2(origin.x, origin.y + yOffset);
                yield return null;
            }
        }

        private IEnumerator CharacterNeutralIdleRoutine()
        {
            if (characterImage == null) yield break;
            RectTransform rt = characterImage.rectTransform;
            Vector2 origin = rt.anchoredPosition;

            while (true)
            {
                float time = Time.time * 2f;
                float yOffset = Mathf.Sin(time) * 5f;
                rt.anchoredPosition = new Vector2(origin.x, origin.y + yOffset);
                yield return null;
            }
        }

        private IEnumerator SpawnConfettiRoutine()
        {
            if (confettiContainer == null) yield break;

            Color32[] confettiColors = new Color32[]
            {
                new Color32(255, 215, 0, 255),  // Gold
                new Color32(0, 240, 160, 255),  // Green
                new Color32(0, 229, 255, 255),  // Cyan
                new Color32(255, 78, 120, 255), // Coral Pink
                new Color32(186, 104, 200, 255) // Purple
            };

            for (int i = 0; i < 45; i++)
            {
                GameObject piece = new GameObject("ConfettiPiece");
                piece.transform.SetParent(confettiContainer, false);
                Image img = piece.AddComponent<Image>();
                img.color = confettiColors[UnityEngine.Random.Range(0, confettiColors.Length)];

                RectTransform rt = piece.GetComponent<RectTransform>();
                float startX = UnityEngine.Random.Range(-550f, 550f);
                float startY = UnityEngine.Random.Range(250f, 450f);
                float size = UnityEngine.Random.Range(8f, 18f);
                rt.sizeDelta = new Vector2(size, size);
                rt.anchoredPosition = new Vector2(startX, startY);

                StartCoroutine(DropConfettiPiece(rt, UnityEngine.Random.Range(1.8f, 3.5f)));
                yield return new WaitForSeconds(0.04f);
            }
        }

        private IEnumerator DropConfettiPiece(RectTransform rt, float lifetime)
        {
            float elapsed = 0f;
            Vector2 startPos = rt.anchoredPosition;
            float fallSpeed = UnityEngine.Random.Range(180f, 320f);
            float swaySpeed = UnityEngine.Random.Range(2f, 5f);
            float swayAmount = UnityEngine.Random.Range(20f, 60f);
            float rotSpeed = UnityEngine.Random.Range(-180f, 180f);

            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float y = startPos.y - (fallSpeed * elapsed);
                float x = startPos.x + (Mathf.Sin(elapsed * swaySpeed) * swayAmount);
                rt.anchoredPosition = new Vector2(x, y);
                rt.Rotate(0, 0, rotSpeed * Time.deltaTime);
                yield return null;
            }

            if (rt != null && rt.gameObject != null)
            {
                Destroy(rt.gameObject);
            }
        }

        private void PopulateAdviceCards()
        {
            if (adviceCardsContainer == null) return;

            // Clear any placeholders
            for (int i = adviceCardsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(adviceCardsContainer.GetChild(i).gameObject);
            }

            List<AdviceTip> topTips = AdviceEngine.GenerateTopAdvice(3);

            foreach (var tip in topTips)
            {
                CreateAdviceCard(tip);
            }
        }

        private void CreateAdviceCard(AdviceTip tip)
        {
            GameObject card = new GameObject("AdviceCard_" + tip.id);
            card.transform.SetParent(adviceCardsContainer, false);

            Image bg = card.AddComponent<Image>();
            bg.color = new Color32(21, 27, 51, 240); // Dark sleek card bg

            RectTransform rt = card.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(900, 100);

            // Left vertical accent stripe
            GameObject stripe = new GameObject("Stripe");
            stripe.transform.SetParent(card.transform, false);
            Image stripeImg = stripe.AddComponent<Image>();
            stripeImg.color = tip.accentColor;
            RectTransform srt = stripe.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0);
            srt.anchorMax = new Vector2(0, 1);
            srt.pivot = new Vector2(0, 0.5f);
            srt.sizeDelta = new Vector2(8, 0);
            srt.anchoredPosition = Vector2.zero;

            // Icon & Category Badge & Title container
            GameObject headerObj = new GameObject("CardHeader");
            headerObj.transform.SetParent(card.transform, false);
            RectTransform hrt = headerObj.AddComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0, 1);
            hrt.anchorMax = new Vector2(1, 1);
            hrt.pivot = new Vector2(0.5f, 1);
            hrt.sizeDelta = new Vector2(-40, 32);
            hrt.anchoredPosition = new Vector2(16, -12);

            TextMeshProUGUI headerText = headerObj.AddComponent<TextMeshProUGUI>();
            headerText.fontSize = 19;
            headerText.fontStyle = FontStyles.Bold;
            headerText.color = Color.white;
            headerText.alignment = TextAlignmentOptions.Left;
            headerText.text = $"{tip.icon} <color=#{ColorUtility.ToHtmlStringRGBA(tip.accentColor)}>[{tip.category.ToUpper()}]</color> {tip.title}";

            // Body text
            GameObject bodyObj = new GameObject("CardBody");
            bodyObj.transform.SetParent(card.transform, false);
            RectTransform brt = bodyObj.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0);
            brt.anchorMax = new Vector2(1, 1);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(-40, -48);
            brt.anchoredPosition = new Vector2(16, -10);

            TextMeshProUGUI bodyText = bodyObj.AddComponent<TextMeshProUGUI>();
            bodyText.fontSize = 15;
            bodyText.color = new Color32(200, 215, 240, 255);
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            bodyText.text = tip.message;
        }

        private void PopulateEventsEncounteredList()
        {
            if (eventsContainer == null) return;

            for (int i = eventsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(eventsContainer.GetChild(i).gameObject);
            }

            var gm = GameManager.Instance;
            if (gm == null || gm.encounteredEvents == null) return;

            // Enforce strictly no duplicates
            HashSet<string> seenTitles = new HashSet<string>();
            List<EncounteredEventRecord> uniqueEvents = new List<EncounteredEventRecord>();

            foreach (var ev in gm.encounteredEvents)
            {
                if (!seenTitles.Contains(ev.title))
                {
                    seenTitles.Add(ev.title);
                    uniqueEvents.Add(ev);
                }
            }

            foreach (var ev in uniqueEvents)
            {
                CreateEventRow(ev);
            }
        }

        private void CreateEventRow(EncounteredEventRecord ev)
        {
            GameObject row = new GameObject("EventRow_" + ev.title);
            row.transform.SetParent(eventsContainer, false);

            Image rowBg = row.AddComponent<Image>();
            rowBg.color = new Color32(25, 33, 62, 180);

            RectTransform rt = row.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(430, 44);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(row.transform, false);
            RectTransform trt = titleObj.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0);
            trt.anchorMax = new Vector2(0.7f, 1);
            trt.sizeDelta = Vector2.zero;
            trt.anchoredPosition = new Vector2(12, 0);

            TextMeshProUGUI titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.fontSize = 15;
            titleTxt.color = new Color32(220, 230, 250, 255);
            titleTxt.alignment = TextAlignmentOptions.Left;
            titleTxt.textWrappingMode = TextWrappingModes.NoWrap;
            titleTxt.overflowMode = TextOverflowModes.Ellipsis;
            titleTxt.text = ev.title;

            // Amount
            GameObject amtObj = new GameObject("Amount");
            amtObj.transform.SetParent(row.transform, false);
            RectTransform art = amtObj.AddComponent<RectTransform>();
            art.anchorMin = new Vector2(0.7f, 0);
            art.anchorMax = new Vector2(1, 1);
            art.sizeDelta = Vector2.zero;
            art.anchoredPosition = new Vector2(-12, 0);

            TextMeshProUGUI amtTxt = amtObj.AddComponent<TextMeshProUGUI>();
            amtTxt.fontSize = 16;
            amtTxt.fontStyle = FontStyles.Bold;
            amtTxt.alignment = TextAlignmentOptions.Right;

            if (ev.amount < 0)
            {
                amtTxt.color = new Color32(255, 78, 120, 255); // Pink/Red
                amtTxt.text = $"-₹{Mathf.Abs(ev.amount):N0}";
            }
            else
            {
                amtTxt.color = new Color32(0, 240, 160, 255); // Cyan/Green
                amtTxt.text = $"+₹{ev.amount:N0}";
            }
        }

        private void CheckCharacterUnlocks()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            int prevLifetimeScore = gm.lifetimeScore;
            // Add current game final score to lifetime score
            gm.AddLifetimeScore(targetFinalScore);
            int newLifetimeScore = gm.lifetimeScore;

            Debug.Log($"[ResultUI] Lifetime score updated: {prevLifetimeScore} -> {newLifetimeScore}");

            // Check if any character unlocked during this step
            CharacterData newlyUnlocked = null;

            // Character unlock thresholds:
            // Eco Ranger: 300
            // Tech Ranger: 600
            // Royal Ranger: 1000
            var charDatabase = LoadAllCharacters();
            foreach (var ch in charDatabase)
            {
                if (ch != null && !string.Equals(ch.id, "penny", StringComparison.OrdinalIgnoreCase))
                {
                    if (prevLifetimeScore < ch.unlockScore && newLifetimeScore >= ch.unlockScore)
                    {
                        gm.UnlockCharacter(ch.id);
                        newlyUnlocked = ch;
                        break; // Show one unlock celebration at a time
                    }
                }
            }

            if (newlyUnlocked != null && characterUnlockModal != null)
            {
                if (unlockedCharacterImage != null && newlyUnlocked.sprite != null)
                {
                    unlockedCharacterImage.sprite = newlyUnlocked.sprite;
                }
                if (unlockedCharacterNameText != null)
                {
                    unlockedCharacterNameText.text = $"🎉 {newlyUnlocked.characterName} Unlocked!";
                }
                if (unlockedCharacterDescText != null)
                {
                    unlockedCharacterDescText.text = $"{newlyUnlocked.description}\n<color=#FFD700>Requires {newlyUnlocked.unlockScore} Lifetime Points.</color>";
                }
                characterUnlockModal.SetActive(true);
            }
        }

        private List<CharacterData> LoadAllCharacters()
        {
            List<CharacterData> list = new List<CharacterData>();
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CharacterData");
            foreach (var g in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                var cd = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                if (cd != null) list.Add(cd);
            }
#endif
            return list;
        }

        private void SetupActionButtons()
        {
            if (playAgainButton != null)
            {
                playAgainButton.onClick.AddListener(() =>
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.ResetGameSession();
                    }
                    SceneManager.LoadScene("PlayerSetup");
                });
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(() =>
                {
                    SceneManager.LoadScene("Splash");
                });
            }

            if (celebrationCloseButton != null && celebrationModal != null)
            {
                celebrationCloseButton.onClick.AddListener(() =>
                {
                    celebrationModal.SetActive(false);
                });
            }

            if (unlockModalCloseButton != null && characterUnlockModal != null)
            {
                unlockModalCloseButton.onClick.AddListener(() =>
                {
                    characterUnlockModal.SetActive(false);
                });
            }
        }
    }
}
