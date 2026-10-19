using System;
using UnityEngine;

namespace RupeeRanger.Data
{
    public enum EventCategory
    {
        Emergency,
        Windfall,
        Opportunity
    }

    [Serializable]
    public class EventChoice
    {
        public string choiceText;
        public int rupeeChange; // Cost (negative) or Income (positive)
        public int scoreChange; // Score impact (e.g. +25 or -10)
        public string feedbackMessage;
        public Color32 buttonColor = new Color32(0x00, 0xC8, 0x53, 0xFF);
    }

    [CreateAssetMenu(fileName = "NewEventData", menuName = "RupeeRanger/Event Data")]
    [Serializable]
    public class EventData : ScriptableObject
    {
        [Header("Event Identification")]
        public string id;
        public string title;
        public EventCategory category = EventCategory.Emergency;
        [TextArea(2, 4)]
        public string description;

        [Header("Financial Settings")]
        public int costOrIncome; // Negative for expense, positive for windfall
        public float lifestyleMultiplier = 1.0f; // Scale expense by lifestyle
        public bool isMandatory = true; // Single mandatory pay/claim button vs 2-choice decision

        [Header("Choice Configuration (For Flexible Events)")]
        public bool hasChoices = false;
        public EventChoice choiceA;
        public EventChoice choiceB;

        /// <summary>
        /// Calculates the scaled rupee amount based on player's chosen lifestyle.
        /// Basic = 1.0x, Middle = 1.25x, High = 1.8x.
        /// Windfalls do not penalize.
        /// </summary>
        public int GetScaledAmount(LifestyleType lifestyle)
        {
            if (costOrIncome > 0)
            {
                // Windfalls give full generous amount across all lifestyles
                return costOrIncome;
            }

            float mult = 1.0f;
            switch (lifestyle)
            {
                case LifestyleType.Middle:
                    mult = 1.25f;
                    break;
                case LifestyleType.High:
                    mult = 1.8f;
                    break;
                default:
                    mult = 1.0f;
                    break;
            }

            return Mathf.RoundToInt(costOrIncome * mult);
        }

        public string GetCategoryBadgeText()
        {
            switch (category)
            {
                case EventCategory.Emergency:
                    return "⚠ SUDDEN EMERGENCY";
                case EventCategory.Windfall:
                    return "🎁 SURPRISE WINDFALL";
                case EventCategory.Opportunity:
                default:
                    return "⚡ UNEXPECTED DILEMMA";
            }
        }
    }
}
