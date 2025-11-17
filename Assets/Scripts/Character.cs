using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public string characterName;
    public string strategyName = "공격형 작전";
    Strategy currentStrategy; // 현재 사용 중인 작전
    List<StrategyAction> availableActions = new List<StrategyAction>();
    
    
    // 캐릭터 스탯 (예제)
    public float hp = 100f;
    public float maxHp = 100f;
    public float mp = 100f;
    public float maxMp = 100f;
    public float attackPower = 50f;
    public float speed = 100f;    
    int actionPoint = 3; // 행동 포인트 (0이면 행동 불가)
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 1초후 호출 하기        
        Invoke("SetStrategyName", 1f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetStrategyName()
    {
        currentStrategy = StrategyManager.Instance.GetStrategyByName(strategyName);        
        SetStrategy(currentStrategy);
    }
    
    // 작전 설정
    public void SetStrategy(Strategy strategy)
    {
        currentStrategy = strategy;
        Debug.Log($"{characterName}의 작전을 '{strategy.name}'으로 설정했습니다.");

        availableActions.Clear();
        availableActions.AddRange(currentStrategy.actions); 
        // 우선 순위에 따라 정렬 
        availableActions.Sort((a, b) => a.priority.CompareTo(b.priority));

        //Debug.Log($"{characterName}의 작전 액션: {availableActions.Count}개");
    }
    
    // 작전에 따라 행동 결정
    public StrategyAction RunAction()
    {
        if (currentStrategy == null) return null;
        if (actionPoint <= 0) return null;
        
        // 우선순위가 높은 순서대로 조건을 확인하여 실행할 액션 결정
        for(int i = 0; i < availableActions.Count; i++)
        {
            if (CheckConditions(availableActions[i]))
            {
                actionPoint--; 

                Debug.Log($"{characterName}이(가) {availableActions[i].action}을(를) 실행했습니다.");

                // 자신의 액션이 끝났음을 BattleManager에 알린다.
                BattleManager.Instance.OnCharacterActionFinished(this);

                return availableActions[i];
            }
        }

        Debug.Log($"{characterName}이(가) 행동할 수 없습니다.");
        return null;
    }
    
    // 조건 확인 (예제)
    private bool CheckConditions(StrategyAction action)
    {
        // TODO: 실제 게임 로직에 맞게 조건을 확인하는 코드 구현
        // 예: HP 비율, MP, 적의 상태 등을 확인
        
        bool condition1Met = EvaluateCondition(action.condition1);
        bool condition2Met = string.IsNullOrEmpty(action.condition2) || EvaluateCondition(action.condition2);

        //Debug.Log($"{characterName}의 조건 확인: {action.action}, condition1Met: {condition1Met}, condition2Met: {condition2Met}");
        
        return condition1Met && condition2Met;
    }
    
    // 개별 조건 평가 (예제)
    private bool EvaluateCondition(string condition)
    {
        if (string.IsNullOrEmpty(condition)) return true;
        
        // 간단한 조건 평가 예제
        if (condition.Contains("HP <"))
        {
            // HP 조건 파싱 및 확인
            float hpPercent = (hp / maxHp) * 100f;
            // 예: "아군 HP < 50%"
            // 실제로는 더 정교한 파싱이 필요
            return true; // 임시
        }
        
        return true; // 기본값
    }
}
