using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Character : MonoBehaviour
{
    public string characterName;
    public string className;    // 직업
    public string strategyName;
    Strategy currentStrategy; // 현재 사용 중인 작전
    List<StrategyAction> availableActions = new List<StrategyAction>();
    public int position = 1;

    [Header("HP Bar")]
    public GameObject hpBarPrefab; // HP 바 프리팹
    public Vector3 hpBarOffset = new Vector3(0, 2.5f, 0); // HP 바 위치 오프셋
    private HPBar hpBar; // HP 바 인스턴스

    // 캐릭터 스탯
    [Header("Stats")]

    public float hp = 100f;
    public float maxHp = 100f;
    public int actionPoint = 2;      // 행동 포인트 (0이면 행동 불가)
    public int passivePoint = 2;
    internal float attackPower;
    internal float defense;
    internal float magicPower;
    internal float speed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 직업 스탯 적용
        if (!string.IsNullOrEmpty(className))
        {
            ClassManager.Instance.ApplyClassStatsToCharacter(this, className);
        }

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

    public void MoveCharacter(int position)
    {
        // this.position = position;   
        // transform.position = BattleManager.Instance.GetPosition(position);
        // Debug.Log($"{characterName}이(가) {position}로 이동했습니다.");
        
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
        for (int i = 0; i < availableActions.Count; i++)
        {
            Character target = GetTarget(availableActions[i]);
            if (target != null)
            {
                StrategyAction strategyAction = availableActions[i];
                Skill skill = SkillManager.Instance.GetSkillByName(strategyAction.action);
                if (skill == null)
                {
                    Debug.Log($"{characterName}이(가) {strategyAction.action}을(를) 실행할 수 없습니다.");
                    return null;
                }
                else
                {
                    Debug.Log($"{characterName}이(가) {skill.name}을(를) 실행했습니다.");
                    UseSkill(skill.id, target);
                    return strategyAction;
                }
            }
        }

        Debug.Log($"{characterName}이(가) 행동할 수 없습니다.");
        return null;
    }

    // 조건 확인. 조건에 맞는 타겟을 반환한다. 없으면 null을 반환한다.
    private Character GetTarget(StrategyAction action)
    {
        // TODO: 실제 게임 로직에 맞게 조건을 확인하는 코드 구현
        // 예: HP 비율, MP, 적의 상태 등을 확인

        Character[] targetCharacters = BattleManager.Instance.GetEnemyTargets(this);
        Character target = null;        

        // 컨디션1, 컨디션2가 다 비어있으면, 기본 타겟을 반환 
        if (string.IsNullOrEmpty(action.condition1) && string.IsNullOrEmpty(action.condition2))
        {
            // 1. 우선 전열(1,2,3)에서 자신의 앞의 적을 찾고, 
            int targetPosition = ((this.position - 1) % 3) + 1;
            target = Array.Find(targetCharacters, c => c.position == targetPosition);

            // 2. 자신의 앞에 적이 없으면 가장 빠른 포지션의 적을 타겟팅한다. 
            if (target == null)
            {
                target = Array.Find(targetCharacters, c => c != null);
            }

            Debug.Log($"{characterName}의 기본 타겟: {target.characterName}");
            return target;
        }

        // 2번 조건 체크 
        Character[] targets = EvaluateCondition(targetCharacters, action.condition2);
        if (targets == null)
            return null;

        // 컨디션2에서 필터링된 타겟들 내에서만 필터링한다.
        targets = EvaluateCondition(targets, action.condition1);
        if (targets == null || targets.Count() == 0)
            return null;

        target = Array.Find(targetCharacters, c => c != null);
        Debug.Log($"{characterName}의 기본 타겟: {target.characterName}");

        return target;
    }

    // 개별 조건 평가 
    private Character[] EvaluateCondition(Character[] targets, string condition)
    {
        if (targets == null || targets.Count() == 0)
            return null;

        if (string.IsNullOrEmpty(condition))
            return targets;

        // HP가 가장 적은 
        if (condition.Contains("HP가 가장 적은"))
        {
            // targets 안에서 HP가 가장 적은 타겟을 반환 
            Character minHPCharacter = targets.OrderBy(c => c.hp).FirstOrDefault();
            if (minHPCharacter == null)
                return null;

            Debug.Log($"HP가 가장 적은 타겟: {minHPCharacter.characterName}");
            return new Character[] { minHPCharacter };

        }
        // HP 가장 많은 
        else if (condition.Contains("HP가 가장 많은"))
        {
            Character maxHPCharacter = targets.OrderByDescending(c => c.hp).FirstOrDefault();
            if (maxHPCharacter == null)
                return null;

            Debug.Log($"HP가 가장 많은 타겟: {maxHPCharacter.characterName}");
            return new Character[] { maxHPCharacter };
        }
        // 방어력이 가장 높은
        else if (condition.Contains("방어력이 가장 높은"))
        {
            Character maxDefenseCharacter = targets.OrderByDescending(c => c.defense).FirstOrDefault();
            if (maxDefenseCharacter == null)
                return null;

            Debug.Log($"방어력이 가장 높은 타겟: {maxDefenseCharacter.characterName}");
            return new Character[] { maxDefenseCharacter };
        }
        // 방어력이 가장 낮은
        else if (condition.Contains("방어력이 가장 낮은"))
        {
            Character minDefenseCharacter = targets.OrderBy(c => c.defense).FirstOrDefault();
            if (minDefenseCharacter == null)
                return null;

            Debug.Log($"방어력이 가장 낮은 타겟: {minDefenseCharacter.characterName}");
            return new Character[] { minDefenseCharacter };
        }
        
        
        

        return targets; // 기본값
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
        Animator animator = GetComponent<Animator>();
        StartCoroutine(PlayAnimationAndWait(animator, "Damaged@loop"));

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
        if (SkillManager.Instance == null) return;

        Skill skill = SkillManager.Instance.GetSkillById(skillId);
        if (skill == null)
        {
            Debug.LogWarning($"스킬 ID '{skillId}'를 찾을 수 없습니다.");
            return;
        }

        actionPoint -= skill.costAP;
        passivePoint -= skill.costPP;
        BattleManager.Instance.AddWaitFinished(this);
        BattleManager.Instance.AddWaitFinished(target);

        // UI에 스킬 이름 표시
        BattleManager.Instance.ShowSkillName(skill.name);

        if (skill.animation != "")
        {
            Animator animator = GetComponent<Animator>();
            if (animator != null)
            {
                // 코루틴으로 애니메이션 종료 대기
                StartCoroutine(PlaySkillAnimationAndWait(animator, skill, target));
                return;
            }
        }

        // 애니메이션이 없으면 바로 스킬 효과 적용
        OnSkillAnimationComplete(skill, target);
    }

    // 애니메이션 재생 후 대기하는 코루틴
    private IEnumerator PlaySkillAnimationAndWait(Animator animator, Skill skill, Character target)
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
    private void OnSkillAnimationComplete(Skill skill, Character target)
    {
        Debug.Log($"{characterName}: {skill.name} 애니메이션 완료!");

        // 스킬 효과 적용
        SkillManager.Instance.ApplySkillEffects(skill, this, target);

        // BattleManager에 액션 완료 알림
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.OnCharacterActionFinished(this);
        }
    }

    private IEnumerator PlayAnimationAndWait(Animator animator, String animationName)
    {
        // 애니메이션 재생 (normalizedTime = 0으로 설정하여 처음부터 강제 재생)
        // 동일 애니메이션을 연속 재생할 때도 정상 작동
        animator.Play(animationName, 0, 0f);

        // 한 프레임 대기 (애니메이션 시작 대기)
        yield return null;

        // 현재 애니메이션 상태 정보 가져오기
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // 애니메이션 길이만큼 대기
        float animationLength = stateInfo.length;
        yield return new WaitForSeconds(animationLength);

        // BattleManager에 액션 완료 알림
        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.OnCharacterActionFinished(this);
        }
    }

    // PP 회복
    public void RestorePP(int amount)
    {

    }

}
