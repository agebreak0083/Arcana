using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class StrategyManager : MonoBehaviour
{
    private StrategyCollection strategyCollection;

    public static StrategyManager Instance;

    void Awake()
    {
        // 씬마다 독립적인 인스턴스 사용
        Instance = this;
    }

    // TacticsPlan을 Strategy로 변환
    public Strategy CreateStrategy(Arcana.Tactics.Data.TacticsPlan plan)
    {
        if (plan == null) return null;

        Strategy strategy = new Strategy();
        strategy.name = "Tactics Plan";

        for (int i = 0; i < plan.rows.Count; i++)
        {
            var row = plan.rows[i];

            // skillName이 "---"인 Row는 실행되지 않음
            if (row.skillName == "---") continue;

            strategy.actions.Add(new StrategyAction
            {
                priority = i + 1,
                action = row.skillName,
                condition1 = row.condition1,
                condition2 = row.condition2
            });
        }

        return strategy;
    }

    // Legacy support - might be removed later
    public Strategy GetStrategyByName(string name)
    {
        return null;
    }
}

