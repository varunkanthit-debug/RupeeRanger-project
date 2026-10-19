using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using RupeeRanger.Core;
using RupeeRanger.Data;

namespace RupeeRanger.UI
{
    public class PlayerSetupUI : MonoBehaviour
    {
        [Header("Header Elements")]
        public TMP_Text lifetimeScoreText;

        [Header("Personal Information")]
        public TMP_InputField nameInputField;
        public TMP_InputField ageInputField;
        public TMP_Text ageValidationText;

        [Header("Lifestyle Section")]
        public List<LifestyleConfig> availableLifestyles = new List<LifestyleConfig>();
        public List<LifestyleCardItem> lifestyleCardViews = new List<LifestyleCardItem>();

        [Header("Character Section")]
        public List<CharacterData> availableCharacters = new List<CharacterData>();
        public List<CharacterCardItem> characterCardViews = new List<CharacterCardItem>();

        [Header("Feedback Toast")]
        public TMP_Text feedbackToastText;

        [Header("Bottom Action")]
        public Button startGameButton;
        public Image startGameButtonImage;
        public Color buttonEnabledColor = new Color32(0xFF, 0x47, 0x6E, 0xFF); // Coral Red
        public Color buttonDisabledColor = new Color32(0x50, 0x40, 0x60, 0x80); // Dimmed

        private LifestyleConfig selectedLifestyleConfig;
        private CharacterData selectedCharacterData;

        private void Start()
        {
            // Set career score
            if (lifetimeScoreText != null)
            {
                int score = GameManager.Instance != null ? GameManager.Instance.lifetimeScore : 0;
                lifetimeScoreText.text = $"Lifetime Score: {score:N0} pts";
            }

            // Setup listeners
            if (nameInputField != null)
            {
                nameInputField.onValueChanged.AddListener(OnInputChanged);
            }

            if (ageInputField != null)
            {
                ageInputField.onValueChanged.AddListener(OnInputChanged);
            }

            if (startGameButton != null)
            {
                startGameButton.onClick.AddListener(OnStartGameClicked);
            }

            SetupLifestyles();
            SetupCharacters();

            // Initial validation check
            ValidateForm();
        }

        private void SetupLifestyles()
        {
            if (availableLifestyles == null || availableLifestyles.Count == 0) return;

            for (int i = 0; i < lifestyleCardViews.Count && i < availableLifestyles.Count; i++)
            {
                lifestyleCardViews[i].Setup(availableLifestyles[i], OnLifestyleSelected);
            }

            // Select Middle lifestyle by default (index 1 if available, otherwise 0)
            int defaultIndex = availableLifestyles.Count > 1 ? 1 : 0;
            if (lifestyleCardViews.Count > defaultIndex)
            {
                OnLifestyleSelected(lifestyleCardViews[defaultIndex]);
            }
        }

        private void SetupCharacters()
        {
            if (availableCharacters == null || availableCharacters.Count == 0) return;

            for (int i = 0; i < characterCardViews.Count && i < availableCharacters.Count; i++)
            {
                CharacterData charData = availableCharacters[i];
                bool isUnlocked = GameManager.Instance != null && GameManager.Instance.IsCharacterUnlocked(charData.id, charData.unlockScore);

                characterCardViews[i].Setup(charData, isUnlocked, OnCharacterSelected, ShowFeedback);
            }

            // Select Penny by default (first character)
            if (characterCardViews.Count > 0 && characterCardViews[0].IsUnlocked)
            {
                OnCharacterSelected(characterCardViews[0]);
            }
        }

        private void OnLifestyleSelected(LifestyleCardItem selectedItem)
        {
            foreach (var item in lifestyleCardViews)
            {
                item.SetSelected(item == selectedItem);
            }

            selectedLifestyleConfig = selectedItem.Config;
            ValidateForm();
        }

        private void OnCharacterSelected(CharacterCardItem selectedItem)
        {
            foreach (var item in characterCardViews)
            {
                item.SetSelected(item == selectedItem);
            }

            selectedCharacterData = selectedItem.Character;
            ShowFeedback($"Selected Ranger: {selectedCharacterData.characterName}!");
            ValidateForm();
        }

        private void OnInputChanged(string _)
        {
            ValidateForm();
        }

        private void ValidateForm()
        {
            bool isNameValid = !string.IsNullOrWhiteSpace(nameInputField != null ? nameInputField.text : "");
            
            bool isAgeValid = false;
            string ageStr = ageInputField != null ? ageInputField.text.Trim() : "";

            if (string.IsNullOrEmpty(ageStr))
            {
                if (ageValidationText != null)
                {
                    ageValidationText.text = "Enter your age (between 5 and 14)";
                    ageValidationText.color = new Color32(0x9E, 0x9B, 0xC4, 0xFF);
                }
            }
            else if (int.TryParse(ageStr, out int ageVal))
            {
                if (ageVal >= 5 && ageVal <= 14)
                {
                    isAgeValid = true;
                    if (ageValidationText != null)
                    {
                        ageValidationText.text = "Great! Age confirmed.";
                        ageValidationText.color = new Color32(0x00, 0xE6, 0x76, 0xFF);
                    }
                }
                else
                {
                    if (ageValidationText != null)
                    {
                        ageValidationText.text = "Note: RupeeRanger is designed for ages 5 to 14! Please enter an age between 5 and 14.";
                        ageValidationText.color = new Color32(0xFF, 0x52, 0x52, 0xFF);
                    }
                }
            }
            else
            {
                if (ageValidationText != null)
                {
                    ageValidationText.text = "Please enter a valid number for age (5 - 14).";
                    ageValidationText.color = new Color32(0xFF, 0x52, 0x52, 0xFF);
                }
            }

            bool isLifestyleValid = selectedLifestyleConfig != null;
            bool isCharacterValid = selectedCharacterData != null;

            bool canStart = isNameValid && isAgeValid && isLifestyleValid && isCharacterValid;

            if (startGameButton != null)
            {
                startGameButton.interactable = canStart;
            }

            if (startGameButtonImage != null)
            {
                startGameButtonImage.color = canStart ? buttonEnabledColor : buttonDisabledColor;
            }
        }

        private void ShowFeedback(string message)
        {
            if (feedbackToastText != null)
            {
                feedbackToastText.text = message;
            }
        }

        private void OnStartGameClicked()
        {
            string name = nameInputField != null ? nameInputField.text.Trim() : "Ranger";
            int age = 10;
            if (ageInputField != null && int.TryParse(ageInputField.text.Trim(), out int parsedAge))
            {
                age = parsedAge;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetPlayerProfile(name, age, selectedLifestyleConfig, selectedCharacterData);
            }

            // Navigate to ExpenseSetup scene
            SceneManager.LoadScene("ExpenseSetup");
        }
    }
}
