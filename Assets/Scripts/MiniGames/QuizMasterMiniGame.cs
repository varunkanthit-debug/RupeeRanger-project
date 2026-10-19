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
    public class QuizMasterMiniGame : MiniGame
    {
        [System.Serializable]
        public class QuizQuestion
        {
            public string category;
            public string question;
            public string[] options; // 4 options
            public int correctIndex; // 0-3
            public string explanation;
        }

        [Header("UI References")]
        public TMP_Text titleText;
        public TMP_Text categoryText;
        public TMP_Text questionText;
        public TMP_Text progressText;
        public TMP_Text feedbackText;
        public Button[] optionButtons; // 4 buttons
        public TMP_Text[] optionTexts; // 4 option texts
        public GameObject victoryPanel;
        public TMP_Text victoryMessage;
        public Button finishButton;
        public Button closeButton;

        private static readonly QuizQuestion[] QUESTIONS_EASY = new QuizQuestion[]
        {
            new QuizQuestion
            {
                category = "Piggy Bank",
                question = "Where is the best place to safely keep your pocket money coins?",
                options = new[] { "In a Piggy Bank", "Throw them in trash", "Under a wet rock", "Leave on the road" },
                correctIndex = 0,
                explanation = "A piggy bank keeps your coins safe so you can save for something special!"
            },
            new QuizQuestion
            {
                category = "Needs vs Wants",
                question = "Which of these is an essential 'NEED' for school?",
                options = new[] { "Toy video game", "Pencil and Notebook", "Chocolate candy", "Toy robot" },
                correctIndex = 1,
                explanation = "Books and pencils are school needs. Toys and sweets are fun wants!"
            },
            new QuizQuestion
            {
                category = "Money Math",
                question = "If you have 10 Rupees and spend 4 Rupees on an eraser, how much is left?",
                options = new[] { "0 Rupees", "14 Rupees", "6 Rupees", "2 Rupees" },
                correctIndex = 2,
                explanation = "10 minus 4 equals 6 Rupees left to save!"
            },
            new QuizQuestion
            {
                category = "Daily Saving",
                question = "If you save 5 Rupees every day for 4 days, how much have you saved?",
                options = new[] { "5 Rupees", "10 Rupees", "50 Rupees", "20 Rupees" },
                correctIndex = 3,
                explanation = "5 + 5 + 5 + 5 = 20 Rupees! Daily savings add up fast!"
            },
            new QuizQuestion
            {
                category = "Smart Thinking",
                question = "What should you do before buying a toy with your pocket money?",
                options = new[] { "Ask: Do I really need this or just want it?", "Buy everything in 1 second", "Scream loudly in the shop", "Throw your wallet away" },
                correctIndex = 0,
                explanation = "Pausing to think helps you save money for things that really matter!"
            },
            new QuizQuestion
            {
                category = "Wants vs Needs",
                question = "Which of these items is a 'WANT' rather than a need?",
                options = new[] { "Clean drinking water", "A remote-control toy car", "Healthy warm food", "Warm winter jacket" },
                correctIndex = 1,
                explanation = "A toy car is a fun want! Needs like clean water and food always come first."
            }
        };

        private static readonly QuizQuestion[] QUESTIONS_MEDIUM = new QuizQuestion[]
        {
            new QuizQuestion
            {
                category = "Bank Safety",
                question = "Where can your family safely keep money and watch it grow?",
                options = new[] { "In a sandpit", "Under a mattress", "In a Savings Bank", "In a toy box" },
                correctIndex = 2,
                explanation = "Banks keep family money completely safe and pay bonus interest!"
            },
            new QuizQuestion
            {
                category = "Saving Goals",
                question = "If you want to buy a new bicycle, what is the smartest plan?",
                options = new[] { "Save a little pocket money every week", "Spend all money on ice-cream", "Borrow and never return", "Forget about the bicycle" },
                correctIndex = 0,
                explanation = "Saving every week helps you afford your dream goals!"
            },
            new QuizQuestion
            {
                category = "Shopping Smart",
                question = "What is the smartest thing to bring when shopping at the supermarket?",
                options = new[] { "A shopping list of items you need", "Empty pockets with no plan", "A box of toy cars", "A loud whistle" },
                correctIndex = 0,
                explanation = "A shopping list stops you from impulse buying things you don't need!"
            },
            new QuizQuestion
            {
                category = "Bank Interest",
                question = "What is bank 'Interest' for your savings account?",
                options = new[] { "A fee to take your money", "A toy from the manager", "Extra bonus money the bank adds to your savings", "A penalty for saving" },
                correctIndex = 2,
                explanation = "Interest is free bonus money that banks pay you for saving with them!"
            },
            new QuizQuestion
            {
                category = "Sharing & Giving",
                question = "What is a wonderful way to use a small part of your saved money?",
                options = new[] { "Tear the paper notes", "Throw coins in a lake", "Hide it and forget it", "Help someone in need or buy a family gift" },
                correctIndex = 3,
                explanation = "Sharing your blessings and helping others is part of wise money habits!"
            }
        };

        private static readonly QuizQuestion[] QUESTIONS_HARD = new QuizQuestion[]
        {
            new QuizQuestion
            {
                category = "Emergency Fund",
                question = "Why do families keep emergency savings in reserve?",
                options = new[] { "To buy 100 video games", "For unexpected things like doctor visits or repairs", "To burn the money", "To hide from everyone" },
                correctIndex = 1,
                explanation = "Emergency savings protect your family when unexpected costs arise!"
            },
            new QuizQuestion
            {
                category = "Smart Shopper",
                question = "If Shop A sells a box of crayons for Rs 50 and Shop B sells it for Rs 35, where should you buy?",
                options = new[] { "Shop B to save 15 Rupees", "Shop A to spend more money", "Neither, don't draw", "Both shops at the same time" },
                correctIndex = 0,
                explanation = "Comparing prices helps you save money on the exact same item!"
            },
            new QuizQuestion
            {
                category = "Honest Habits",
                question = "If a shopkeeper accidentally gives you 10 Rupees extra change, what should you do?",
                options = new[] { "Politely return the extra 10 Rupees", "Run away as fast as possible", "Brag to your friends", "Hide behind a tree" },
                correctIndex = 0,
                explanation = "Honesty is the most valuable trait in life and finance!"
            },
            new QuizQuestion
            {
                category = "Borrowing Rules",
                question = "If you borrow 20 Rupees from a friend to buy a notebook, what must you do?",
                options = new[] { "Pretend you forgot about it", "Pay it back promptly as promised", "Run away to another town", "Ask for 200 more" },
                correctIndex = 1,
                explanation = "Paying back borrowed money on time builds trust and true friendship!"
            }
        };

        private static HashSet<string> recentQuestions = new HashSet<string>();

        private List<QuizQuestion> sessionQuestions = new List<QuizQuestion>();
        private int currentQuestionIndex = 0;
        private int correctAnswersCount = 0;
        private int targetCorrect = 2; // Always 2 easy questions for kids
        private bool isAnsweringLocked = false;
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
            gameTitle = "Library Quiz Master";

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

            // Exactly 2 questions for kids
            targetCorrect = 2;

            // Wire option buttons
            if (optionButtons != null)
            {
                for (int i = 0; i < optionButtons.Length; i++)
                {
                    int capturedIndex = i;
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(capturedIndex));
                }
            }

            SelectSessionQuestions(band);
        }

        public override void StartGame()
        {
            isGameActive = true;
            isCompleted = false;
            currentQuestionIndex = 0;
            correctAnswersCount = 0;
            isAnsweringLocked = false;

            DisplayCurrentQuestion();
        }

        private void SelectSessionQuestions(DifficultyBand band)
        {
            QuizQuestion[] source = (band == DifficultyBand.Easy) ? QUESTIONS_EASY :
                                   (band == DifficultyBand.Medium) ? QUESTIONS_MEDIUM : QUESTIONS_HARD;

            List<QuizQuestion> pool = new List<QuizQuestion>();
            foreach (var q in source)
            {
                if (!recentQuestions.Contains(q.question))
                {
                    pool.Add(q);
                }
            }

            if (pool.Count < 2)
            {
                recentQuestions.Clear();
                pool = new List<QuizQuestion>(source);
            }

            sessionQuestions.Clear();
            int countToPick = Mathf.Min(2, pool.Count);
            for (int i = 0; i < countToPick; i++)
            {
                int r = UnityEngine.Random.Range(0, pool.Count);
                sessionQuestions.Add(pool[r]);
                recentQuestions.Add(pool[r].question);
                pool.RemoveAt(r);
            }
        }

        private void DisplayCurrentQuestion()
        {
            if (currentQuestionIndex >= sessionQuestions.Count)
            {
                if (correctAnswersCount >= targetCorrect)
                {
                    TriggerVictory();
                }
                else
                {
                    // Let's try 2 fresh questions
                    if (feedbackText != null)
                    {
                        feedbackText.text = $"You got {correctAnswersCount}/{targetCorrect} correct. Let's try 2 fresh questions!";
                        feedbackText.color = new Color32(0xFF, 0x52, 0x52, 0xFF);
                    }
                    SelectSessionQuestions(difficultyBand);
                    currentQuestionIndex = 0;
                    correctAnswersCount = 0;
                    Invoke(nameof(DisplayCurrentQuestion), 2f);
                }
                return;
            }

            isAnsweringLocked = false;
            QuizQuestion q = sessionQuestions[currentQuestionIndex];

            if (titleText != null) titleText.text = "Financial Literacy Quiz";
            if (categoryText != null) categoryText.text = $"Category: {q.category}";
            if (questionText != null) questionText.text = q.question;
            if (progressText != null) progressText.text = $"Question {currentQuestionIndex + 1} of {sessionQuestions.Count}   |   Score: {correctAnswersCount}/{targetCorrect}";

            if (feedbackText != null)
            {
                feedbackText.text = "Select the best answer:";
                feedbackText.color = new Color32(0x90, 0xCA, 0xF9, 0xFF);
            }

            // Populate option buttons
            char[] prefixes = new[] { 'A', 'B', 'C', 'D' };
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (i < q.options.Length)
                {
                    optionButtons[i].gameObject.SetActive(true);
                    optionButtons[i].interactable = true;

                    Image btnImg = optionButtons[i].GetComponent<Image>();
                    if (btnImg != null) btnImg.color = new Color32(0x13, 0x38, 0x6E, 0xFF);

                    if (optionTexts != null && i < optionTexts.Length)
                    {
                        optionTexts[i].text = $"<b>[{prefixes[i]}]</b>  {q.options[i]}";
                        optionTexts[i].color = Color.white;
                    }
                }
                else
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnOptionSelected(int selectedIndex)
        {
            if (!isGameActive || isCompleted || isAnsweringLocked) return;
            if (currentQuestionIndex >= sessionQuestions.Count) return;

            isAnsweringLocked = true;
            QuizQuestion q = sessionQuestions[currentQuestionIndex];
            bool isCorrect = (selectedIndex == q.correctIndex);

            // Highlight chosen button
            Image chosenBtnImg = optionButtons[selectedIndex].GetComponent<Image>();
            if (isCorrect)
            {
                correctAnswersCount++;
                if (chosenBtnImg != null) chosenBtnImg.color = new Color32(0x00, 0xC8, 0x53, 0xFF);
                if (feedbackText != null)
                {
                    feedbackText.text = $"Correct! {q.explanation}";
                    feedbackText.color = new Color32(0x00, 0xE6, 0x76, 0xFF);
                }
            }
            else
            {
                if (chosenBtnImg != null) chosenBtnImg.color = new Color32(0xD5, 0x00, 0x00, 0xFF);
                // Highlight correct button in green
                Image correctBtnImg = optionButtons[q.correctIndex].GetComponent<Image>();
                if (correctBtnImg != null) correctBtnImg.color = new Color32(0x00, 0xC8, 0x53, 0xFF);

                if (feedbackText != null)
                {
                    feedbackText.text = $"Incorrect! {q.explanation}";
                    feedbackText.color = new Color32(0xFF, 0x8A, 0x80, 0xFF);
                }
            }

            if (progressText != null)
            {
                progressText.text = $"Question {currentQuestionIndex + 1} of {sessionQuestions.Count}   |   Score: {correctAnswersCount}/{targetCorrect}";
            }

            // Next question after delay
            StartCoroutine(AdvanceQuestionRoutine());
        }

        private IEnumerator AdvanceQuestionRoutine()
        {
            yield return new WaitForSeconds(2.0f);
            currentQuestionIndex++;

            if (correctAnswersCount >= targetCorrect)
            {
                TriggerVictory();
            }
            else
            {
                DisplayCurrentQuestion();
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
                    victoryMessage.text = $"Brilliant Mind!\nYou scored {correctAnswersCount}/{targetCorrect} in the Library Quiz!\n+500 Savings Reward & Task Completed!";
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
