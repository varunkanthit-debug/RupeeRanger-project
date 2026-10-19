using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RupeeRanger.Core;
using RupeeRanger.Data;

namespace RupeeRanger.Town
{
    public class DayManager : MonoBehaviour
    {
        public static DayManager Instance { get; private set; }

        [Header("Day Schedule Data")]
        public List<TaskData> allDays = new List<TaskData>();
        public TextAsset dayPlansJsonAsset;

        [Header("World References")]
        public PlayerController2D player;
        public Transform playerTransform;
        public Vector3 homeSpawnPosition = new Vector3(0.5f, 0.5f, 0f);

        [Header("Guidance & Navigation")]
        public GameObject guideArrow;
        public SpriteRenderer guideArrowRenderer;
        public GameObject destinationBeacon;
        public SpriteRenderer destinationBeaconRenderer;
        public LineRenderer pathLineRenderer;
        public List<TownBuilding> townBuildings = new List<TownBuilding>();

        [Header("State")]
        public int currentDayNumber = 1;
        public TaskData currentDayTask;
        public bool isTaskCompleted = false;
        public bool isEventResolved = false;
        public TownBuilding currentTargetBuilding;

        [Header("Sudden Random Events")]
        public bool hasSuddenEventOccurredToday = false;
        private bool isSuddenEventScheduledToday = false;
        private float suddenEventWalkTimer = 0f;
        private float suddenEventTriggerDelay = 6f;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            FindWorldReferences();
            LoadScheduleIfNeeded();

            int initialDay = 1;
            if (GameManager.Instance != null)
            {
                initialDay = GameManager.Instance.currentDay;
            }

            StartDay(initialDay);
        }

        private void Update()
        {
            UpdateGuideArrow();
            UpdateHighlightedPath();
            CheckSuddenExplorationEvent();
            CheckKeyboardShortcuts();
        }

        private void FindWorldReferences()
        {
            if (player == null)
            {
                player = FindAnyObjectByType<PlayerController2D>();
            }
            if (player != null && playerTransform == null)
            {
                playerTransform = player.transform;
            }

            if (townBuildings == null || townBuildings.Count == 0)
            {
                townBuildings = new List<TownBuilding>(FindObjectsByType<TownBuilding>());
            }
        }

        private void LoadScheduleIfNeeded()
        {
            if (allDays == null || allDays.Count == 0)
            {
                // Try loading from JSON asset if assigned
                if (dayPlansJsonAsset != null)
                {
                    LoadFromJsonText(dayPlansJsonAsset.text);
                }
                else
                {
                    // Try Resources
                    TextAsset resJson = Resources.Load<TextAsset>("DayPlans");
                    if (resJson != null)
                    {
                        LoadFromJsonText(resJson.text);
                    }
                }
            }
        }

