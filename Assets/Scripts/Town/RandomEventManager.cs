using System;
using System.Collections.Generic;
using UnityEngine;
using RupeeRanger.Core;
using RupeeRanger.Data;

namespace RupeeRanger.Town
{
    public class RandomEventManager : MonoBehaviour
    {
        private static RandomEventManager _instance;
        public static RandomEventManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<RandomEventManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("RandomEventManager");
                        _instance = go.AddComponent<RandomEventManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Event Database")]
        public TextAsset eventsJsonAsset;
        private List<EventData> allEvents = new List<EventData>();
        private HashSet<string> seenEventIds = new HashSet<string>();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            InitializeEvents();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        public void ResetGameSession()
        {
            seenEventIds.Clear();
        }

        public void InitializeEvents()
        {
            allEvents.Clear();

            // Try loading from JSON asset if available
            if (eventsJsonAsset == null)
            {
                eventsJsonAsset = Resources.Load<TextAsset>("Events");
                if (eventsJsonAsset == null)
                {
#if UNITY_EDITOR
                    eventsJsonAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/Events.json");
#endif
                }
            }

            if (eventsJsonAsset != null && !string.IsNullOrWhiteSpace(eventsJsonAsset.text))
            {
                try
                {
                    string wrappedJson = $"{{\"events\":{eventsJsonAsset.text}}}";
                    EventsDatabase db = JsonUtility.FromJson<EventsDatabase>(wrappedJson);
                    if (db != null && db.events != null && db.events.Count > 0)
                    {
                        foreach (var item in db.events)
                        {
                            EventData ed = ScriptableObject.CreateInstance<EventData>();
                            ed.id = item.id;
                            ed.title = item.title;
                            ed.category = ParseCategory(item.category);
                            ed.description = item.description;
                            ed.costOrIncome = item.costOrIncome;
                            ed.lifestyleMultiplier = item.lifestyleMultiplier > 0 ? item.lifestyleMultiplier : 1.0f;
                            ed.isMandatory = item.isMandatory;
                            ed.hasChoices = item.hasChoices;
                            if (item.choiceA != null)
                            {
                                ed.choiceA = new EventChoice
                                {
                                    choiceText = item.choiceA.choiceText,
                                    rupeeChange = item.choiceA.rupeeChange,
                                    scoreChange = item.choiceA.scoreChange,
                                    feedbackMessage = item.choiceA.feedbackMessage,
                                    buttonColor = new Color32((byte)item.choiceA.buttonColor.r, (byte)item.choiceA.buttonColor.g, (byte)item.choiceA.buttonColor.b, (byte)item.choiceA.buttonColor.a)
                                };
                            }
                            if (item.choiceB != null)
                            {
                                ed.choiceB = new EventChoice
                                {
                                    choiceText = item.choiceB.choiceText,
                                    rupeeChange = item.choiceB.rupeeChange,
                                    scoreChange = item.choiceB.scoreChange,
                                    feedbackMessage = item.choiceB.feedbackMessage,
                                    buttonColor = new Color32((byte)item.choiceB.buttonColor.r, (byte)item.choiceB.buttonColor.g, (byte)item.choiceB.buttonColor.b, (byte)item.choiceB.buttonColor.a)
                                };
                            }
                            allEvents.Add(ed);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RandomEventManager] Failed to parse Events.json: {ex.Message}. Falling back to default events.");
                }
            }

            if (allEvents.Count == 0)
            {
                CreateFallbackEvents();
            }

            Debug.Log($"[RandomEventManager] Initialized with {allEvents.Count} financial dilemma events.");
        }

        private EventCategory ParseCategory(string cat)
        {
            if (string.Equals(cat, "Emergency", StringComparison.OrdinalIgnoreCase)) return EventCategory.Emergency;
            if (string.Equals(cat, "Windfall", StringComparison.OrdinalIgnoreCase)) return EventCategory.Windfall;
            return EventCategory.Opportunity;
        }

        private void CreateFallbackEvents()
        {
            allEvents = new List<EventData>
            {
                CreateEvent("event_viral_fever", "Viral Fever & Clinic Visit", EventCategory.Emergency,
                    "A sudden viral fever hit you before mid-terms. Doctor consultation and prescriptions cost you money.",
                    -1200, true),
                CreateEvent("event_phone_screen", "Cracked Phone Screen", EventCategory.Emergency,
                    "Your phone slipped from your study desk and shattered the display glass.",
                    -2400, true),
                CreateEvent("event_lab_fee", "Mandatory Lab Manual & Kit Fee", EventCategory.Emergency,
                    "Department lab practicals require purchasing the official experiment manual and component kit.",
                    -850, true),
                CreateEvent("event_hostel_repair", "Hostel TV Short-Circuit Repair", EventCategory.Emergency,
                    "A sudden electrical short-circuit in the student common room requires shared contribution.",
                    -850, true),
                CreateEvent("event_college_fee", "College Semester Activity Fee", EventCategory.Emergency,
                    "University portal exam registration and student union activity dues must be cleared.",
                    -1500, true),
                CreateEvent("event_book_fee", "Reference Library Book Fee", EventCategory.Emergency,
                    "Annual institutional library membership and reserve reference volumes fee is due.",
                    -750, true),
                CreateEvent("event_freelance_gig", "Freelance Poster Design Gig", EventCategory.Windfall,
                    "A local cafe hired you over the weekend to design promotional flyers and festival branding!",
                    2500, true),
                CreateEvent("event_tutoring_bonus", "Extra Tutoring Session Bonus", EventCategory.Windfall,
                    "You tutored junior high students in math and their parents awarded you a generous bonus!",
                    1500, true),
                CreateEvent("event_grandpa_gift", "Birthday Cash Gift from Grandpa", EventCategory.Windfall,
                    "Your grandfather came to visit your student accommodation and surprised you with celebration cash!",
                    2000, true),
                CreateEvent("event_scrap_recycling", "Hostel Old Books & Scrap Recycling", EventCategory.Windfall,
                    "You cleared out semester scrap, packaging cartons, and old newspapers to the eco-recycling vendor.",
                    400, true)
            };
        }

        private EventData CreateEvent(string id, string title, EventCategory category, string desc, int costOrIncome, bool mandatory)
        {
            EventData ed = ScriptableObject.CreateInstance<EventData>();
            ed.id = id;
            ed.title = title;
            ed.category = category;
            ed.description = desc;
            ed.costOrIncome = costOrIncome;
            ed.isMandatory = mandatory;
            ed.hasChoices = false;
            return ed;
        }

        public EventData GetRandomEvent()
        {
            if (allEvents == null || allEvents.Count == 0) InitializeEvents();

            List<EventData> available = new List<EventData>();
            foreach (var ev in allEvents)
            {
                if (!seenEventIds.Contains(ev.id))
                {
                    available.Add(ev);
                }
            }

            if (available.Count == 0)
            {
                // If all 16 events have been seen in a 30-day playthrough, reset history
                seenEventIds.Clear();
                available = new List<EventData>(allEvents);
            }

            int index = UnityEngine.Random.Range(0, available.Count);
            EventData chosen = available[index];
            seenEventIds.Add(chosen.id);
            return chosen;
        }

        public EventData GetEventById(string id)
        {
            if (allEvents == null || allEvents.Count == 0) InitializeEvents();
            foreach (var ev in allEvents)
            {
                if (string.Equals(ev.id, id, StringComparison.OrdinalIgnoreCase))
                {
                    seenEventIds.Add(ev.id);
                    return ev;
                }
            }
            return GetRandomEvent();
        }

        public void TriggerEvent(EventData ev, Action<EventChoice> onResolved = null)
        {
            if (ev == null) ev = GetRandomEvent();

            LifestyleType currentLifestyle = LifestyleType.Middle;
            if (GameManager.Instance != null)
            {
                currentLifestyle = GameManager.Instance.selectedLifestyle;
            }

            int scaledAmount = ev.GetScaledAmount(currentLifestyle);

            if (TownHUD.Instance != null)
            {
                TownHUD.Instance.OpenEventModal(ev, scaledAmount, (choice) =>
                {
                    int rupeeDelta = choice.rupeeChange;
                    int scoreDelta = choice.scoreChange;

                    if (GameManager.Instance != null)
                    {
                        string catStr = ev != null ? ev.category.ToString() : "Emergency";
                        GameManager.Instance.RecordEncounteredEvent(ev != null ? ev.title : "Unexpected Event", rupeeDelta, catStr);
                        if (scoreDelta > 0) GameManager.Instance.wiseEventChoicesCount++;
                        if (rupeeDelta < 0 && ev != null && !ev.isMandatory) GameManager.Instance.impulseSpendingEventsCount++;

                        if (rupeeDelta != 0)
                        {
                            string type = (rupeeDelta > 0) ? "Income" : "Expense";
                            GameManager.Instance.ApplySavingsChange(rupeeDelta, $"{ev.title}", type);
                        }
                        if (scoreDelta != 0)
                        {
                            GameManager.Instance.score = Mathf.Max(0, GameManager.Instance.score + scoreDelta);
                        }
                    }

                    if (TownHUD.Instance != null)
                    {
                        if (scoreDelta != 0)
                        {
                            TownHUD.Instance.ShowScoreFloatingFeedback(scoreDelta);
                        }
                        TownHUD.Instance.UpdateFinancials();
                        TownHUD.Instance.UpdateTransactionFeed();

                        // Check if payment caused deficit below Rs. 0 -> Triggers Deficit Alert immediately!
                        TownHUD.Instance.CheckAndShowDeficitAlert();
                    }

                    if (DayManager.Instance != null && DayManager.Instance.currentDayTask != null && DayManager.Instance.currentDayTask.isEventDay)
                    {
                        DayManager.Instance.ResolveCurrentEvent();
                    }

                    onResolved?.Invoke(choice);
                });
            }
        }

        public void TriggerRandomEvent(Action<EventChoice> onResolved = null)
        {
            EventData ev = GetRandomEvent();
            TriggerEvent(ev, onResolved);
        }
    }

    [Serializable]
    public class EventsDatabase
    {
        public List<JsonEventItem> events;
    }

    [Serializable]
    public class JsonEventItem
    {
        public string id;
        public string title;
        public string category;
        public string description;
        public int costOrIncome;
        public float lifestyleMultiplier;
        public bool isMandatory;
        public bool hasChoices;
        public JsonEventChoiceItem choiceA;
        public JsonEventChoiceItem choiceB;
    }

    [Serializable]
    public class JsonEventChoiceItem
    {
        public string choiceText;
        public int rupeeChange;
        public int scoreChange;
        public string feedbackMessage;
        public JsonColor buttonColor;
    }

    [Serializable]
    public class JsonColor
    {
        public int r = 0;
        public int g = 200;
        public int b = 83;
        public int a = 255;
    }
}
