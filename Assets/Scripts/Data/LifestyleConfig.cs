using UnityEngine;

namespace RupeeRanger.Data
{
    [CreateAssetMenu(fileName = "LifestyleConfig", menuName = "RupeeRanger/Lifestyle Config")]
    public class LifestyleConfig : ScriptableObject
    {
        public LifestyleType lifestyleType;
        public string displayName;
        public string subtitle;
        public int monthlyIncome;
        
        [Header("Expense Slider Maximums")]
        public int maxRent;
        public int maxFood;
        public int maxTransport;
        public int maxEntertainment;

        [Header("Visuals")]
        public Sprite icon;
        public Color themeColor = Color.white;
    }
}