        public void LoadFromJsonText(string jsonText)
        {
            try
            {
                string wrappedJson = $"{{\"days\":{jsonText}}}";
                DayScheduleData schedule = JsonUtility.FromJson<DayScheduleData>(wrappedJson);
                if (schedule != null && schedule.days != null)
                {
                    allDays.Clear();
                    foreach (var raw in schedule.days)
                    {
                        TaskData task = ScriptableObject.CreateInstance<TaskData>();
                        task.id = raw.id;
                        task.dayNumber = raw.dayNumber;
                        task.chapterTitle = raw.chapterTitle;
                        task.objectiveText = raw.objectiveText;
                        if (Enum.TryParse(raw.targetBuilding, out BuildingType bType))
                        {
                            task.targetBuilding = bType;
                        }
                        task.miniGameId = raw.miniGameId;
                        task.rewardAmount = raw.rewardAmount;
                        task.isEventDay = raw.isEventDay;
                        task.description = raw.description;
                        task.hintText = raw.hintText;

                        allDays.Add(task);
                    }
                    Debug.Log($"[DayManager] Successfully loaded {allDays.Count} day plans from JSON.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DayManager] Failed to parse day plans JSON: {ex.Message}");
            }
        }

        public void StartDay(int dayNumber)
        {
            if (dayNumber > 30)
            {
                Debug.Log("[DayManager] Day 31 reached! Transitioning to Result scene...");
                SceneManager.LoadScene("Result");
                return;
            }

            currentDayNumber = dayNumber;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.currentDay = currentDayNumber;
            }

            isTaskCompleted = false;
            isEventResolved = false;

            if (TownHUD.Instance != null)
            {
                TownHUD.Instance.EnableNextDayButton(false);
            }

            currentDayTask = GetTaskForDay(currentDayNumber);

            if (currentDayTask != null)
            {
                if (TownHUD.Instance != null)
                {
                    TownHUD.Instance.UpdateFinancials();

                    if (TownHUD.Instance.chapterText != null)
                    {
                        TownHUD.Instance.chapterText.text = currentDayTask.chapterTitle;
                    }

                    if (TownHUD.Instance.objectiveText != null)
                    {
                        TownHUD.Instance.objectiveText.text = currentDayTask.objectiveText;
                    }

                    TownHUD.Instance.SetHintText(currentDayTask.hintText);
                }

                if (currentDayTask.isEventDay)
                {
                    hasSuddenEventOccurredToday = true;
                    isSuddenEventScheduledToday = false;
                    currentTargetBuilding = null;
                    if (guideArrow != null) guideArrow.SetActive(false);
                    if (destinationBeacon != null) destinationBeacon.SetActive(false);
                    if (pathLineRenderer != null) pathLineRenderer.enabled = false;

                    // Automatically open event modal on event days
                    if (TownHUD.Instance != null)
                    {
                        TownHUD.Instance.OpenEventDayModal(currentDayTask);
                    }
                }
                else
                {
                    // Non-event day: assign target building
                    currentTargetBuilding = FindBuilding(currentDayTask.targetBuilding);
                    if (guideArrow != null) guideArrow.SetActive(true);
                    if (pathLineRenderer != null) pathLineRenderer.enabled = true;

                    // Sudden random events can occur on ANY day across the 30-day journey
                    hasSuddenEventOccurredToday = false;
                    suddenEventWalkTimer = 0f;
                    // Day 1 has a guaranteed sudden event in 3.5-6.5 seconds so player experiences it immediately.
                    // Subsequent days (2-30) have a high 75% daily chance.
                    isSuddenEventScheduledToday = (dayNumber == 1 || UnityEngine.Random.value < 0.75f);
                    suddenEventTriggerDelay = (dayNumber == 1) ? UnityEngine.Random.Range(3.5f, 6.5f) : UnityEngine.Random.Range(4.5f, 9.5f);
                }
            }
            else
            {
                Debug.LogWarning($"[DayManager] No task found for day {dayNumber}.");
            }
        }

        private TaskData GetTaskForDay(int day)
        {
            if (allDays != null)
            {
                foreach (var t in allDays)
                {
                    if (t.dayNumber == day) return t;
                }
            }
            return null;
        }

        private TownBuilding FindBuilding(BuildingType type)
        {
            if (townBuildings == null) return null;
            foreach (var b in townBuildings)
            {
                if (b != null && b.buildingType == type) return b;
            }
            return null;
        }

        private void CheckSuddenExplorationEvent()
        {
            if (isSuddenEventScheduledToday && !hasSuddenEventOccurredToday && !isEventResolved)
            {
                // Advance timer while player is in town (full speed when walking, steady speed when exploring)
                float rate = (player != null && player.IsMoving) ? 1.0f : 0.6f;
                suddenEventWalkTimer += Time.deltaTime * rate;

                if (suddenEventWalkTimer >= suddenEventTriggerDelay)
                {
                    if (TownHUD.Instance != null && !TownHUD.Instance.IsAnyModalOpen)
                    {
                        TriggerSuddenRandomEvent();
                    }
                }
            }
        }

