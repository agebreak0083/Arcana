using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Arcana.Tactics;
using UnityEngine;
using DG.Tweening;

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
    public ClassStats stats;
    public float hp = 100;
    public float maxHp = 100;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // HP 바 생성
        CreateHPBar();

        // SetStrategyName();
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

    // 직업의 실제 스탯 수치 반환 (레벨 1 기준)
    public void ApplyClassStatsToCharacter()
    {
        var classInfo = TacticsDataManager.Instance.GetClassInfo(className);

        if (classInfo == null)
        {
            Debug.LogWarning($"직업 '{className}'을 찾을 수 없습니다.");
            return;
        }

        // ClassStats 객체 복사 (각 캐릭터가 독립적인 stats를 가지도록)
        stats = classInfo.stats.Clone();

        // HP 적용 (등급을 실제 수치로 변환)
        maxHp = stats.GetHPValue();
        hp = maxHp;

        Debug.Log($"{characterName}에게 {className} 직업 스탯 적용 완료 (AP: {stats.actionPoint}, PP: {stats.passivePoint})");
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
        if (stats.actionPoint <= 0) return null;

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

                Debug.Log($"{characterName}이(가) {skill.name}을(를) 실행했습니다.");

                // 전투 로그에 공격 기록
                if (BattleLogManager.Instance != null)
                {
                    BattleLogManager.Instance.LogAttack(characterName, target.characterName, skill.name);
                }

                UseSkill(skill.id, target);
                return strategyAction;

            }
        }

        Debug.Log($"{characterName}이(가) 행동할 수 없습니다.");
        return null;
    }

    /// <summary>
    /// 조건에 맞는 타겟을 선택
    /// Condition2: 필터링 (조건에 맞는 캐릭터만 남김)
    /// Condition1: 선택 (필터링된 리스트에서 최종 타겟 1명 선택)
    /// </summary>
    private Character GetTarget(StrategyAction action)
    {
        // 1. 적 리스트 가져오기 (복사본)
        List<Character> originalTargets = BattleManager.Instance.GetEnemyTargets(this);
        List<Character> candidates = new List<Character>(originalTargets);

        // 2. 사망한 캐릭터 제거
        candidates.RemoveAll(c => c == null || c.hp <= 0);

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"{characterName}: 타겟 후보가 없습니다.");
            return null;
        }

        // 3. Condition2 적용 (필터링)
        if (!string.IsNullOrEmpty(action.condition2) && action.condition2 != "조건 없음")
        {
            var filter = TargetConditionFactory.CreateFilter(action.condition2);
            if (filter != null)
            {
                var filtered = filter.Filter(candidates, this);
                if (filtered.Count > 0)
                {
                    candidates = filtered;
                    Debug.Log($"{characterName}: Condition2 '{action.condition2}' 적용 → {candidates.Count}명 남음");
                }
                else
                {
                    // 필터링 조건을 만족하는 타겟이 없으면 null 반환
                    Debug.LogWarning($"{characterName}: Condition2 '{action.condition2}' 조건을 만족하는 타겟이 없습니다.");
                    return null;
                }
            }
        }

        // 4. Condition1 적용 (선택)
        var selector = TargetConditionFactory.CreateSelector(action.condition1);
        Character target = selector.Select(candidates, this);

        if (target != null)
        {
            Debug.Log($"{characterName}의 최종 타겟: {target.characterName} (Condition1: {action.condition1})");
        }
        else
        {
            Debug.LogWarning($"{characterName}: 타겟 선택 실패");
        }

        return target;
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
                hpBar.Initialize(transform, maxHp, hp, characterName);
                hpBar.SetOffset(hpBarOffset);
            }
        }
    }

    // HP 변경
    public void TakeDamage(float damage, bool isCritical = false)
    {
        Animator animator = GetComponent<Animator>();
        StartCoroutine(PlayAnimationAndWait(animator, "Damaged@loop"));

        hp = Mathf.Max(0, hp - damage);
        UpdateHPBar();

        // 데미지 텍스트 표시
        if (BattleUI.Instance != null)
        {
            // 캐릭터 머리 위 위치 계산 (약간 위로)
            Vector3 damageTextPosition = transform.position + Vector3.up * 1f;
            BattleUI.Instance.ShowDamageText(Mathf.RoundToInt(damage), damageTextPosition, isCritical);
        }

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

        // 사망 애니메이션 재생
        Animator animator = GetComponent<Animator>();
        animator.Play("KneelDown", 0, 0f);
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

        stats.actionPoint -= skill.costAP;
        stats.passivePoint -= skill.costPP;

        // 전투 로그에 AP/PP 소모 및 남은 포인트 기록
        if (BattleLogManager.Instance != null)
        {
            string apInfo = skill.costAP > 0 ? $"<color=#FF6B6B>AP -{skill.costAP}</color> (남은 AP: <color=#87CEEB>{stats.actionPoint}</color>)" : "";
            string ppInfo = skill.costPP > 0 ? $"<color=#FFA500>PP -{skill.costPP}</color> (남은 PP: <color=#90EE90>{stats.passivePoint}</color>)" : "";

            if (!string.IsNullOrEmpty(apInfo) || !string.IsNullOrEmpty(ppInfo))
            {
                string separator = (!string.IsNullOrEmpty(apInfo) && !string.IsNullOrEmpty(ppInfo)) ? ", " : "";
                BattleLogManager.Instance.AddLog($"  → {apInfo}{separator}{ppInfo}");
            }
        }

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

    // 애니메이션 재생 후 대기하는 코루틴 (DoTween 이동 포함)
    private IEnumerator PlaySkillAnimationAndWait(Animator animator, Skill skill, Character target)
    {
        // 원래 위치 저장
        Vector3 originalPosition = transform.position;

        // Step 1: 타겟 앞으로 이동 (타겟의 X좌표 + 1m)
        Vector3 targetPosition = target.transform.position + target.transform.forward * 1.0f;

        // DoTween으로 이동 
        float moveTime = 0.5f;
        transform.DOMove(targetPosition, moveTime).SetEase(Ease.OutQuad);

        // 이동 완료 대기
        yield return new WaitForSeconds(moveTime);

        // Step 2: 스킬 애니메이션 재생
        animator.Play(skill.animation, 0, 0f);
        Debug.Log($"{characterName}: 애니메이션 '{skill.animation}' 재생 시작");

        // 스킬 효과 적용
        float effectTime = 0.5f;
        yield return new WaitForSeconds(effectTime);
        SkillManager.Instance.ApplySkillEffects(skill, this, target);

        // 한 프레임 대기 (애니메이션 시작 대기)
        yield return null;

        // 현재 애니메이션 상태 정보 가져오기
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // 애니메이션 길이만큼 대기
        float animationLength = stateInfo.length;
        yield return new WaitForSeconds(animationLength - effectTime);

        // Step 3: 원래 자리로 복귀 
        float returnTime = 0.5f;
        transform.DOMove(originalPosition, returnTime).SetEase(Ease.InQuad);

        // 복귀 완료 대기
        yield return new WaitForSeconds(returnTime);

        // 애니메이션 종료 후 실행
        OnSkillAnimationComplete(skill, target);
    }

    // 애니메이션 완료 후 호출되는 함수
    private void OnSkillAnimationComplete(Skill skill, Character target)
    {
        Debug.Log($"{characterName}: {skill.name} 애니메이션 완료!");



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
        if (stats != null)
        {
            stats.passivePoint += amount;
            // 최대치 제한 로직이 필요하다면 추가
        }
    }

    // AP/PP 회복 (라운드 시작 시 호출)
    public void RestoreAPPP()
    {
        if (stats != null)
        {
            // 가장 확실한 방법: 원본 ClassInfo에서 다시 가져오기
            var classInfo = TacticsDataManager.Instance.GetClassInfo(className);
            if (classInfo != null)
            {
                stats.actionPoint = classInfo.stats.actionPoint;
                stats.passivePoint = classInfo.stats.passivePoint;
                Debug.Log($"{characterName}의 AP/PP가 회복되었습니다. (AP: {stats.actionPoint}, PP: {stats.passivePoint})");
            }
        }
    }
}
