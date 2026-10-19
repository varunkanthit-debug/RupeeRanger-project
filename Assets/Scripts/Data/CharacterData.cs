using UnityEngine;

namespace RupeeRanger.Data
{
    [CreateAssetMenu(fileName = "CharacterData", menuName = "RupeeRanger/Character Data")]
    public class CharacterData : ScriptableObject
    {
        public string id;
        public string characterName;
        public Sprite sprite;
        public int unlockScore;
        [TextArea(2, 4)]
        public string description;
    }
}