        public void TriggerSuddenRandomEvent()
        {
            if (hasSuddenEventOccurredToday) return;
            hasSuddenEventOccurredToday = true;
            isSuddenEventScheduledToday = false;

            if (RandomEventManager.Instance != null)
            {
                Debug.Log($"[DayManager] Sudden random event triggered unexpectedly on Day {currentDayNumber}!");
                RandomEventManager.Instance.TriggerRandomEvent();
            }
        }

        private void UpdateGuideArrow()
        {
            if (guideArrow == null && destinationBeacon == null) return;

            if (!isTaskCompleted && !isEventResolved && currentTargetBuilding != null && playerTransform != null)
            {
                if (guideArrow != null)
                {
                    guideArrow.SetActive(true);
                    Vector3 toTarget = currentTargetBuilding.transform.position - playerTransform.position;
                    Vector3 norm = toTarget.normalized;

                    // Orbit 1.6f units away from player in the direction of the target building
                    float hover = Mathf.Sin(Time.time * 6f) * 0.12f;
                    guideArrow.transform.position = playerTransform.position + (norm * 1.6f) + new Vector3(0f, hover, 0f);

                    // Rotate arrow to point directly at the target
                    float angle = Mathf.Atan2(norm.y, norm.x) * Mathf.Rad2Deg - 90f;
                    guideArrow.transform.rotation = Quaternion.Euler(0f, 0f, angle);

                    // Pulsing scale to make it eye-catching and clear
                    float pulse = 1.35f + Mathf.PingPong(Time.time * 2.5f, 0.35f);
                    guideArrow.transform.localScale = new Vector3(pulse, pulse, 1f);

                    if (guideArrowRenderer != null)
                    {
                        guideArrowRenderer.sortingOrder = 50;
                        guideArrowRenderer.color = new Color32(0xFF, 0xEA, 0x00, 0xFF); // Vibrant glowing gold
                    }
                }

                UpdateDestinationBeacon();
            }
            else
            {
                if (guideArrow != null) guideArrow.SetActive(false);
                if (destinationBeacon != null) destinationBeacon.SetActive(false);
            }
        }

        private void UpdateDestinationBeacon()
        {
            if (currentTargetBuilding == null)
            {
                if (destinationBeacon != null) destinationBeacon.SetActive(false);
                return;
            }

            if (destinationBeacon == null && guideArrowRenderer != null && guideArrowRenderer.sprite != null)
            {
                destinationBeacon = new GameObject("DestinationBeacon", typeof(SpriteRenderer));
                destinationBeaconRenderer = destinationBeacon.GetComponent<SpriteRenderer>();
                destinationBeaconRenderer.sprite = guideArrowRenderer.sprite;
                destinationBeaconRenderer.sortingOrder = 55;
            }

            if (destinationBeacon != null)
            {
                destinationBeacon.SetActive(true);
                float bounce = Mathf.Sin(Time.time * 4f) * 0.25f;
                // Position hovering over building entrance, pointing down
                destinationBeacon.transform.position = currentTargetBuilding.transform.position + new Vector3(0f, 2.7f + bounce, 0f);
                destinationBeacon.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
                float pulse = 1.4f + Mathf.PingPong(Time.time * 2f, 0.3f);
                destinationBeacon.transform.localScale = new Vector3(pulse, pulse, 1f);

                if (destinationBeaconRenderer != null)
                {
                    destinationBeaconRenderer.sortingOrder = 55;
                    destinationBeaconRenderer.color = new Color32(0x00, 0xE5, 0xFF, 0xFF); // Cyan beacon
                }
            }
        }

