using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using RupeeRanger.Core;
using RupeeRanger.Data;

namespace RupeeRanger.UI
{
    public class ExpenseSetupUI : MonoBehaviour
    {
        [Header("Monthly Income Card")]
        public TMP_Text monthlyIncomeText;
        public Image incomeIcon;

        [Header("Sliders")]
        public Slider rentSlider;
        public Slider foodSlider;
        public Slider transportSlider;
        public Slider entertainmentSlider;

        [Header("Value Texts")]
        public TMP_Text rentValueText;
        public TMP_Text foodValueText;
        public TMP_Text transportValueText;
        public TMP_Text entertainmentValueText;

        [Header("Min/Max Labels")]
        public TMP_Text rentMinText;
        public TMP_Text rentMaxText;
        public TMP_Text foodMinText;
        public TMP_Text foodMaxText;
        public TMP_Text transportMinText;
        public TMP_Text transportMaxText;
        public TMP_Text entertainmentMinText;
        public TMP_Text entertainmentMaxText;

        [Header("Live Calculation Panel")]
        public TMP_Text calcIncomeText;
        public TMP_Text calcExpensesText;
        public TMP_Text calcSavingsText;
        public TMP_Text currentScoreText;
        public GameObject savingsWarningObject;
        public TMP_Text savingsWarningText;

        [Header("Character Interactive Stage")]
        public RectTransform characterContainer;
        public Image characterImage;
        public TMP_Text characterPlaceLabel;
        public RectTransform[] stationWaypoints; // 0: Rent/Home, 1: Food/Restaurant, 2: Transport/Bus, 3: Entertainment/Arcade
        public string[] stationLabels = new string[]
        {
            "Home (Rent)",
            "Restaurant (Food)",
            "Bus Stop (Transport)",
            "Game Zone (Entertainment)"
        };

        [Header("Action Button")]
        public Button startSimulationButton;

        // Internal state
        private int income = 15000;
        private int maxRent = 15000;
        private int maxFood = 8000;
        private int maxTransport = 3000;
        private int maxEntertainment = 5000;

        private int rentVal = 3000;
        private int foodVal = 2000;
        private int transportVal = 500;
        private int entertainmentVal = 1000;

        private int targetStation = 0;
        private Vector2 currentCharacterPos;
        private bool isUpdatingSliders = false;

        private void Start()
        {
            LoadFromGameManager();
            SetupSliders();
            SetupCharacter();
            UpdateLiveCalculation();

            if (startSimulationButton != null)
            {
                startSimulationButton.onClick.AddListener(OnStartSimulationClicked);
            }
        }

        private void LoadFromGameManager()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                income = gm.monthlyIncome;
                LifestyleConfig cfg = gm.lifestyleConfig;
                if (cfg != null)
                {
                    income = cfg.monthlyIncome > 0 ? cfg.monthlyIncome : income;
                    maxRent = cfg.maxRent > 0 ? cfg.maxRent : 15000;
                    maxFood = cfg.maxFood > 0 ? cfg.maxFood : 8000;
                    maxTransport = cfg.maxTransport > 0 ? cfg.maxTransport : 3000;
                    maxEntertainment = cfg.maxEntertainment > 0 ? cfg.maxEntertainment : 5000;
                }
                else
                {
                    // Scale defaults to current income
                    maxRent = income;
                    maxFood = Mathf.RoundToInt(income * 0.55f);
                    maxTransport = Mathf.RoundToInt(income * 0.25f);
                    maxEntertainment = Mathf.RoundToInt(income * 0.35f);
                }

                // Default sensible budget that matches the design (Rent 3000, Food 2000, Transport 500, Ent 1000)
                rentVal = Mathf.Min(3000, maxRent);
                foodVal = Mathf.Min(2000, maxFood);
                transportVal = Mathf.Min(500, maxTransport);
                entertainmentVal = Mathf.Min(1000, maxEntertainment);
            }
            else
            {
                income = 15000;
                maxRent = 15000;
                maxFood = 8000;
                maxTransport = 3000;
                maxEntertainment = 5000;
                rentVal = 3000;
                foodVal = 2000;
                transportVal = 500;
                entertainmentVal = 1000;
            }

            if (monthlyIncomeText != null)
            {
                monthlyIncomeText.text = FormatCurrency(income);
            }

