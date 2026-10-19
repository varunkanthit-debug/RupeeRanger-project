using System;
using System.Collections.Generic;
using UnityEngine;
using RupeeRanger.Data;

namespace RupeeRanger.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public const string PREFS_KEY_LIFETIME_SCORE = "RupeeRanger_LifetimeScore";
        public const string PREFS_KEY_UNLOCKED_CHARS = "RupeeRanger_UnlockedCharacters";
        public const string PREFS_KEY_DARK_MODE = "RupeeRanger_DarkMode";

        [Header("Player Profile")]
        public string playerName = "Adventurer";
        public int playerAge = 10;
        public LifestyleType selectedLifestyle = LifestyleType.Middle;
        public LifestyleConfig lifestyleConfig;
        public string selectedCharacterId = "penny";
        public CharacterData selectedCharacter;

        [Header("Financial Simulation")]
        public int monthlyIncome = 15000;
        public int rentExpense = 5000;
        public int foodExpense = 3500;
        public int transportExpense = 1500;
        public int entertainmentExpense = 1500;
        public int initialSavings = 3500;
        public int savings = 3500;
        public int debt = 0;
        public int currentDay = 1;
        public int score = 1000;

        public int TotalExpenses => rentExpense + foodExpense + transportExpense + entertainmentExpense;

        [Header("Career & Progression")]
        public int lifetimeScore = 0;
        private HashSet<string> unlockedCharacterIds = new HashSet<string>();

        [Header("History & Result Tracking")]
        public List<string> eventHistory = new List<string>();
        public List<TransactionRecord> transactionHistory = new List<TransactionRecord>();
        public List<EncounteredEventRecord> encounteredEvents = new List<EncounteredEventRecord>();
        public int totalLoanTaken = 0;
        public int totalInterestPaid = 0;
        public int totalEventExpenses = 0;
        public int totalEventWindfalls = 0;
        public bool playerRepaidLoanEarly = false;
        public int wiseEventChoicesCount = 0;
        public int emergencyEventsCount = 0;
        public int windfallEventsCount = 0;
        public int impulseSpendingEventsCount = 0;

        [Header("Theme Settings")]
        public bool isDarkMode = true;
        public event Action<bool> OnThemeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadCareerData();
        }

        #region Career Save & Load (PlayerPrefs)

        public void LoadCareerData()
        {
            lifetimeScore = PlayerPrefs.GetInt(PREFS_KEY_LIFETIME_SCORE, 0);
            isDarkMode = PlayerPrefs.GetInt(PREFS_KEY_DARK_MODE, 1) == 1;

            unlockedCharacterIds.Clear();
            unlockedCharacterIds.Add("penny"); // default first character

            string savedChars = PlayerPrefs.GetString(PREFS_KEY_UNLOCKED_CHARS, "penny");
            string[] split = savedChars.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string id in split)
            {
                unlockedCharacterIds.Add(id.Trim().ToLowerInvariant());
            }
        }

        public void SaveCareerData()
        {
            PlayerPrefs.SetInt(PREFS_KEY_LIFETIME_SCORE, lifetimeScore);
            PlayerPrefs.SetInt(PREFS_KEY_DARK_MODE, isDarkMode ? 1 : 0);

            string joined = string.Join(",", unlockedCharacterIds);
            PlayerPrefs.GetString(PREFS_KEY_UNLOCKED_CHARS, joined);
            PlayerPrefs.SetString(PREFS_KEY_UNLOCKED_CHARS, joined);
            PlayerPrefs.Save();
        }

        public void AddLifetimeScore(int points)
        {
            lifetimeScore += Mathf.Max(0, points);
            SaveCareerData();
        }

        public bool IsCharacterUnlocked(string charId, int requiredScore)
        {
            if (string.IsNullOrEmpty(charId)) return false;
            string lowerId = charId.ToLowerInvariant();

            // Default character is always unlocked
            if (lowerId == "penny" || requiredScore <= 0)
            {
                return true;
            }

            // Already unlocked or qualifies via lifetime score
            if (unlockedCharacterIds.Contains(lowerId) || lifetimeScore >= requiredScore)
            {
                unlockedCharacterIds.Add(lowerId);
                return true;
            }

            return false;
        }

        public void UnlockCharacter(string charId)
        {
            if (string.IsNullOrEmpty(charId)) return;
            unlockedCharacterIds.Add(charId.ToLowerInvariant());
            SaveCareerData();
        }

        #endregion

        #region Profile Setup

        public void SetPlayerProfile(string name, int age, LifestyleConfig config, CharacterData character)
        {
            playerName = string.IsNullOrWhiteSpace(name) ? "Adventurer" : name.Trim();
            playerAge = Mathf.Clamp(age, 5, 14);

            lifestyleConfig = config;
            if (config != null)
            {
                selectedLifestyle = config.lifestyleType;
                monthlyIncome = config.monthlyIncome;
            }

            selectedCharacter = character;
            if (character != null)
            {
                selectedCharacterId = character.id;
            }
        }

        public void SetInitialExpenses(int rent, int food, int transport, int entertainment)
        {
            rentExpense = rent;
            foodExpense = food;
            transportExpense = transport;
            entertainmentExpense = entertainment;

            initialSavings = monthlyIncome - TotalExpenses;
            savings = initialSavings;
            debt = 0;
            currentDay = 1;
            score = 1000;

            eventHistory.Clear();
            transactionHistory.Clear();

            AddTransaction(1, "Monthly Income (Budget Approved)", monthlyIncome, "Income");
            AddTransaction(1, "Total Upfront Budget Allocation", -TotalExpenses, "Expenses");
        }

        #endregion

        #region Transactions & Ledger

        public void AddTransaction(int day, string description, int amount, string category)
        {
            transactionHistory.Add(new TransactionRecord(day, description, amount, category));
        }

        public void ApplySavingsChange(int delta, string description, string category = "Event")
        {
            savings += delta;
            AddTransaction(currentDay, description, delta, category);
        }

        public void ApplyDebtChange(int delta, string description)
        {
            debt += delta;
            AddTransaction(currentDay, description, delta, "Loan");
        }

        public void RecordEncounteredEvent(string title, int amount, string category)
        {
            if (string.IsNullOrWhiteSpace(title)) return;

            // Guarantee no duplicates in encountered events list
            if (!encounteredEvents.Exists(e => string.Equals(e.title, title, StringComparison.OrdinalIgnoreCase)))
            {
                encounteredEvents.Add(new EncounteredEventRecord
                {
                    title = title,
                    amount = amount,
                    category = category
                });
            }

            if (amount < 0)
            {
                totalEventExpenses += Mathf.Abs(amount);
            }
            else
            {
                totalEventWindfalls += amount;
            }

            if (string.Equals(category, "Emergency", StringComparison.OrdinalIgnoreCase))
            {
                emergencyEventsCount++;
            }
            else if (string.Equals(category, "Windfall", StringComparison.OrdinalIgnoreCase))
            {
                windfallEventsCount++;
            }
        }

        #endregion

        #region Theme Toggle

        public void ToggleTheme()
        {
            SetTheme(!isDarkMode);
        }

        public void SetTheme(bool dark)
        {
            isDarkMode = dark;
            PlayerPrefs.SetInt(PREFS_KEY_DARK_MODE, isDarkMode ? 1 : 0);
            PlayerPrefs.Save();
            OnThemeChanged?.Invoke(isDarkMode);
        }

        #endregion

        #region Money Formatting Utility

        /// <summary>
        /// Formats integer amount with clean rupee prefix and thousands separators (e.g. ₹15,000).
        /// </summary>
        public static string FormatMoney(int amount)
        {
            if (amount < 0)
            {
                return $"-₹{Mathf.Abs(amount):N0}";
            }
            return $"₹{amount:N0}";
        }

        #endregion

        #region Reset Utility

        public void ResetGameSession()
        {
            currentDay = 1;
            debt = 0;
            savings = initialSavings;
            score = 1000;
            totalLoanTaken = 0;
            totalInterestPaid = 0;
            totalEventExpenses = 0;
            totalEventWindfalls = 0;
            encounteredEvents.Clear();
            eventHistory.Clear();
            transactionHistory.Clear();
            playerRepaidLoanEarly = false;
            wiseEventChoicesCount = 0;
            emergencyEventsCount = 0;
            windfallEventsCount = 0;
            impulseSpendingEventsCount = 0;
        }

        public void ResetAllDataForDebug()
        {
            PlayerPrefs.DeleteKey(PREFS_KEY_LIFETIME_SCORE);
            PlayerPrefs.DeleteKey(PREFS_KEY_UNLOCKED_CHARS);
            PlayerPrefs.DeleteKey(PREFS_KEY_DARK_MODE);
            PlayerPrefs.Save();
            LoadCareerData();
        }

        #endregion
    }

    [Serializable]
    public class EncounteredEventRecord
    {
        public string title;
        public int amount;
        public string category;
    }
}
