using System;
using System.Collections.Generic;

namespace Arcana.Tactics.Data
{
    [Serializable]
    public class TacticsPlan
    {
        public string characterId;
        public List<TacticRow> rows = new List<TacticRow>();

        public TacticsPlan(string charId)
        {
            characterId = charId;
        }
    }

    [Serializable]
    public class TacticRow
    {
        public string skillName;
        public string skillType; // "AP" or "PP"
        public string condition1;
        public string condition2;

        public TacticRow(string skill, string type, string c1, string c2)
        {
            skillName = skill;
            skillType = type;
            condition1 = c1;
            condition2 = c2;
        }
    }
}
