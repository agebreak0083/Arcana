using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StrategyAction
{
    public int priority;        // 우선순위
    public string action;       // 행동
    public string condition1;   // 조건1
    public string condition2;   // 조건2
}

[Serializable]
public class Strategy
{
    public string name;         // 작전 이름
    public List<StrategyAction> actions; // 액션 리스트 (최대 8개)
    
    public Strategy()
    {
        actions = new List<StrategyAction>();
    }
}

[Serializable]
public class StrategyCollection
{
    public List<Strategy> strategies;
    
    public StrategyCollection()
    {
        strategies = new List<Strategy>();
    }
}

