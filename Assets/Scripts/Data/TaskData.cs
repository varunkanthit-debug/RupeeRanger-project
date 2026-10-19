using System;
using UnityEngine;
using RupeeRanger.Town;

namespace RupeeRanger.Data
{
    [CreateAssetMenu(fileName = "TaskData", menuName = "RupeeRanger/Task Data")]
    public class TaskData : ScriptableObject
    {
        [Header("Task Identity")]
        public string id;
        public int dayNumber;
        public string chapterTitle;

        [Header("Objectives")]
        public string objectiveText;
        public BuildingType targetBuilding;
        public string miniGameId;
        public int rewardAmount;

        [Header("Flags & Dialogue")]
        public bool isEventDay;
        [TextArea(2, 4)]
        public string description;
        [TextArea(2, 4)]
        public string hintText;
    }
}