        private void UpdateHighlightedPath()
        {
            if (pathLineRenderer == null) return;

            if (!isTaskCompleted && !isEventResolved && currentTargetBuilding != null && playerTransform != null)
            {
                pathLineRenderer.enabled = true;
                pathLineRenderer.sortingOrder = 15;
                pathLineRenderer.startWidth = 0.28f;
                pathLineRenderer.endWidth = 0.28f;

                // Grid-based road navigation waypoints:
                // 1. Current player position
                // 2. Nearest point on main East-West Highway (y = 0.5f)
                // 3. Point on highway at target's X column (target.x, 0.5f)
                // 4. Target building entrance position
                Vector3 p = playerTransform.position;
                Vector3 t = currentTargetBuilding.transform.position;

                Vector3 p1 = new Vector3(p.x, p.y, 0f);
                Vector3 p2 = new Vector3(p.x, 0.5f, 0f);
                Vector3 p3 = new Vector3(t.x, 0.5f, 0f);
                Vector3 p4 = new Vector3(t.x, t.y, 0f);

                pathLineRenderer.positionCount = 4;
                pathLineRenderer.SetPosition(0, p1);
                pathLineRenderer.SetPosition(1, p2);
                pathLineRenderer.SetPosition(2, p3);
                pathLineRenderer.SetPosition(3, p4);
            }
            else
            {
                pathLineRenderer.enabled = false;
            }
        }

        private void CheckKeyboardShortcuts()
        {
            bool nextDayPressed = false;

#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.nKey.wasPressedThisFrame)
            {
                nextDayPressed = true;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.N))
                {
                    nextDayPressed = true;
                }
            }
            catch {}
