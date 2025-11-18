using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class StrategyManager : MonoBehaviour
{
    private StrategyCollection strategyCollection;

    public static StrategyManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        LoadStrategies();
    }
    
    // Json 파일에서 작전 데이터 로드
    public void LoadStrategies()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("strategies");
        if (jsonFile != null)
        {
            strategyCollection = JsonUtility.FromJson<StrategyCollection>(jsonFile.text);
            Debug.Log($"작전 데이터 로드 완료: {strategyCollection.strategies.Count}개의 작전");
            
            // 로드된 작전 정보 출력
            // foreach (Strategy strategy in strategyCollection.strategies)
            // {
            //     Debug.Log($"작전명: {strategy.name}, 액션 수: {strategy.actions.Count}");
            // }
        }
        else
        {
            Debug.LogError("strategies.json 파일을 찾을 수 없습니다!");
        }
    }
    
    // 작전 이름으로 작전 가져오기
    public Strategy GetStrategyByName(string name)
    {
        if (strategyCollection == null) return null;
        
        return strategyCollection.strategies.Find(s => s.name == name);
    }
    
    // 모든 작전 가져오기
    public List<Strategy> GetAllStrategies()
    {
        if (strategyCollection == null) return new List<Strategy>();
        
        return strategyCollection.strategies;
    }
    
    // 작전의 액션을 우선순위 순으로 정렬하여 가져오기
    public List<StrategyAction> GetSortedActions(string strategyName)
    {
        Strategy strategy = GetStrategyByName(strategyName);
        if (strategy == null) return new List<StrategyAction>();
        
        List<StrategyAction> sortedActions = new List<StrategyAction>(strategy.actions);
        sortedActions.Sort((a, b) => a.priority.CompareTo(b.priority));
        
        return sortedActions;
    }
    
    // 조건을 평가하여 실행할 액션 결정 (예제)
    public StrategyAction GetActionToExecute(string strategyName)
    {
        List<StrategyAction> actions = GetSortedActions(strategyName);
        
        // 우선순위가 높은 순서대로 조건 확인
        foreach (StrategyAction action in actions)
        {
            // 실제로는 여기서 조건1, 조건2를 평가해야 합니다
            // 예제에서는 첫 번째 액션을 반환
            if (EvaluateConditions(action))
            {
                return action;
            }
        }
        
        return null;
    }
    
    // 조건 평가 함수 (구현 예제)
    private bool EvaluateConditions(StrategyAction action)
    {
        // TODO: 실제 게임 로직에 맞게 조건을 평가하는 코드 구현
        // 예: HP, MP, 적의 상태 등을 확인
        
        // 현재는 조건이 비어있지 않으면 true 반환 (임시)
        return !string.IsNullOrEmpty(action.condition1);
    }
}

