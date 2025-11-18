using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public string characterName;
    public string strategyName = "공격형 작전";
    Strategy currentStrategy; // 현재 사용 중인 작전
    List<StrategyAction> availableActions = new List<StrategyAction>();
    
    [Header("HP Bar")]
    public GameObject hpBarPrefab; // HP 바 프리팹
    public Vector3 hpBarOffset = new Vector3(0, 2.5f, 0); // HP 바 위치 오프셋
    private HPBar hpBar; // HP 바 인스턴스
    
    // 캐릭터 스탯
    [Header("Stats")]
    public int level = 1;            // 레벨
    public float hp = 100f;
    public float maxHp = 100f;
    public float mp = 100f;
    public float maxMp = 100f;
    public int pp = 10;              // PP (스킬 포인트)
    public int maxPP = 10;           // 최대 PP
    public float attackPower = 50f;
    public float magicPower = 50f;   // 마법 공격력
    public float defense = 30f;      // 방어력
    public float speed = 100f;    
    public int actionPoint = 3;      // 행동 포인트 (0이면 행동 불가)
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // HP 바 생성
        CreateHPBar();
        
        // 1초후 호출 하기        
        Invoke("SetStrategyName", 1f);
    }
    
    void OnDestroy()
    {
        // 캐릭터가 파괴될 때 HP 바도 제거
        if (hpBar != null)
        {
            Destroy(hpBar.gameObject);
        }
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
    
    // HP 바 생성
    private void CreateHPBar()
    {
        if (hpBarPrefab != null)
        {
            GameObject hpBarObj = Instantiate(hpBarPrefab);
            hpBarObj.transform.SetParent(transform);
            
            hpBar = hpBarObj.GetComponent<HPBar>();
            
            if (hpBar != null)
            {
                hpBar.Initialize(transform, maxHp, hp);
                hpBar.SetOffset(hpBarOffset);
            }
        }
    }
    
    // HP 변경
    public void TakeDamage(float damage)
    {
        hp = Mathf.Max(0, hp - damage);
        UpdateHPBar();
        
        if (hp <= 0)
        {
            OnDeath();
        }
    }
    
    // HP 회복
    public void Heal(float amount)
    {
        hp = Mathf.Min(maxHp, hp + amount);
        UpdateHPBar();
    }
    
    // HP 바 업데이트
    private void UpdateHPBar()
    {
        if (hpBar != null)
        {
            hpBar.UpdateHP(hp, maxHp);
        }
    }
    
    // 사망 처리
    private void OnDeath()
    {
        Debug.Log($"{characterName}이(가) 사망했습니다.");
        // TODO: 사망 애니메이션 및 처리
    }
    
    // HP 바 표시/숨김
    public void ShowHPBar(bool show)
    {
        if (hpBar != null)
        {
            hpBar.Show(show);
        }
    }
    
    // ========== 스킬 시스템 ==========
    
    // 스킬 사용 (ID로)
    public void UseSkill(string skillId, Character target)
    {
        if (ActionManager.Instance == null) return;
        
        Action skill = ActionManager.Instance.GetActionById(skillId);
        if (skill == null)
        {
            Debug.LogWarning($"스킬 ID '{skillId}'를 찾을 수 없습니다.");
            return;
        }
        
        // 스킬 사용 가능 여부 확인
        if (!ActionManager.Instance.CanUseAction(skill, this))
        {
            return;
        }
        
        // 스킬 효과 적용
        ActionManager.Instance.ApplyActionEffects(skill, this, target);
    }
    
    // 스킬 사용 (이름으로)
    public void UseSkillByName(string skillName, Character target)
    {
        if (ActionManager.Instance == null) return;
        
        Action skill = ActionManager.Instance.GetActionByName(skillName);
        if (skill != null)
        {
            UseSkill(skill.id, target);
        }
        else
        {
            Debug.LogWarning($"스킬을 찾을 수 없습니다: {skillName}");
        }
    }
    
    // 모든 스킬 가져오기
    public List<Action> GetAllSkills()
    {
        if (ActionManager.Instance == null) return new List<Action>();
        return ActionManager.Instance.GetAllActions();
    }
    
    // 액티브 스킬만 가져오기
    public List<Action> GetActiveSkills()
    {
        if (ActionManager.Instance == null) return new List<Action>();
        return ActionManager.Instance.GetActionsByType("active");
    }
    
    // 패시브 스킬만 가져오기
    public List<Action> GetPassiveSkills()
    {
        if (ActionManager.Instance == null) return new List<Action>();
        return ActionManager.Instance.GetActionsByType("passive");
    }
    
    // PP 회복
    public void RestorePP(int amount)
    {
        pp = Mathf.Min(maxPP, pp + amount);
        Debug.Log($"{characterName}의 PP +{amount} (현재: {pp}/{maxPP})");
    }
    
    // 레벨업
    public void LevelUp()
    {
        level++;
        
        // 스탯 증가
        maxHp += 10;
        hp = maxHp;
        maxMp += 5;
        mp = maxMp;
        maxPP += 1;
        pp = maxPP;
        attackPower += 5;
        magicPower += 5;
        defense += 3;
        
        UpdateHPBar();
        
        Debug.Log($"{characterName}이(가) 레벨 {level}이 되었습니다!");
    }
}