#endif

            if (nextDayPressed)
            {
                if (isTaskCompleted || isEventResolved)
                {
                    AdvanceNextDay();
                }
                else
                {
                    if (TownHUD.Instance != null)
                    {
                        TownHUD.Instance.ShowNextDayTooltip();
                    }
                }
            }
        }

        public void OnPlayerInteractBuilding(TownBuilding building)
        {
            if (building == null) return;

            // If it is a Game House (Maths Speed, Maths Puzzle, Word Search, Word Finder), ALWAYS open the challenge modal with "Play Mini-Game"!
            if (building.IsGameHouse())
            {
                if (TownHUD.Instance != null)
                {
                    TaskData taskForModal = (currentDayTask != null && !currentDayTask.isEventDay) ? currentDayTask : null;
                    TownHUD.Instance.OpenMiniGameModal(taskForModal, building);
                }
                return;
            }

            // Bank building special routing: if player carries active debt, open Loan Repayment desk
            if (building.buildingType == BuildingType.Bank)
            {
                if (GameManager.Instance != null && GameManager.Instance.debt > 0)
                {
                    if (TownHUD.Instance != null)
                    {
                        TownHUD.Instance.OpenBankLoanDialog();
                        return;
                    }
                }
            }

            // Normal buildings (College, Shop, Bank, Hospital, FriendsHouse, BusStop, Home)
            if (currentDayTask != null && !currentDayTask.isEventDay && (building == currentTargetBuilding || currentDayTask.targetBuilding == building.buildingType) && !isTaskCompleted)
            {
                if (TownHUD.Instance != null)
                {
                    TownHUD.Instance.OpenVisitTaskModal(currentDayTask, building);
                }
            }
            else
            {
                if (TownHUD.Instance != null)
                {
                    TownHUD.Instance.OpenBuildingDialog(building);
                }
            }
        }

        public void CompleteCurrentTask()
        {
            if (isTaskCompleted) return;

            isTaskCompleted = true;
            int reward = (currentDayTask != null) ? currentDayTask.rewardAmount : 500;

            if (GameManager.Instance != null)
            {
                string desc = (currentDayTask != null) ? $"{currentDayTask.objectiveText} Reward" : "Task Reward";
                GameManager.Instance.ApplySavingsChange(reward, desc, "Reward");
                GameManager.Instance.score += 50;
                if (TownHUD.Instance != null)
                {
                    TownHUD.Instance.ShowScoreFloatingFeedback(50);
                }
            }

            if (TownHUD.Instance != null)
            {
                TownHUD.Instance.CloseMiniGameModal();
                TownHUD.Instance.UpdateTransactionFeed();
                TownHUD.Instance.UpdateFinancials();

                if (TownHUD.Instance.objectiveText != null)
                {
                    string objName = (currentDayTask != null) ? currentDayTask.objectiveText : "Challenge Completed";
                    TownHUD.Instance.objectiveText.text = $"[DONE] {objName.Replace("* Objective: ", "").Replace("Objective: ", "")}";
                }

                TownHUD.Instance.SetHintText("Task Complete! Return home or press NEXT DAY [N] to advance.");
                TownHUD.Instance.EnableNextDayButton(true);
            }

            if (guideArrow != null) guideArrow.SetActive(false);
            if (destinationBeacon != null) destinationBeacon.SetActive(false);
            if (pathLineRenderer != null) pathLineRenderer.enabled = false;

            Debug.Log($"[DayManager] Day {currentDayNumber} task completed! Reward: Rs. {reward}. Next Day enabled.");

            // If a sudden event was scheduled today and hasn't popped up yet, trigger it unexpectedly as you exit the building!
            if (isSuddenEventScheduledToday && !hasSuddenEventOccurredToday)
            {
                Invoke(nameof(TriggerSuddenRandomEvent), 1.2f);
            }
        }

        public void ResolveCurrentEvent()
        {
            if (isEventResolved) return;

            isEventResolved = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.score += 25;
            }

            if (TownHUD.Instance != null)
            {
                TownHUD.Instance.CloseEventDayModal();
                TownHUD.Instance.UpdateFinancials();

                if (TownHUD.Instance.objectiveText != null && currentDayTask != null)
                {
                    TownHUD.Instance.objectiveText.text = $"[RESOLVED] {currentDayTask.objectiveText.Replace("* Objective: ", "").Replace("Objective: ", "")}";
                }

                TownHUD.Instance.SetHintText("Event handled! Press NEXT DAY [N] to advance.");
                TownHUD.Instance.EnableNextDayButton(true);
            }

            if (guideArrow != null) guideArrow.SetActive(false);
            if (destinationBeacon != null) destinationBeacon.SetActive(false);
            if (pathLineRenderer != null) pathLineRenderer.enabled = false;

            Debug.Log($"[DayManager] Day {currentDayNumber} event resolved! Next Day enabled.");
        }

        public void AdvanceNextDay()
        {
            if (!isTaskCompleted && !isEventResolved)
            {
                Debug.Log("[DayManager] Cannot advance: complete the day's task or event first!");
                if (TownHUD.Instance != null)
                {
                    TownHUD.Instance.ShowNextDayTooltip();
                }
                return;
            }

            int nextDay = currentDayNumber + 1;
            if (nextDay > 30)
            {
                Debug.Log("[DayManager] 30 Days Completed! Loading Result Scene...");
                SceneManager.LoadScene("Result");
                return;
            }

            // Reset player to home spawn
            if (playerTransform == null)
            {
                var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController2D>();
                if (pc != null) playerTransform = pc.transform;
            }

            if (playerTransform != null)
            {
                playerTransform.position = homeSpawnPosition;
                var rb = playerTransform.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }
            if (Camera.main != null)
            {
                Camera.main.transform.position = new Vector3(homeSpawnPosition.x, homeSpawnPosition.y, -10f);
            }

            // Active debt compounding interest: 5% monthly rate compounding daily
            if (GameManager.Instance != null && GameManager.Instance.debt > 0)
            {
                float dailyRate = 0.05f / 30f;
                int interest = Mathf.Max(5, Mathf.RoundToInt(GameManager.Instance.debt * dailyRate));
                GameManager.Instance.debt += interest;
                GameManager.Instance.totalInterestPaid += interest;
                GameManager.Instance.score = Mathf.Max(0, GameManager.Instance.score - 2);
                GameManager.Instance.AddTransaction(currentDayNumber, "Loan Interest Accrued (5% Compounding)", -interest, "Debt");
                if (TownHUD.Instance != null)
                {
                    TownHUD.Instance.ShowScoreFloatingFeedback(-2);
                    TownHUD.Instance.SetHintText($"⚠ Unpaid debt: +Rs. {interest} compounding interest accrued. Visit the Bank to repay!");
                }
            }

            StartDay(nextDay);

            if (TownHUD.Instance != null)
            {
                TownHUD.Instance.CheckAndShowDeficitAlert();
            }
        }
    }
}
