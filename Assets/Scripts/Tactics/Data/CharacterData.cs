using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcana.Tactics.Data
{
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "Arcana/Character Data")]
    public class CharacterData : ScriptableObject
    {
        public string id;
        public string characterName;
        public string characterClass;
        public int cost;
        public string arcana;
        public int speed;
        [TextArea] public string description;
        public int imgSeed; // For the placeholder image URL logic if needed, or just use a Sprite in real Unity
        public Sprite portrait; // In a real project we use Sprites
        public List<SkillData> skills = new List<SkillData>();
    }

    [Serializable]
    public class SkillData
    {
        public string id;
        public string name;
        public string type; // "active" or "passive" from JSON
        public string description;
        public string target;
        public int costAP;
        public int costPP;

        // Helper property to maintain compatibility with existing code
        public SkillType skillType => (type == "active" || costAP > 0) ? SkillType.AP : SkillType.PP;
    }

    public enum SkillType
    {
        AP,
        PP
    }
}