            if (currentScoreText != null)
            {
                int score = gm != null ? gm.score : 1000;
                currentScoreText.text = $"Starting Score: {score:N0} pts";
            }
        }

        private void SetupSliders()
        {
            isUpdatingSliders = true;

            // Configure Rent
            if (rentSlider != null)
            {
                rentSlider.minValue = 0;
                rentSlider.maxValue = maxRent;
                rentSlider.wholeNumbers = true;
                rentSlider.value = rentVal;
                rentSlider.onValueChanged.AddListener((v) => OnSliderChanged(0, (int)v));
                AddSliderTriggers(rentSlider, 0);
            }
            if (rentMinText != null) rentMinText.text = FormatCurrency(0);
            if (rentMaxText != null) rentMaxText.text = FormatCurrency(maxRent);

            // Configure Food
            if (foodSlider != null)
            {
                foodSlider.minValue = 0;
                foodSlider.maxValue = maxFood;
                foodSlider.wholeNumbers = true;
                foodSlider.value = foodVal;
                foodSlider.onValueChanged.AddListener((v) => OnSliderChanged(1, (int)v));
                AddSliderTriggers(foodSlider, 1);
            }
            if (foodMinText != null) foodMinText.text = FormatCurrency(0);
            if (foodMaxText != null) foodMaxText.text = FormatCurrency(maxFood);

            // Configure Transport
            if (transportSlider != null)
            {
                transportSlider.minValue = 0;
                transportSlider.maxValue = maxTransport;
                transportSlider.wholeNumbers = true;
                transportSlider.value = transportVal;
                transportSlider.onValueChanged.AddListener((v) => OnSliderChanged(2, (int)v));
                AddSliderTriggers(transportSlider, 2);
            }
            if (transportMinText != null) transportMinText.text = FormatCurrency(0);
            if (transportMaxText != null) transportMaxText.text = FormatCurrency(maxTransport);

            // Configure Entertainment
            if (entertainmentSlider != null)
            {
                entertainmentSlider.minValue = 0;
                entertainmentSlider.maxValue = maxEntertainment;
                entertainmentSlider.wholeNumbers = true;
                entertainmentSlider.value = entertainmentVal;
                entertainmentSlider.onValueChanged.AddListener((v) => OnSliderChanged(3, (int)v));
                AddSliderTriggers(entertainmentSlider, 3);
            }
            if (entertainmentMinText != null) entertainmentMinText.text = FormatCurrency(0);
            if (entertainmentMaxText != null) entertainmentMaxText.text = FormatCurrency(maxEntertainment);

            isUpdatingSliders = false;
            UpdateSliderValueTexts();
        }

        private void AddSliderTriggers(Slider slider, int stationIndex)
        {
            EventTrigger trigger = slider.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = slider.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry pointerDown = new EventTrigger.Entry();
            pointerDown.eventID = EventTriggerType.PointerDown;
            pointerDown.callback.AddListener((data) => MoveCharacterToStation(stationIndex));
            trigger.triggers.Add(pointerDown);

            EventTrigger.Entry drag = new EventTrigger.Entry();
            drag.eventID = EventTriggerType.Drag;
            drag.callback.AddListener((data) => MoveCharacterToStation(stationIndex));
            trigger.triggers.Add(drag);
        }

        private void SetupCharacter()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.selectedCharacter != null && characterImage != null)
            {
                if (gm.selectedCharacter.sprite != null)
                {
                    characterImage.sprite = gm.selectedCharacter.sprite;
                }
            }

            MoveCharacterToStation(0);
            if (characterContainer != null && stationWaypoints != null && stationWaypoints.Length > 0 && stationWaypoints[0] != null)
            {
                characterContainer.anchoredPosition = stationWaypoints[0].anchoredPosition;
                currentCharacterPos = stationWaypoints[0].anchoredPosition;
            }
        }

        public void MoveCharacterToStation(int index)
        {
            targetStation = Mathf.Clamp(index, 0, 3);
            if (characterPlaceLabel != null && targetStation < stationLabels.Length)
            {
                characterPlaceLabel.text = stationLabels[targetStation];
            }
        }

        private void Update()
        {
            // Smoothly move character to target station
            if (characterContainer != null && stationWaypoints != null && targetStation < stationWaypoints.Length && stationWaypoints[targetStation] != null)
            {
                Vector2 targetPos = stationWaypoints[targetStation].anchoredPosition;
                float dist = Vector2.Distance(currentCharacterPos, targetPos);

                if (dist > 1f)
                {
                    // Walking animation: lerp position and add a bounce
                    currentCharacterPos = Vector2.MoveTowards(currentCharacterPos, targetPos, Time.deltaTime * 650f);
                    float bounce = Mathf.Abs(Mathf.Sin(Time.time * 16f)) * 8f;
                    characterContainer.anchoredPosition = new Vector2(currentCharacterPos.x, currentCharacterPos.y + bounce);

                    // Face walking direction
                    if (characterImage != null)
                    {
                        float facing = targetPos.x < currentCharacterPos.x ? -1f : 1f;
                        characterImage.rectTransform.localScale = new Vector3(facing, 1f, 1f);
                    }
                }
                else
                {
                    // Idle breathing bob
                    currentCharacterPos = targetPos;
                    float idleBob = Mathf.Sin(Time.time * 3f) * 2f;
                    characterContainer.anchoredPosition = new Vector2(currentCharacterPos.x, currentCharacterPos.y + idleBob);
                }
            }
        }

        private void OnSliderChanged(int index, int rawValue)
        {
            if (isUpdatingSliders) return;

            // Move character
            MoveCharacterToStation(index);

            // Compute other expenses
            int otherSum = 0;
            switch (index)
            {
                case 0: otherSum = foodVal + transportVal + entertainmentVal; break;
                case 1: otherSum = rentVal + transportVal + entertainmentVal; break;
                case 2: otherSum = rentVal + foodVal + entertainmentVal; break;
                case 3: otherSum = rentVal + foodVal + transportVal; break;
            }

            // Prevent total expenses from exceeding income
            int maxAllowedForThis = Mathf.Max(0, income - otherSum);
            int clampedValue = Mathf.Min(rawValue, maxAllowedForThis);

            if (clampedValue != rawValue)
            {
                isUpdatingSliders = true;
                switch (index)
                {
                    case 0: if (rentSlider != null) rentSlider.value = clampedValue; break;
                    case 1: if (foodSlider != null) foodSlider.value = clampedValue; break;
                    case 2: if (transportSlider != null) transportSlider.value = clampedValue; break;
                    case 3: if (entertainmentSlider != null) entertainmentSlider.value = clampedValue; break;
                }
                isUpdatingSliders = false;
            }

            switch (index)
            {
                case 0: rentVal = clampedValue; break;
                case 1: foodVal = clampedValue; break;
                case 2: transportVal = clampedValue; break;
                case 3: entertainmentVal = clampedValue; break;
            }

            UpdateSliderValueTexts();
            UpdateLiveCalculation();
        }

        private void UpdateSliderValueTexts()
        {
            if (rentValueText != null) rentValueText.text = FormatCurrency(rentVal);
            if (foodValueText != null) foodValueText.text = FormatCurrency(foodVal);
            if (transportValueText != null) transportValueText.text = FormatCurrency(transportVal);
            if (entertainmentValueText != null) entertainmentValueText.text = FormatCurrency(entertainmentVal);
        }

        private void UpdateLiveCalculation()
        {
            int totalExpenses = rentVal + foodVal + transportVal + entertainmentVal;
            int initialSavings = income - totalExpenses;

            if (calcIncomeText != null)
            {
                calcIncomeText.text = FormatCurrency(income);
            }

            if (calcExpensesText != null)
            {
                calcExpensesText.text = $"-{FormatCurrency(totalExpenses)}";
            }

            if (calcSavingsText != null)
            {
                calcSavingsText.text = FormatCurrency(initialSavings);

                // Healthy green vs amber vs red
                if (initialSavings >= income * 0.35f)
                {
                    calcSavingsText.color = new Color32(0x00, 0xE6, 0x76, 0xFF); // Vibrant green
                }
                else if (initialSavings >= income * 0.15f)
                {
                    calcSavingsText.color = new Color32(0xFF, 0xC1, 0x07, 0xFF); // Amber
                }
                else
                {
                    calcSavingsText.color = new Color32(0xFF, 0x52, 0x52, 0xFF); // Red
                }
            }

            // Gentle warning for children if savings are very low
            bool isVeryLow = initialSavings < (income * 0.15f);
            if (savingsWarningObject != null)
            {
                savingsWarningObject.SetActive(isVeryLow);
            }

            if (savingsWarningText != null && isVeryLow)
            {
                if (initialSavings <= 0)
                {
                    savingsWarningText.text = "Careful! You have zero savings left! If an emergency happens in town, you won't have funds.";
                }
                else
                {
                    savingsWarningText.text = "Tip: Your savings are quite low! Try saving a little more so you have room for surprises in town.";
                }
            }
        }

        private string FormatCurrency(int amount)
        {
            return $"Rs. {amount:N0}";
        }

        private void OnStartSimulationClicked()
        {
            int totalExpenses = rentVal + foodVal + transportVal + entertainmentVal;
            int initialSavings = income - totalExpenses;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetInitialExpenses(rentVal, foodVal, transportVal, entertainmentVal);
            }

            // Load Town scene
            SceneManager.LoadScene("Town");
        }
    }
}
