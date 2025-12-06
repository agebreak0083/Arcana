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

            // 무조건 8개의 Row를 생성
            for (int i = 0; i < TacticsDatabase.MAX_TACTICS_ROW; i++)
            {
                rows.Add(new TacticRow());
            }
        }
    }

    [Serializable]
    public class TacticRow
    {
        public string skillName = "---";
        public string skillType = "AP"; // "AP" or "PP"
        public string condition1 = "조건 없음";
        public string condition2 = "조건 없음";

        // 기본 생성자 (JSON 직렬화를 위해 필요)
        public TacticRow()
        {
        }

        public TacticRow(string skill, string type, string c1, string c2)
        {
            skillName = skill;
            skillType = type;
            condition1 = c1;
            condition2 = c2;
        }
    }
}
