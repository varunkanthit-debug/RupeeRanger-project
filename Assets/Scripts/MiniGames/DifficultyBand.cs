using System;

namespace RupeeRanger.MiniGames
{
    public enum DifficultyBand
    {
        Easy,   // Ages 5-7
        Medium, // Ages 8-10
        Hard    // Ages 11-14
    }

    public static class DifficultyHelper
    {
        public static DifficultyBand FromAge(int age)
        {
            if (age <= 7) return DifficultyBand.Easy;
            if (age <= 10) return DifficultyBand.Medium;
            return DifficultyBand.Hard;
        }

        public static string GetLabel(DifficultyBand band)
        {
            switch (band)
            {
                case DifficultyBand.Easy: return "Easy (Ages 5-7)";
                case DifficultyBand.Medium: return "Medium (Ages 8-10)";
                case DifficultyBand.Hard: return "Hard (Ages 11-14)";
                default: return "Normal";
            }
        }
    }
}
