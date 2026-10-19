using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RupeeRanger.Core;
using RupeeRanger.Data;
using RupeeRanger.MiniGames;

namespace RupeeRanger.Town
{
    public class TownHUD : MonoBehaviour
    {
        public static TownHUD Instance { get; private set; }

        [Header("Top Header Bar")]
        public TMP_Text dayText;
        public TMP_Text moneyText;
        public TMP_Text debtText;
        public TMP_Text scoreText;

        [Header("Sub Header Bar")]
        public TMP_Text chapterText;
        public TMP_Text objectiveText;

        [Header("Helper Hint")]
        public GameObject hintObject;
        public TMP_Text hintText;

        [Header("Building Prompt")]
        public GameObject promptContainer;
        public TMP_Text promptText;

        [Header("Transaction Feed")]
        public GameObject transactionPanel;
        public TMP_Text transactionFeedText;

        [Header("Next Day Button")]
        public Button nextDayButton;
        public TMP_Text nextDayButtonText;
        public TMP_Text nextDayTooltipText;

        [Header("Touch Controls")]
        public GameObject touchControlsContainer;
        public Button touchInteractButton;

        [Header("Minimap")]
        public TownMinimap minimap;

        [Header("Building Entry Modal Dialog")]
        public GameObject buildingModal;
        public Image modalIcon;
        public TMP_Text modalTitle;
        public TMP_Text modalDescription;
        public TMP_Text modalActionNote;
        public Button modalCloseButton;
        public Button modalPlayButton;

        [Header("Mini-Game Task Modal")]
        public GameObject miniGameModal;
        public Image miniGameIcon;
        public TMP_Text miniGameTitle;
        public TMP_Text miniGameTaskName;
        public TMP_Text miniGameDescription;
        public TMP_Text miniGameRewardText;
        public Button miniGameCompleteButton;
        public Button miniGameCancelButton;

        [Header("Event Day Modal")]
        public GameObject eventDayModal;
        public TMP_Text eventBadgeText;
        public TMP_Text eventTitle;
        public TMP_Text eventDescription;
        public Button eventResolveButton;
        public TMP_Text eventResolveButtonText;
        public Button eventChoice1Button;
        public TMP_Text eventChoice1Text;
        public Button eventChoice2Button;
        public TMP_Text eventChoice2Text;
        public TMP_Text eventFeedbackText;

        [Header("Deficit Alert Modal (Loan System)")]
        public GameObject deficitAlertModal;
        public TMP_Text deficitBadgeText;
        public TMP_Text deficitSubtitleText;
        public TMP_Text deficitCoverText;
        public TMP_Text deficitReliefText;
        public TMP_Text deficitSurchargeText;
        public TMP_Text deficitInterestText;
        public TMP_Text deficitTotalDebtText;
        public TMP_Text deficitCreditImpactText;
        public Button deficitAcceptButton;

        [Header("Bank Loan Repayment Modal")]
        public GameObject bankLoanModal;
        public TMP_Text bankLoanTitleText;
        public TMP_Text bankLoanDescText;
        public TMP_Text bankDebtText;
        public TMP_Text bankSavingsText;
        public Button bankRepayFullButton;
        public TMP_Text bankRepayFullButtonText;
        public Button bankRepayInstallmentButton;
        public TMP_Text bankRepayInstallmentButtonText;
        public Button bankCloseButton;

        [Header("Floating Score Feedback")]
        public TMP_Text floatingScoreText;

        public bool IsAnyModalOpen =>
            (eventDayModal != null && eventDayModal.activeSelf) ||
            (buildingModal != null && buildingModal.activeSelf) ||
            (miniGameModal != null && miniGameModal.activeSelf) ||
            (deficitAlertModal != null && deficitAlertModal.activeSelf) ||
            (bankLoanModal != null && bankLoanModal.activeSelf);

        private TownBuilding activePromptBuilding = null;
        private TownBuilding openedBuilding = null;
        private bool isNextDayActive = false;
        private TaskData currentModalTask = null;
        private TownBuilding currentModalBuilding = null;

