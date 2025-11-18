using System.Collections;
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
    public int actionPoint = 2;      // 행동 포인트 (0이면 행동 불가)
    public int passivePoint = 2;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // HP 바 생성
        CreateHPBar();        
        
        SetStrategyName();
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
                StrategyAction strategyAction = availableActions[i];
                Action action = ActionManager.Instance.GetActionByName(strategyAction.action);
                if (action == null)
                {
                    Debug.Log($"{characterName}이(가) {strategyAction.action}을(를) 실행할 수 없습니다.");
                    return null;
                }
                else
                {
                    Debug.Log($"{characterName}이(가) {action.name}을(를) 실행했습니다.");
                    UseSkill(action.id, null);
                    return strategyAction;
                }
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

        actionPoint -= skill.costAP;     
        passivePoint -= skill.costPP; 

        if(skill.animation != "")
        {
            Animator animator = GetComponent<Animator>();
            if (animator != null)
            {
                // 코루틴으로 애니메이션 종료 대기
                StartCoroutine(PlayAnimationAndWait(animator, skill, target));
                return;
            }
        }
        
        // 애니메이션이 없으면 바로 스킬 효과 적용
        OnSkillAnimationComplete(skill, target);
    }
    
    // 애니메이션 재생 후 대기하는 코루틴
    private IEnumerator PlayAnimationAndWait(Animator animator, Action skill, Character target)
    {
        // 애니메이션 재생 (normalizedTime = 0으로 설정하여 처음부터 강제 재생)
        // 동일 애니메이션을 연속 재생할 때도 정상 작동
        animator.Play(skill.animation, 0, 0f);
        Debug.Log($"{characterName}: 애니메이션 '{skill.animation}' 재생 시작");
        
        // 한 프레임 대기 (애니메이션 시작 대기)
        yield return null;
        
        // 현재 애니메이션 상태 정보 가져오기
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        
        // 애니메이션 길이만큼 대기
        float animationLength = stateInfo.length;
        yield return new WaitForSeconds(animationLength);
        
        // 애니메이션 종료 후 실행
        OnSkillAnimationComplete(skill, target);
    }
    
    // 애니메이션 완료 후 호출되는 함수
    private void OnSkillAnimationComplete(Action skill, Character target)
    {
        Debug.Log($"{characterName}: {skill.name} 애니메이션 완료!");
        
        // 스킬 효과 적용
        ActionManager.Instance.ApplyActionEffects(skill, this, target);
        
        // BattleManager에 액션 완료 알림
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.OnCharacterActionFinished(this);
        }
    }
 
    // PP 회복
    public void RestorePP(int amount)
    {
        pp = Mathf.Min(maxPP, pp + amount);
        Debug.Log($"{characterName}의 PP +{amount} (현재: {pp}/{maxPP})");
    }    
    
}
