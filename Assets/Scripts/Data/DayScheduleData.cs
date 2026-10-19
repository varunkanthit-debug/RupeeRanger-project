using System;
using System.Collections.Generic;
using UnityEngine;
using RupeeRanger.Town;

namespace RupeeRanger.Data
{
    [Serializable]
    public class DayEntryRaw
    {
        public string id;
        public int dayNumber;
        public string chapterTitle;
        public string objectiveText;
        public string targetBuilding;
        public string miniGameId;
        public int rewardAmount;
        public bool isEventDay;
        public string description;
        public string hintText;
    }

    [Serializable]
    public class DayScheduleData
    {
        public List<DayEntryRaw> days = new List<DayEntryRaw>();
    }
}