        private void Awake()
        {
            Instance = this;
            RemoveSurpriseEventButtons();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void RemoveSurpriseEventButtons()
        {
            // The game does NOT have a button for surprise events. Events happen suddenly during gameplay.
            var allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            foreach (var btn in allButtons)
            {
                if (btn == null) continue;
                string btnName = btn.gameObject.name.ToLower();
                if (btnName.Contains("randomevent") || btnName.Contains("surprise") || btnName.Contains("eventbutton"))
                {
                    btn.gameObject.SetActive(false);
                    Destroy(btn.gameObject);
                }
            }
        }

        private void Start()
        {
            RemoveSurpriseEventButtons();
            EnsurePhase6ModalsExist();
            InitializeHUD();
            UpdateFinancials();
            UpdateTransactionFeed();

            if (promptContainer != null) promptContainer.SetActive(false);
            if (buildingModal != null) buildingModal.SetActive(false);
            if (miniGameModal != null) miniGameModal.SetActive(false);
            if (eventDayModal != null) eventDayModal.SetActive(false);
            if (deficitAlertModal != null) deficitAlertModal.SetActive(false);
            if (bankLoanModal != null) bankLoanModal.SetActive(false);
            if (nextDayTooltipText != null) nextDayTooltipText.gameObject.SetActive(false);
            if (floatingScoreText != null) floatingScoreText.gameObject.SetActive(false);

            EnableNextDayButton(false);

            if (nextDayButton != null)
            {
                nextDayButton.onClick.RemoveAllListeners();
                nextDayButton.onClick.AddListener(OnNextDayClicked);
            }

            if (touchInteractButton != null)
            {
                touchInteractButton.onClick.RemoveAllListeners();
                touchInteractButton.onClick.AddListener(OnTouchInteractClicked);
            }

            if (modalCloseButton != null)
            {
                modalCloseButton.onClick.RemoveAllListeners();
                modalCloseButton.onClick.AddListener(CloseBuildingDialog);
            }

            if (modalPlayButton != null)
            {
                modalPlayButton.onClick.RemoveAllListeners();
                modalPlayButton.onClick.AddListener(() =>
                {
                    var b = openedBuilding;
                    CloseBuildingDialog();
                    if (b != null && b.IsGameHouse())
                    {
                        TaskData t = (DayManager.Instance != null) ? DayManager.Instance.currentDayTask : null;
                        OpenMiniGameModal(t, b);
                    }
                });
            }

            if (miniGameCompleteButton != null)
            {
                miniGameCompleteButton.onClick.RemoveAllListeners();
                miniGameCompleteButton.onClick.AddListener(() =>
                {
                    var taskToLaunch = currentModalTask;
                    var buildingToLaunch = currentModalBuilding;
                    CloseMiniGameModal();

                    if (buildingToLaunch != null && buildingToLaunch.IsGameHouse())
                    {
                        if (MiniGameManager.Instance != null)
                        {
                            MiniGameManager.Instance.LaunchGameForTask(taskToLaunch, buildingToLaunch);
                        }
                    }
                    else
                    {
                        if (DayManager.Instance != null)
                        {
                            DayManager.Instance.CompleteCurrentTask();
                        }
                    }
                });
            }

            if (miniGameCancelButton != null)
            {
                miniGameCancelButton.onClick.RemoveAllListeners();
                miniGameCancelButton.onClick.AddListener(CloseMiniGameModal);
            }

            if (deficitAcceptButton != null)
            {
                deficitAcceptButton.onClick.RemoveAllListeners();
                deficitAcceptButton.onClick.AddListener(OnAcceptEmergencyLoan);
            }

            if (bankRepayFullButton != null)
            {
                bankRepayFullButton.onClick.RemoveAllListeners();
                bankRepayFullButton.onClick.AddListener(OnRepayFullLoan);
            }

            if (bankRepayInstallmentButton != null)
            {
                bankRepayInstallmentButton.onClick.RemoveAllListeners();
                bankRepayInstallmentButton.onClick.AddListener(OnRepayInstallment);
            }

            if (bankCloseButton != null)
            {
                bankCloseButton.onClick.RemoveAllListeners();
                bankCloseButton.onClick.AddListener(CloseBankLoanDialog);
            }
        }

        private void Update()
        {
            UpdateNavigationDisplay();

            // Keyboard shortcut [L] in Editor to simulate deficit and test the loan popup immediately!
#if UNITY_EDITOR || DEVELOPMENT_BUILD
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.lKey.wasPressedThisFrame)
            {
                SimulateDeficitForTesting();
            }
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                Debug.Log("[TownHUD] Debug key [R] pressed: Transitioning to Result scene...");
                UnityEngine.SceneManagement.SceneManager.LoadScene("Result");
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.L)) SimulateDeficitForTesting();
                if (Input.GetKeyDown(KeyCode.R)) UnityEngine.SceneManagement.SceneManager.LoadScene("Result");
            }
            catch {}
#endif
#endif

            // Close open modals with Escape
            bool escapePressed = false;
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) escapePressed = true;
#elif ENABLE_LEGACY_INPUT_MANAGER
            try { if (Input.GetKeyDown(KeyCode.Escape)) escapePressed = true; } catch {}
