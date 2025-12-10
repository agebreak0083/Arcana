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
        public string model; // Model prefab name/path
        public List<Skill> skills = new List<Skill>();
    }

    public enum SkillType
    {
        AP,
        PP
    }
}