#endif

            if (escapePressed)
            {
                if (miniGameModal != null && miniGameModal.activeSelf) CloseMiniGameModal();
                else if (buildingModal != null && buildingModal.activeSelf) CloseBuildingDialog();
                else if (bankLoanModal != null && bankLoanModal.activeSelf) CloseBankLoanDialog();
                else if (eventDayModal != null && eventDayModal.activeSelf) CloseEventDayModal();
            }
        }

        private void SimulateDeficitForTesting()
        {
            if (GameManager.Instance != null)
            {
                Debug.Log("[TownHUD] Simulating deficit (-Rs. 90) for testing loan system...");
                GameManager.Instance.savings = -90;
                UpdateFinancials();
                CheckAndShowDeficitAlert();
            }
        }

        private void UpdateNavigationDisplay()
        {
            if (objectiveText == null || DayManager.Instance == null) return;

            var dm = DayManager.Instance;
            if (dm.currentDayTask == null) return;

            if (dm.currentDayTask.isEventDay)
            {
                if (dm.isEventResolved)
                {
                    objectiveText.text = $"[RESOLVED] {dm.currentDayTask.objectiveText.Replace("◆ Event: ", "").Replace("◆ Special Event: ", "")}";
                }
                else
                {
                    objectiveText.text = $"◆ {dm.currentDayTask.objectiveText.Replace("◆ Event: ", "").Replace("◆ Special Event: ", "")}";
                }
                return;
            }

            if (dm.isTaskCompleted)
            {
                string objName = dm.currentDayTask.objectiveText.Replace("* Objective: ", "").Replace("Objective: ", "");
                objectiveText.text = $"[DONE] {objName}";
                return;
            }

            if (dm.currentTargetBuilding != null && dm.playerTransform != null)
            {
                Vector3 toTarget = dm.currentTargetBuilding.transform.position - dm.playerTransform.position;
                toTarget.z = 0f;
                float distance = toTarget.magnitude;

                string dirStr = GetCardinalDirection(toTarget);
                string bldName = dm.currentTargetBuilding.buildingName;

                if (distance <= 2.2f)
                {
                    objectiveText.text = $"* [🎯 HERE! Press E] Enter {bldName}";
                }
                else
                {
                    objectiveText.text = $"* [{dirStr} • {Mathf.RoundToInt(distance)}m] Go to {bldName}";
                }
            }
        }

        private string GetCardinalDirection(Vector3 v)
        {
            float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;

            if (angle >= 337.5f || angle < 22.5f) return "East";
            if (angle >= 22.5f && angle < 67.5f) return "North-East";
            if (angle >= 67.5f && angle < 112.5f) return "North";
            if (angle >= 112.5f && angle < 157.5f) return "North-West";
            if (angle >= 157.5f && angle < 202.5f) return "West";
            if (angle >= 202.5f && angle < 247.5f) return "South-West";
            if (angle >= 247.5f && angle < 292.5f) return "South";
            return "South-East";
        }

        public void InitializeHUD()
        {
            if (GameManager.Instance != null && GameManager.Instance.selectedCharacter != null)
            {
                // Can bind character portrait if assigned
            }
        }

        public void UpdateFinancials()
        {
            if (GameManager.Instance != null)
            {
                int savings = GameManager.Instance.savings;
                int debt = GameManager.Instance.debt;
                int score = GameManager.Instance.score;
                int currentDay = GameManager.Instance.currentDay;

                if (dayText != null) dayText.text = $"DAY {currentDay} / 30";

                if (moneyText != null)
                {
                    if (savings < 0)
                    {
                        moneyText.text = $"-Rs. {Mathf.Abs(savings):N0}";
                        moneyText.color = new Color32(0xFF, 0x3B, 0x30, 0xFF); // Red alert
                    }
                    else
                    {
                        moneyText.text = $"Rs. {savings:N0}";
                        moneyText.color = new Color32(0x2D, 0xFF, 0xAA, 0xFF); // Vibrant green
                    }
                }

                if (debtText != null)
                {
                    debtText.text = $"Debt: Rs. {debt:N0}";
                    debtText.color = (debt > 0) ? new Color32(0xFF, 0x52, 0x52, 0xFF) : new Color32(0xD5, 0xF9, 0xEE, 0xFF);
                }

                if (scoreText != null) scoreText.text = $"Score: {score}";
            }
            else
            {
                if (dayText != null) dayText.text = "DAY 1 / 30";
                if (moneyText != null) moneyText.text = "Rs. 3,500";
                if (debtText != null) debtText.text = "Debt: Rs. 0";
                if (scoreText != null) scoreText.text = "Score: 1000";
            }
        }

        public void UpdateTransactionFeed()
        {
            if (transactionFeedText == null) return;

            StringBuilder sb = new StringBuilder();
            if (GameManager.Instance != null && GameManager.Instance.transactionHistory.Count > 0)
            {
                int maxLines = 7;
                int count = GameManager.Instance.transactionHistory.Count;
                int startIdx = Mathf.Max(0, count - maxLines);

                for (int i = count - 1; i >= startIdx; i--)
                {
                    var rec = GameManager.Instance.transactionHistory[i];
                    string sign = rec.amount >= 0 ? "+" : "-";
                    string colorHex = rec.amount >= 0 ? "#00E676" : "#FF5252";
                    sb.AppendLine($"[Day {rec.day}] {rec.description} (<color={colorHex}>{sign}Rs. {Mathf.Abs(rec.amount):N0}</color>)");
                }
            }
            else
            {
                sb.AppendLine("[Day 1] Initial Budget Savings Deposited (<color=#00E676>+Rs. 3,500</color>)");
            }

            transactionFeedText.text = sb.ToString().TrimEnd();
        }

        #region Floating Score Feedback

        public void ShowScoreFloatingFeedback(int deltaScore)
        {
            if (deltaScore == 0) return;
            if (floatingScoreText == null)
            {
                EnsureFloatingScoreTextExists();
            }

            if (floatingScoreText != null)
            {
                StopCoroutine(nameof(AnimateScoreFloating));
                StartCoroutine(AnimateScoreFloating(deltaScore));
            }
        }

        private IEnumerator AnimateScoreFloating(int delta)
        {
            floatingScoreText.gameObject.SetActive(true);
            string prefix = delta > 0 ? "+" : "";
            floatingScoreText.text = $"{prefix}{delta} pts";
            floatingScoreText.color = delta > 0 ? new Color32(0x00, 0xE6, 0x76, 0xFF) : new Color32(0xFF, 0x52, 0x52, 0xFF);

            RectTransform rt = floatingScoreText.rectTransform;
            Vector2 startPos = new Vector2(300f, -48f);
            Vector2 targetPos = startPos + new Vector2(0f, 38f);

            float elapsed = 0f;
            float duration = 1.35f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                Color c = floatingScoreText.color;
                c.a = Mathf.Lerp(1f, 0f, t * t);
                floatingScoreText.color = c;
                yield return null;
            }

            floatingScoreText.gameObject.SetActive(false);
        }

        #endregion

        #region Deficit & Loan System

        public void CheckAndShowDeficitAlert()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.savings < 0)
            {
                int deficit = Mathf.Abs(GameManager.Instance.savings);
                int approvedRelief = 2000;
                int surcharge = 100;
                int totalRepaymentDebt = 2200; // Rs. 2,000 relief + Rs. 100 surcharge + 5% compounding interest margin

                if (deficitAlertModal == null)
                {
                    EnsurePhase6ModalsExist();
                }

                if (deficitCoverText != null) deficitCoverText.text = $"Deficit to Cover: <color=#FF5252>-Rs. {deficit:N0}</color>";
                if (deficitReliefText != null) deficitReliefText.text = $"Approved Cash Relief: <color=#00E676>+Rs. {approvedRelief:N0}</color>";
                if (deficitSurchargeText != null) deficitSurchargeText.text = $"Processing Surcharge: Rs. {surcharge:N0}";
                if (deficitInterestText != null) deficitInterestText.text = "Monthly Interest Rate: 5% Compounding";
                if (deficitTotalDebtText != null) deficitTotalDebtText.text = $"Total Repayment Debt: <color=#FFB300>Rs. {totalRepaymentDebt:N0}</color>";
                if (deficitCreditImpactText != null) deficitCreditImpactText.text = "Credit Rating Impact: <color=#FF5252>-60 Points</color>";

                if (deficitAlertModal != null)
                {
                    deficitAlertModal.SetActive(true);
                }
            }
        }

        public void OnAcceptEmergencyLoan()
        {
            int approvedRelief = 2000;
            int totalRepaymentDebt = 2200;
            int creditScorePenalty = -60;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.savings += approvedRelief;
                GameManager.Instance.debt += totalRepaymentDebt;
                GameManager.Instance.totalLoanTaken += approvedRelief;
                GameManager.Instance.score = Mathf.Max(0, GameManager.Instance.score + creditScorePenalty);
                GameManager.Instance.AddTransaction(GameManager.Instance.currentDay, $"Emergency Student Loan (+Rs. {approvedRelief:N0} with Rs. {totalRepaymentDebt:N0} debt)", approvedRelief, "Loan");
            }

            ShowScoreFloatingFeedback(creditScorePenalty);
            UpdateFinancials();
            UpdateTransactionFeed();

            if (deficitAlertModal != null)
            {
                deficitAlertModal.SetActive(false);
            }

            SetHintText("Emergency micro-loan accepted. Visit the State Bank of Rupee to repay your debt!");
        }

        #endregion

        #region Bank Loan Repayment

        public void OpenBankLoanDialog()
        {
            if (bankLoanModal == null)
            {
                EnsurePhase6ModalsExist();
            }

            int debt = GameManager.Instance != null ? GameManager.Instance.debt : 0;
            int savings = GameManager.Instance != null ? GameManager.Instance.savings : 0;

            if (bankDebtText != null) bankDebtText.text = $"Outstanding Debt: <color=#FF5252>Rs. {debt:N0}</color>";
            if (bankSavingsText != null) bankSavingsText.text = $"Available Savings: <color=#00E676>Rs. {savings:N0}</color>";

            if (bankRepayFullButton != null)
            {
                bool canRepayFull = savings >= debt && debt > 0;
                bankRepayFullButton.interactable = canRepayFull;
                if (bankRepayFullButtonText != null)
                {
                    bankRepayFullButtonText.text = $"Repay Full Loan (-Rs. {debt:N0}) [+50 pts]";
                }
            }

            if (bankRepayInstallmentButton != null)
            {
                int installment = 1000;
                bool canPayPart = savings >= installment && debt > installment;
                bankRepayInstallmentButton.interactable = canPayPart;
                if (bankRepayInstallmentButtonText != null)
                {
                    bankRepayInstallmentButtonText.text = $"Pay Installment (-Rs. 1,000) [+25 pts]";
                }
            }

            if (bankLoanModal != null)
            {
                bankLoanModal.SetActive(true);
            }
        }

        public void CloseBankLoanDialog()
        {
            if (bankLoanModal != null)
            {
                bankLoanModal.SetActive(false);
            }
        }

        public void OnRepayFullLoan()
        {
            if (GameManager.Instance == null) return;
            int debtToRepay = GameManager.Instance.debt;
            if (debtToRepay <= 0 || GameManager.Instance.savings < debtToRepay) return;

            GameManager.Instance.savings -= debtToRepay;
            GameManager.Instance.debt = 0;
            GameManager.Instance.playerRepaidLoanEarly = true;
            GameManager.Instance.score += 50;

            GameManager.Instance.AddTransaction(GameManager.Instance.currentDay, $"Bank Loan Repaid in Full (-Rs. {debtToRepay:N0})", -debtToRepay, "Debt Repayment");

            ShowScoreFloatingFeedback(50);
            UpdateFinancials();
            UpdateTransactionFeed();
            CloseBankLoanDialog();

            SetHintText("Awesome! You repaid your loan in full! +50 Credit Score awarded.");
        }

        public void OnRepayInstallment()
        {
            if (GameManager.Instance == null) return;
            int installment = 1000;
            if (GameManager.Instance.savings < installment || GameManager.Instance.debt < installment) return;

            GameManager.Instance.savings -= installment;
            GameManager.Instance.debt -= installment;
            GameManager.Instance.score += 25;

            GameManager.Instance.AddTransaction(GameManager.Instance.currentDay, $"Bank Loan Partial Installment (-Rs. {installment:N0})", -installment, "Debt Repayment");

            ShowScoreFloatingFeedback(25);
            UpdateFinancials();
            UpdateTransactionFeed();
            CloseBankLoanDialog();

            SetHintText("Great discipline! Loan reduced by Rs. 1,000. +25 Score awarded.");
        }

        #endregion

        #region Event Day Modal

        public void OpenEventDayModal(TaskData task)
        {
            if (eventDayModal == null) return;

            if (RandomEventManager.Instance != null && task != null)
            {
                EventData ev = RandomEventManager.Instance.GetEventById(task.miniGameId);
                if (ev != null)
                {
                    LifestyleType lf = GameManager.Instance != null ? GameManager.Instance.selectedLifestyle : LifestyleType.Middle;
                    int scaled = ev.GetScaledAmount(lf);
                    OpenEventModal(ev, scaled, (choice) =>
                    {
                        if (DayManager.Instance != null)
                        {
                            DayManager.Instance.ResolveCurrentEvent();
                        }
                    });
                    return;
                }
            }

            // Fallback default event
            if (eventBadgeText != null)
            {
                eventBadgeText.text = "⚠ SUDDEN EMERGENCY";
                eventBadgeText.color = new Color32(0xFF, 0x3B, 0x30, 0xFF);
            }
            if (eventTitle != null) eventTitle.text = (task != null) ? task.objectiveText.Replace("◆ Event: ", "").Replace("◆ Special Event: ", "") : "Unexpected Event";
            if (eventDescription != null) eventDescription.text = (task != null) ? task.description : "An unexpected financial event has occurred!";
            if (eventFeedbackText != null) eventFeedbackText.text = "";

            if (eventChoice1Button != null) eventChoice1Button.gameObject.SetActive(false);
            if (eventChoice2Button != null) eventChoice2Button.gameObject.SetActive(false);

            if (eventResolveButton != null)
            {
                eventResolveButton.gameObject.SetActive(true);
                eventResolveButton.interactable = true;
                if (eventResolveButtonText != null) eventResolveButtonText.text = "RESOLVE EVENT";
                eventResolveButton.onClick.RemoveAllListeners();
                eventResolveButton.onClick.AddListener(() =>
                {
                    if (DayManager.Instance != null)
                    {
                        DayManager.Instance.ResolveCurrentEvent();
                    }
                    CloseEventDayModal();
                });
            }

            eventDayModal.SetActive(true);
        }

        public void OpenEventModal(EventData ev, int scaledAmount, Action<EventChoice> onResolved)
        {
            if (eventDayModal == null || ev == null) return;

            if (eventBadgeText != null)
            {
                eventBadgeText.text = ev.GetCategoryBadgeText();
                if (ev.category == EventCategory.Emergency)
                {
                    eventBadgeText.color = new Color32(0xFF, 0x3B, 0x30, 0xFF); // Red badge
                }
                else if (ev.category == EventCategory.Windfall)
                {
                    eventBadgeText.color = new Color32(0x00, 0xE6, 0x76, 0xFF); // Green badge
                }
                else
                {
                    eventBadgeText.color = new Color32(0x00, 0xE5, 0xFF, 0xFF); // Cyan badge
                }
            }

            if (eventTitle != null) eventTitle.text = ev.title;
            if (eventDescription != null) eventDescription.text = ev.description;
            if (eventFeedbackText != null) eventFeedbackText.text = "";

            SetHintText("Unexpected Event! Make your decision.");

            if (!ev.hasChoices)
            {
                // Single mandatory pay or claim button (matches Screenshot 1)
                if (eventChoice1Button != null) eventChoice1Button.gameObject.SetActive(false);
                if (eventChoice2Button != null) eventChoice2Button.gameObject.SetActive(false);

                if (eventResolveButton != null)
                {
                    eventResolveButton.gameObject.SetActive(true);
                    eventResolveButton.interactable = true;

                    Image resImg = eventResolveButton.GetComponent<Image>();
                    if (eventResolveButtonText == null)
                    {
                        eventResolveButtonText = eventResolveButton.GetComponentInChildren<TMP_Text>();
                    }

                    if (scaledAmount < 0)
                    {
                        if (eventResolveButtonText != null)
                            eventResolveButtonText.text = $"PAY MANDATORY EXPENSE (-Rs. {Mathf.Abs(scaledAmount):N0})";
                        if (resImg != null) resImg.color = new Color32(0xD3, 0x2F, 0x2F, 0xFF); // Deep Red
                    }
                    else
                    {
                        if (eventResolveButtonText != null)
                            eventResolveButtonText.text = $"CLAIM WINDFALL (+Rs. {scaledAmount:N0})";
                        if (resImg != null) resImg.color = new Color32(0x00, 0xC8, 0x53, 0xFF); // Green
                    }

                    eventResolveButton.onClick.RemoveAllListeners();
                    eventResolveButton.onClick.AddListener(() =>
                    {
                        eventResolveButton.interactable = false;
                        EventChoice choice = new EventChoice
                        {
                            choiceText = (scaledAmount < 0) ? "Mandatory Payment Cleared" : "Windfall Claimed",
                            rupeeChange = scaledAmount,
                            scoreChange = (scaledAmount > 0) ? 30 : 15,
                            feedbackMessage = (scaledAmount < 0) ? "Mandatory expense settled responsibly!" : "Windfall credited to savings!"
                        };
                        HandleEventChoiceSelected(ev, choice, onResolved);
                    });
                }
            }
            else
            {
                // Flexible 2-choice decision
                if (eventResolveButton != null) eventResolveButton.gameObject.SetActive(false);

                if (eventChoice1Button != null)
                {
                    eventChoice1Button.gameObject.SetActive(true);
                    eventChoice1Button.interactable = true;
                    if (eventChoice1Text != null && ev.choiceA != null)
                    {
                        string sign = ev.choiceA.rupeeChange > 0 ? "+" : "";
                        string rText = ev.choiceA.rupeeChange != 0 ? $"{sign}Rs. {ev.choiceA.rupeeChange}" : "Rs. 0";
                        string sText = ev.choiceA.scoreChange != 0 ? $" | +{ev.choiceA.scoreChange} pts" : "";
                        eventChoice1Text.text = $"{ev.choiceA.choiceText}\n<size=14><color=#FFD700>({rText}{sText})</color></size>";
                    }
                    eventChoice1Button.onClick.RemoveAllListeners();
                    eventChoice1Button.onClick.AddListener(() =>
                    {
                        HandleEventChoiceSelected(ev, ev.choiceA, onResolved);
                    });
                }

                if (eventChoice2Button != null)
                {
                    eventChoice2Button.gameObject.SetActive(true);
                    eventChoice2Button.interactable = true;
                    if (eventChoice2Text != null && ev.choiceB != null)
                    {
                        string sign = ev.choiceB.rupeeChange > 0 ? "+" : "";
                        string rText = ev.choiceB.rupeeChange != 0 ? $"{sign}Rs. {ev.choiceB.rupeeChange}" : "Rs. 0";
                        string sText = ev.choiceB.scoreChange != 0 ? $" | {ev.choiceB.scoreChange} pts" : "";
                        eventChoice2Text.text = $"{ev.choiceB.choiceText}\n<size=14><color=#FFD700>({rText}{sText})</color></size>";
                    }
                    eventChoice2Button.onClick.RemoveAllListeners();
                    eventChoice2Button.onClick.AddListener(() =>
                    {
                        HandleEventChoiceSelected(ev, ev.choiceB, onResolved);
                    });
                }
            }

            eventDayModal.SetActive(true);
        }

        private void HandleEventChoiceSelected(EventData ev, EventChoice choice, Action<EventChoice> onResolved)
        {
            if (eventChoice1Button != null) eventChoice1Button.interactable = false;
            if (eventChoice2Button != null) eventChoice2Button.interactable = false;
            if (eventResolveButton != null) eventResolveButton.interactable = false;

            if (eventFeedbackText != null)
            {
                eventFeedbackText.text = choice.feedbackMessage;
                eventFeedbackText.color = new Color32(0x00, 0xE6, 0x76, 0xFF);
            }

            onResolved?.Invoke(choice);
            UpdateFinancials();
            UpdateTransactionFeed();

            Invoke(nameof(CloseEventDayModal), 1.6f);
        }

        public void CloseEventDayModal()
        {
            if (eventDayModal != null)
            {
                eventDayModal.SetActive(false);
            }
        }

        #endregion

        #region Building & Mini-Game Dialogs

        public void ShowBuildingPrompt(TownBuilding building)
        {
            activePromptBuilding = building;
            if (promptContainer != null && promptText != null)
            {
                promptText.text = $"[E] Enter\n{building.buildingName}";
                promptContainer.SetActive(true);
            }
        }

        public void HideBuildingPrompt(TownBuilding building)
        {
            if (activePromptBuilding == building)
            {
                activePromptBuilding = null;
                if (promptContainer != null) promptContainer.SetActive(false);
            }
        }

        public void OpenBuildingDialog(TownBuilding building)
        {
            if (buildingModal == null || building == null) return;
            openedBuilding = building;

            if (modalTitle != null) modalTitle.text = building.buildingName;
            if (modalDescription != null) modalDescription.text = building.description;

            if (modalIcon != null)
            {
                modalIcon.sprite = building.buildingIcon;
                modalIcon.gameObject.SetActive(building.buildingIcon != null);
            }

            if (modalActionNote != null)
            {
                switch (building.buildingType)
                {
                    case BuildingType.WordSearchHouse:
                        modalActionNote.text = "Find financial terms hidden across the puzzle matrix!";
                        break;
                    case BuildingType.WordFinderHouse:
                        modalActionNote.text = "Unscramble letter anagrams to build valid financial words!";
                        break;
                    case BuildingType.MathsSpeedHouse:
                        modalActionNote.text = "Beat the rapid mental math clock and score points!";
                        break;
                    case BuildingType.MathsPuzzleHouse:
                        modalActionNote.text = "Solve arithmetic equation patterns and operator puzzles to win!";
                        break;
                    case BuildingType.GameCentre:
                        modalActionNote.text = "Flip and match financial concept card pairs at the Game Arcade!";
                        break;
                    case BuildingType.Library:
                        modalActionNote.text = "Answer 2 fun financial literacy questions to complete today's task!";
                        break;
                    case BuildingType.Bank:
                        if (GameManager.Instance != null && GameManager.Instance.debt > 0)
                        {
                            modalActionNote.text = $"<color=#FF5252>Outstanding Student Debt: Rs. {GameManager.Instance.debt:N0}</color>\nManage your loan or repay early to boost your credit rating!";
                        }
                        else
                        {
                            modalActionNote.text = "State Bank of Rupee. No outstanding debt! Your savings are securely compounding.";
                        }
                        break;
                    default:
                        modalActionNote.text = "Take a walk, explore town, and discover everyday financial choices.";
                        break;
                }
            }

            if (modalPlayButton != null)
            {
                bool isGame = building.IsGameHouse();
                modalPlayButton.gameObject.SetActive(isGame);
            }

            buildingModal.SetActive(true);
        }

        public void CloseBuildingDialog()
        {
            if (buildingModal != null)
            {
                buildingModal.SetActive(false);
            }
            openedBuilding = null;
        }

        public void OpenVisitTaskModal(TaskData task, TownBuilding building)
        {
            if (miniGameModal == null) return;

            currentModalTask = task;
            currentModalBuilding = building;

            if (miniGameTitle != null) miniGameTitle.text = $"Visit {building.buildingName}";
            if (task != null)
            {
                if (miniGameTaskName != null) miniGameTaskName.text = task.objectiveText.Replace("◆ Objective: ", "").Replace("* Objective: ", "").Replace("Objective: ", "");
                if (miniGameDescription != null) miniGameDescription.text = task.description;
                if (miniGameRewardText != null) miniGameRewardText.text = $"Reward: +Rs. {task.rewardAmount:N0} Savings & +50 Score";
            }

            if (miniGameIcon != null)
            {
                miniGameIcon.sprite = building.buildingIcon;
                miniGameIcon.gameObject.SetActive(building.buildingIcon != null);
            }

            if (miniGameCompleteButton != null)
            {
                var txt = miniGameCompleteButton.GetComponentInChildren<TMP_Text>();
                if (txt != null) txt.text = "Complete Visit Task";
            }

            miniGameModal.SetActive(true);
        }

        public void OpenMiniGameModal(TaskData task, TownBuilding building)
        {
            if (miniGameModal == null) return;

            currentModalTask = task;
            currentModalBuilding = building;

            if (miniGameTitle != null) miniGameTitle.text = building.buildingName;
            if (miniGameIcon != null)
            {
                miniGameIcon.sprite = building.buildingIcon;
                miniGameIcon.gameObject.SetActive(building.buildingIcon != null);
            }

            if (task != null)
            {
                if (miniGameTaskName != null) miniGameTaskName.text = task.objectiveText.Replace("◆ Objective: ", "").Replace("* Objective: ", "").Replace("Objective: ", "");
                if (miniGameDescription != null && !string.IsNullOrEmpty(task.description)) miniGameDescription.text = task.description;
                if (miniGameRewardText != null) miniGameRewardText.text = $"Today's Objective Reward: +Rs. {task.rewardAmount:N0} Savings & +50 Score";
            }
            else
            {
                if (miniGameTaskName != null) miniGameTaskName.text = "Challenge & Skill Practice";
                if (miniGameDescription != null && building != null) miniGameDescription.text = building.description;
                if (miniGameRewardText != null) miniGameRewardText.text = "Win this game to earn +Rs. 500 & complete today's task!";
            }

            if (miniGameCompleteButton != null)
            {
                var txt = miniGameCompleteButton.GetComponentInChildren<TMP_Text>();
                if (txt != null) txt.text = "Play Mini-Game";
            }

            miniGameModal.SetActive(true);
        }

        public void CloseMiniGameModal()
        {
            if (miniGameModal != null)
            {
                miniGameModal.SetActive(false);
            }
            currentModalTask = null;
            currentModalBuilding = null;
        }

        #endregion

        #region Helpers & Tooltips

        public void SetHintText(string text)
        {
            if (hintText != null)
            {
                hintText.text = text;
            }
            if (hintObject != null)
            {
                hintObject.SetActive(!string.IsNullOrEmpty(text));
            }
        }

        public void EnableNextDayButton(bool enable)
        {
            isNextDayActive = enable;
            if (nextDayButton != null)
            {
                nextDayButton.interactable = true;
                Image btnImg = nextDayButton.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = enable ? new Color32(0x18, 0x70, 0xF5, 0xFF) : new Color32(0x13, 0x38, 0x6E, 0xFF);
                }
            }

            if (nextDayButtonText != null)
            {
                nextDayButtonText.color = enable ? Color.white : new Color32(0x80, 0x9B, 0xC4, 0xFF);
            }
        }

        private void OnTouchInteractClicked()
        {
            var player = FindAnyObjectByType<PlayerController2D>();
            if (player != null) player.Interact();
        }

        private void OnNextDayClicked()
        {
            bool canAdvance = isNextDayActive || (DayManager.Instance != null && (DayManager.Instance.isTaskCompleted || DayManager.Instance.isEventResolved));
            if (canAdvance)
            {
                if (DayManager.Instance != null)
                {
                    DayManager.Instance.AdvanceNextDay();
                }
            }
            else
            {
                ShowNextDayTooltip();
            }
        }

        public void ShowNextDayTooltip()
        {
            if (nextDayTooltipText != null)
            {
                nextDayTooltipText.gameObject.SetActive(true);
                CancelInvoke(nameof(HideNextDayTooltip));
                Invoke(nameof(HideNextDayTooltip), 3f);
            }
        }

        private void HideNextDayTooltip()
        {
            if (nextDayTooltipText != null)
            {
                nextDayTooltipText.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Dynamic Fallback UI Construction

        private void EnsurePhase6ModalsExist()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            Transform root = canvas.transform;

            EnsureFloatingScoreTextExists();

            if (deficitAlertModal == null)
            {
                // Look for existing child first
                Transform existing = root.Find("DeficitAlertModal");
                if (existing != null)
                {
                    deficitAlertModal = existing.gameObject;
                    BindDeficitModalComponents(existing);
                }
                else
                {
                    CreateDeficitAlertModal(root);
                }
            }

            if (bankLoanModal == null)
            {
                Transform existingBank = root.Find("BankLoanModal");
                if (existingBank != null)
                {
                    bankLoanModal = existingBank.gameObject;
                    BindBankModalComponents(existingBank);
                }
                else
                {
                    CreateBankLoanModal(root);
                }
            }
        }

        private void EnsureFloatingScoreTextExists()
        {
            if (floatingScoreText != null) return;

            Transform root = transform;
            Transform existing = root.Find("FloatingScoreFeedback");
            if (existing != null)
            {
                floatingScoreText = existing.GetComponent<TMP_Text>();
                return;
            }

            GameObject go = new GameObject("FloatingScoreFeedback", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(root, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(300f, -48f);
            rt.sizeDelta = new Vector2(160f, 36f);

            floatingScoreText = go.GetComponent<TextMeshProUGUI>();
            floatingScoreText.fontSize = 24;
            floatingScoreText.fontStyle = FontStyles.Bold;
            floatingScoreText.alignment = TextAlignmentOptions.Center;
            floatingScoreText.color = new Color32(0x00, 0xE6, 0x76, 0xFF);
            go.SetActive(false);
        }

        private void CreateDeficitAlertModal(Transform root)
        {
            Sprite roundBox = Resources.Load<Sprite>("rounded_box");

            GameObject modalObj = new GameObject("DeficitAlertModal", typeof(RectTransform), typeof(Image));
            modalObj.transform.SetParent(root, false);
            deficitAlertModal = modalObj;

            Image bg = modalObj.GetComponent<Image>();
            bg.color = new Color32(0x16, 0x1B, 0x2E, 0xFD); // Dark slate blue matching screenshot
            if (roundBox != null)
            {
                bg.sprite = roundBox;
                bg.type = Image.Type.Sliced;
            }

            RectTransform rt = modalObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(740f, 440f);

            deficitBadgeText = CreateModalText("Badge", modalObj.transform, "⚠ DEFICIT ALERT: INSUFFICIENT FUNDS!", 18, FontStyles.Bold, new Color32(0xFF, 0x3B, 0x30, 0xFF), new Vector2(0f, 165f), new Vector2(680f, 35f));
            deficitSubtitleText = CreateModalText("Subtitle", modalObj.transform, "Your savings dropped below Rs. 0. An emergency micro-loan has been approved to cover your expenses.", 14, FontStyles.Normal, new Color32(0xEA, 0xEE, 0xF4, 0xFF), new Vector2(0f, 120f), new Vector2(660f, 40f));

            deficitCoverText = CreateModalText("DeficitCover", modalObj.transform, "Deficit to Cover: -Rs. 90", 17, FontStyles.Bold, new Color32(0xFF, 0x52, 0x52, 0xFF), new Vector2(0f, 65f), new Vector2(600f, 30f));
            deficitReliefText = CreateModalText("ApprovedRelief", modalObj.transform, "Approved Cash Relief: +Rs. 2,000", 17, FontStyles.Bold, new Color32(0x00, 0xE6, 0x76, 0xFF), new Vector2(0f, 30f), new Vector2(600f, 30f));
            deficitSurchargeText = CreateModalText("Surcharge", modalObj.transform, "Processing Surcharge: Rs. 100", 16, FontStyles.Normal, Color.white, new Vector2(0f, -5f), new Vector2(600f, 30f));
            deficitInterestText = CreateModalText("Interest", modalObj.transform, "Monthly Interest Rate: 5% Compounding", 16, FontStyles.Normal, Color.white, new Vector2(0f, -40f), new Vector2(600f, 30f));
            deficitTotalDebtText = CreateModalText("TotalDebt", modalObj.transform, "Total Repayment Debt: Rs. 2,200", 18, FontStyles.Bold, new Color32(0xFF, 0xB3, 0x00, 0xFF), new Vector2(0f, -75f), new Vector2(600f, 32f));
            deficitCreditImpactText = CreateModalText("CreditImpact", modalObj.transform, "Credit Rating Impact: -60 Points", 16, FontStyles.Bold, new Color32(0xFF, 0x52, 0x52, 0xFF), new Vector2(0f, -110f), new Vector2(600f, 30f));

            // Accept Button
            GameObject btnObj = new GameObject("AcceptButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(modalObj.transform, false);
            Image btnImg = btnObj.GetComponent<Image>();
            btnImg.color = new Color32(0xD3, 0x2F, 0x2F, 0xFF); // Red matching screenshot
            if (roundBox != null)
            {
                btnImg.sprite = roundBox;
                btnImg.type = Image.Type.Sliced;
            }

            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(460f, 54f);
            btnRt.anchoredPosition = new Vector2(0f, -165f);

            deficitAcceptButton = btnObj.GetComponent<Button>();
            deficitAcceptButton.onClick.AddListener(OnAcceptEmergencyLoan);

            CreateModalText("BtnText", btnObj.transform, "ACCEPT EMERGENCY STUDENT LOAN", 16, FontStyles.Bold, Color.white, Vector2.zero, new Vector2(450f, 50f));

            modalObj.SetActive(false);
        }

        private void BindDeficitModalComponents(Transform root)
        {
            deficitBadgeText = root.Find("Badge")?.GetComponent<TMP_Text>();
            deficitSubtitleText = root.Find("Subtitle")?.GetComponent<TMP_Text>();
            deficitCoverText = root.Find("DeficitCover")?.GetComponent<TMP_Text>();
            deficitReliefText = root.Find("ApprovedRelief")?.GetComponent<TMP_Text>();
            deficitSurchargeText = root.Find("Surcharge")?.GetComponent<TMP_Text>();
            deficitInterestText = root.Find("Interest")?.GetComponent<TMP_Text>();
            deficitTotalDebtText = root.Find("TotalDebt")?.GetComponent<TMP_Text>();
            deficitCreditImpactText = root.Find("CreditImpact")?.GetComponent<TMP_Text>();
            deficitAcceptButton = root.Find("AcceptButton")?.GetComponent<Button>();
            if (deficitAcceptButton != null)
            {
                deficitAcceptButton.onClick.RemoveAllListeners();
                deficitAcceptButton.onClick.AddListener(OnAcceptEmergencyLoan);
            }
        }

        private void CreateBankLoanModal(Transform root)
        {
            Sprite roundBox = Resources.Load<Sprite>("rounded_box");

            GameObject modalObj = new GameObject("BankLoanModal", typeof(RectTransform), typeof(Image));
            modalObj.transform.SetParent(root, false);
            bankLoanModal = modalObj;

            Image bg = modalObj.GetComponent<Image>();
            bg.color = new Color32(0x10, 0x1F, 0x33, 0xFD);
            if (roundBox != null)
            {
                bg.sprite = roundBox;
                bg.type = Image.Type.Sliced;
            }

            RectTransform rt = modalObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(680f, 420f);

            bankLoanTitleText = CreateModalText("Title", modalObj.transform, "🏦 State Bank of Rupee — Loan Desk", 22, FontStyles.Bold, new Color32(0x00, 0xE5, 0xFF, 0xFF), new Vector2(0f, 155f), new Vector2(620f, 35f));
            bankLoanDescText = CreateModalText("Desc", modalObj.transform, "Repaying student debt early prevents daily 5% compounding interest and awards bonus credit score!", 15, FontStyles.Normal, Color.white, new Vector2(0f, 110f), new Vector2(600f, 40f));

            bankDebtText = CreateModalText("Debt", modalObj.transform, "Outstanding Debt: Rs. 0", 20, FontStyles.Bold, new Color32(0xFF, 0x52, 0x52, 0xFF), new Vector2(0f, 45f), new Vector2(580f, 35f));
            bankSavingsText = CreateModalText("Savings", modalObj.transform, "Available Savings: Rs. 0", 18, FontStyles.Normal, new Color32(0x00, 0xE6, 0x76, 0xFF), new Vector2(0f, 5f), new Vector2(580f, 30f));

            // Repay Full Button
            GameObject btnFullObj = new GameObject("RepayFullButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnFullObj.transform.SetParent(modalObj.transform, false);
            Image imgFull = btnFullObj.GetComponent<Image>();
            imgFull.color = new Color32(0x00, 0xC8, 0x53, 0xFF);
            if (roundBox != null) { imgFull.sprite = roundBox; imgFull.type = Image.Type.Sliced; }
            RectTransform rtFull = btnFullObj.GetComponent<RectTransform>();
            rtFull.sizeDelta = new Vector2(480f, 48f);
            rtFull.anchoredPosition = new Vector2(0f, -55f);
            bankRepayFullButton = btnFullObj.GetComponent<Button>();
            bankRepayFullButtonText = CreateModalText("Txt", btnFullObj.transform, "Repay Full Loan [+50 pts]", 16, FontStyles.Bold, Color.white, Vector2.zero, new Vector2(460f, 40f));
            bankRepayFullButton.onClick.AddListener(OnRepayFullLoan);

            // Installment Button
            GameObject btnPartObj = new GameObject("InstallmentButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnPartObj.transform.SetParent(modalObj.transform, false);
            Image imgPart = btnPartObj.GetComponent<Image>();
            imgPart.color = new Color32(0x02, 0x88, 0xD1, 0xFF);
            if (roundBox != null) { imgPart.sprite = roundBox; imgPart.type = Image.Type.Sliced; }
            RectTransform rtPart = btnPartObj.GetComponent<RectTransform>();
            rtPart.sizeDelta = new Vector2(480f, 48f);
            rtPart.anchoredPosition = new Vector2(0f, -112f);
            bankRepayInstallmentButton = btnPartObj.GetComponent<Button>();
            bankRepayInstallmentButtonText = CreateModalText("Txt", btnPartObj.transform, "Pay Installment (-Rs. 1,000) [+25 pts]", 16, FontStyles.Bold, Color.white, Vector2.zero, new Vector2(460f, 40f));
            bankRepayInstallmentButton.onClick.AddListener(OnRepayInstallment);

            // Close Button
            GameObject btnCloseObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnCloseObj.transform.SetParent(modalObj.transform, false);
            Image imgClose = btnCloseObj.GetComponent<Image>();
            imgClose.color = new Color32(0x45, 0x5A, 0x64, 0xFF);
            if (roundBox != null) { imgClose.sprite = roundBox; imgClose.type = Image.Type.Sliced; }
            RectTransform rtClose = btnCloseObj.GetComponent<RectTransform>();
            rtClose.sizeDelta = new Vector2(220f, 42f);
            rtClose.anchoredPosition = new Vector2(0f, -168f);
            bankCloseButton = btnCloseObj.GetComponent<Button>();
            CreateModalText("Txt", btnCloseObj.transform, "Back to Town", 15, FontStyles.Bold, Color.white, Vector2.zero, new Vector2(200f, 38f));
            bankCloseButton.onClick.AddListener(CloseBankLoanDialog);

            modalObj.SetActive(false);
        }

        private void BindBankModalComponents(Transform root)
        {
            bankLoanTitleText = root.Find("Title")?.GetComponent<TMP_Text>();
            bankLoanDescText = root.Find("Desc")?.GetComponent<TMP_Text>();
            bankDebtText = root.Find("Debt")?.GetComponent<TMP_Text>();
            bankSavingsText = root.Find("Savings")?.GetComponent<TMP_Text>();

            bankRepayFullButton = root.Find("RepayFullButton")?.GetComponent<Button>();
            bankRepayFullButtonText = root.Find("RepayFullButton/Txt")?.GetComponent<TMP_Text>();
            if (bankRepayFullButton != null)
            {
                bankRepayFullButton.onClick.RemoveAllListeners();
                bankRepayFullButton.onClick.AddListener(OnRepayFullLoan);
            }

            bankRepayInstallmentButton = root.Find("InstallmentButton")?.GetComponent<Button>();
            bankRepayInstallmentButtonText = root.Find("InstallmentButton/Txt")?.GetComponent<TMP_Text>();
            if (bankRepayInstallmentButton != null)
            {
                bankRepayInstallmentButton.onClick.RemoveAllListeners();
                bankRepayInstallmentButton.onClick.AddListener(OnRepayInstallment);
            }

            bankCloseButton = root.Find("CloseButton")?.GetComponent<Button>();
            if (bankCloseButton != null)
            {
                bankCloseButton.onClick.RemoveAllListeners();
                bankCloseButton.onClick.AddListener(CloseBankLoanDialog);
            }
        }

        private TMP_Text CreateModalText(string name, Transform parent, string text, float size, FontStyles style, Color color, Vector2 pos, Vector2 sizeDelta)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;

            TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            return tmp;
        }

        #endregion
    }
}
