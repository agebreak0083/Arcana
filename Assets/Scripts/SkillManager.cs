using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Arcana.Tactics;
using Arcana.Tactics.Data;

/// <summary>
/// 스킬 데이터를 로드하고 관리하는 매니저
/// </summary>
[DefaultExecutionOrder(-100)]
public class SkillManager : MonoBehaviour
{
    private Dictionary<string, List<Skill>> skillsByClass = new Dictionary<string, List<Skill>>();
    private List<Skill> allSkills = new List<Skill>();

    public static SkillManager Instance;

    void Awake()
    {
        // 씬마다 독립적인 인스턴스 사용
        Instance = this;
        LoadSkills();
    }

    // Json 파일에서 스킬 데이터 로드
    public void LoadSkills()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Table/SkillList");
        if (jsonFile != null)
        {
            // SkillList.json은 클래스별로 그룹화된 구조
            try
            {
                // 간단한 JSON 파싱 (클래스별 스킬 목록)
                string jsonText = jsonFile.text.Trim();

                // Remove outer braces
                if (jsonText.StartsWith("{") && jsonText.EndsWith("}"))
                {
                    jsonText = jsonText.Substring(1, jsonText.Length - 2).Trim();
                }

                // Split by class sections (looking for ": [" pattern)
                int startIndex = 0;
                while (startIndex < jsonText.Length)
                {
                    // Find class name
                    int classNameStart = jsonText.IndexOf("\"", startIndex);
                    if (classNameStart == -1) break;

                    int classNameEnd = jsonText.IndexOf("\"", classNameStart + 1);
                    if (classNameEnd == -1) break;

                    string className = jsonText.Substring(classNameStart + 1, classNameEnd - classNameStart - 1);

                    // Find array start
                    int arrayStart = jsonText.IndexOf("[", classNameEnd);
                    if (arrayStart == -1) break;

                    // Find matching array end
                    int arrayEnd = FindMatchingBracket(jsonText, arrayStart);
                    if (arrayEnd == -1) break;

                    // Extract skills array JSON
                    string skillsArrayJson = jsonText.Substring(arrayStart, arrayEnd - arrayStart + 1);

                    // Parse skills array
                    List<Skill> classSkills = ParseSkillsArray(skillsArrayJson);

                    skillsByClass[className] = classSkills;
                    allSkills.AddRange(classSkills);

                    startIndex = arrayEnd + 1;
                }

                Debug.Log($"스킬 데이터 로드 완료: {allSkills.Count}개의 스킬, {skillsByClass.Count}개의 클래스");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"SkillList.json 파싱 오류: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("SkillList.json 파일을 찾을 수 없습니다!");
        }
    }

    private int FindMatchingBracket(string text, int openIndex)
    {
        int depth = 0;
        for (int i = openIndex; i < text.Length; i++)
        {
            if (text[i] == '[') depth++;
            else if (text[i] == ']')
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }

    private List<Skill> ParseSkillsArray(string arrayJson)
    {
        List<Skill> skills = new List<Skill>();

        // Wrap in a wrapper for JsonUtility
        string wrappedJson = "{\"skills\":" + arrayJson + "}";
        SkillCollection collection = JsonUtility.FromJson<SkillCollection>(wrappedJson);

        if (collection != null && collection.skills != null)
        {
            skills.AddRange(collection.skills);
        }

        return skills;
    }

    // 이름으로 스킬 가져오기
    public Skill GetSkillByName(string name)
    {
        return allSkills.Find(s => s.name == name);
    }

    // 클래스 이름으로 스킬 목록 가져오기
    public List<Skill> GetSkillsByClassName(string className)
    {
        if (skillsByClass.TryGetValue(className, out List<Skill> skills))
        {
            return new List<Skill>(skills);
        }
        return new List<Skill>();
    }

    // 모든 스킬 가져오기
    public List<Skill> GetAllSkills()
    {
        return new List<Skill>(allSkills);
    }

    // 타입별 스킬 가져오기 (active/passive)
    public List<Skill> GetSkillsByType(string type)
    {
        return allSkills.FindAll(s => s.type == type);
    }

    // 버튼 타입별 스킬 가져오기
    public List<Skill> GetSkillsByButtonType(string buttonType)
    {
        return allSkills.FindAll(s => s.buttonType == buttonType);
    }

    // 스킬 효과 적용
    public void ApplySkillEffects(Skill skill, Character user, List<Character> targets)
    {
        if (skill == null || user == null) return;

        // 타겟의 초기 HP 저장 (on_kill 체크용)
        Dictionary<Character, float> initialHP = new Dictionary<Character, float>();
        foreach (var target in targets)
        {
            if (target != null)
            {
                initialHP[target] = target.hp;
            }
        }

        // 각 효과 적용
        foreach (SkillEffect effect in skill.effects)
        {
            // on_kill 효과는 데미지 적용 후에 처리
            if (effect.type == "on_kill")
            {
                continue;
            }

            foreach(var target in targets)
            {
                ApplyEffect(effect, user, target, skill);
            }
        }

        // on_kill 효과 처리 (데미지 적용 후 타겟이 죽었는지 체크)
        foreach (SkillEffect effect in skill.effects)
        {
            if (effect.type == "on_kill")
            {
                foreach(var target in targets)
                {
                    if (target != null && initialHP.ContainsKey(target))
                    {
                        // 타겟이 죽었는지 체크 (HP <= 0)
                        if (target.hp <= 0)
                        {
                            ApplyEffect(effect, user, target, skill);
                        }
                    }
                }
            }
        }
    }

    // 개별 효과 적용
    private void ApplyEffect(SkillEffect effect, Character user, Character target, Skill skill)
    {
        // 모든 Character에게 누가 누구에게 스킬을 썼는지 알려준다. 
        //PassiveSkillResult result = BattleManager.Instance.OnBeforeSkillUse(user, target, skill, effect);
        PassiveSkillResult result = BattleManager.Instance.passiveSkillResult;

        switch (effect.type)
        {
            case "damage":
                if (target != null)
                {
                    bool isCritical;
                    float damage = CalculateDamage(effect.value, user, target, effect.damageType, result, out isCritical);
                    target.TakeDamage(damage, isCritical);
                }
                break;

            case "heal":
                if (target != null)
                {
                    target.Heal(effect.value);

                    // 전투 로그에 회복 기록
                    if (BattleLogManager.Instance != null)
                    {
                        BattleLogManager.Instance.LogHeal(target.characterName, effect.value);
                    }
                }
                break;

            case "buff":
                Character buffTarget = null;
                if (effect.target == "self")
                {
                    buffTarget = user;
                }
                else if (effect.target == "ally")
                {
                    // TODO : 아군 버프 적용
                }

                buffTarget.AddBuff(effect.stat, effect.value, effect.duration);

                // 전투 로그에 버프 기록
                if (BattleLogManager.Instance != null)
                {
                    BattleLogManager.Instance.AddLog($" {buffTarget.characterName} → <color=#00FF00>[{effect.stat} +{effect.value}% 버프 적용!]</color> (지속: {effect.duration}턴)");
                }
                break;
            case "stun":
                target.AddBuff("stun", 0, effect.duration);

                if (BattleLogManager.Instance != null)
                {
                    BattleLogManager.Instance.AddLog($" {target.characterName} → <color=#FF0000>[기절 부여!]</color> (지속: {effect.duration}턴)");
                }
                break;
            case "debuff":
                Debug.Log($"{effect.stat} {effect.value}% 디버프 적용! (지속: {effect.duration}턴)");
                // TODO: 실제 디버프 시스템 구현
                break;

            case "on_hit":
                if (effect.stat == "get_pp")
                {
                    user.RestorePP((int)effect.value);
                    if (BattleLogManager.Instance != null)
                    {
                        BattleLogManager.Instance.AddLog($" {user.characterName} → <color=#00FF00>[PP +{effect.value} 회복!]</color>");
                    }
                }
                break;

            case "on_kill":
                if (effect.stat == "get_ap")
                {
                    user.RestoreAP((int)effect.value);
                    if (BattleLogManager.Instance != null)
                    {
                        BattleLogManager.Instance.AddLog($" {user.characterName} → <color=#00FF00>[AP +{effect.value} 회복!]</color> (대상을 쓰러뜨림)");
                    }
                }
                break;

            default:
                Debug.Log($"효과 적용: {effect.type}");
                break;
        }
    }

    [Header("Battle Settings")]
    public float defaultCriticalRate = 20f;      // 기본 치명타 확률 (%)
    public float criticalDamageMultiplier = 1.5f; // 치명타 데미지 배율
    public float advantageDamageMultiplier = 2.0f; // 상성 우위 데미지 배율

    // 데미지 계산
    private float CalculateDamage(float skillPower, Character user, Character target, string damageType, PassiveSkillResult result, out bool isCritical)
    {
        isCritical = false;

        // 스킬 위력이 0이면 데미지도 0 (버프/디버프 스킬 등)
        if (skillPower <= 0) return 0.0f;

        // 1. 공격력 및 방어력 계산
        float physicalAttackValue = 0f;
        float physicalDefenseValue = 0f;
        float magicalAttackValue = 0f;
        float magicalDefenseValue = 0f;
        if(damageType == "physical")
        {
            physicalAttackValue = user.GetPhysicalAttackValue();
            physicalDefenseValue = target.GetPhysicalDefenseValue();                
            magicalAttackValue = 0f + result.GetEnchantMagicalAttackValue();

            Debug.Log($"physicalAttackValue: {physicalAttackValue}, physicalDefenseValue: {physicalDefenseValue}, magicalAttackValue: {magicalAttackValue}");
        }
        else if(damageType == "magical")
        {
            magicalAttackValue = user.GetMagicalAttackValue();
            magicalDefenseValue = target.GetMagicalDefenseValue();                
            physicalAttackValue = 0f + result.GetEnchantPhysicalAttackValue();
        }
        
        // 2. 기본 데미지 공식: (공격력 - 방어력) x (위력/100)        
        float physcalBaseDamage = Mathf.Max(0f, physicalAttackValue - physicalDefenseValue) * skillPower / 100f;
        float magicalBaseDamage = Mathf.Max(0f, magicalAttackValue - magicalDefenseValue) * skillPower / 100f;

        Debug.Log($"physcalBaseDamage: {physcalBaseDamage}, magicalBaseDamage: {magicalBaseDamage}");

        float finalDamage = (physcalBaseDamage + magicalBaseDamage) * BattleSetting.DAMAGE_MULTIPLIER;
        finalDamage = Mathf.Max(1f, finalDamage); // 최소 대미지 1 보장

        Debug.Log($"finalDamage: {finalDamage}");

        // 3. 클래스 상성 보정
        if (IsClassAdvantage(user.className, target.className))
        {
            finalDamage *= advantageDamageMultiplier;

            if (BattleLogManager.Instance != null)
            {
                BattleLogManager.Instance.AddLog($"  <color=#FFFF00>[상성 우위!]</color> 데미지 {advantageDamageMultiplier}배 적용");
            }
        }

        // 4. 치명타 계산
        // 캐릭터의 치명타율 스탯 사용
        float currentCriticalRate = user.stats.GetCriticalRateValue();

        if (UnityEngine.Random.Range(0f, 100f) < currentCriticalRate)
        {
            isCritical = true;
            finalDamage *= criticalDamageMultiplier;

            if (BattleLogManager.Instance != null)
            {
                BattleLogManager.Instance.AddLog($"  <color=#FF4500>[치명타!]</color> 데미지 {criticalDamageMultiplier}배 적용");
            }
        }

        // 가드 효과 적용
        if(result.isGuard)
        {
            switch(result.guardLevel)
            {
                case "low":
                    finalDamage = finalDamage * BattleSetting.GUARD_EFFECT_LOW;
                    break;
                case "medium":
                    finalDamage = finalDamage * BattleSetting.GUARD_EFFECT_MEDIUM;
                    break;
                case "high":
                    finalDamage = finalDamage * BattleSetting.GUARD_EFFECT_HIGH;
                    break;
                case "maximum":
                    finalDamage = finalDamage * BattleSetting.GUARD_EFFECT_MAXIMUM;
                    break;
            }

            result.isGuard = false;
        }

        // 최종 데미지 반올림, 최소 1 보장
        return Mathf.Max(1f, Mathf.Round(finalDamage));
    }

    // 클래스 상성 확인
    private bool IsClassAdvantage(string attackerClass, string targetClass)
    {
        var classInfo = TacticsDataManager.Instance.GetClassInfo(attackerClass);
        if (classInfo != null && classInfo.advantage != null)
        {
            return classInfo.advantage.Contains(targetClass);
        }
        return false;
    }

    // 스킬 정보 문자열로 반환
    public string GetSkillInfoString(Skill skill)
    {
        if (skill == null) return "";

        string info = $"{skill.name}\n";
        info += $"타입: {(skill.type == "active" ? "액티브" : "패시브")}\n";

        if (skill.power > 0)
        {
            info += $"위력: {skill.power} / 히트: {skill.hitCount} / 명중: {skill.accuracyRate}%\n";
        }

        info += $"{skill.description}\n";

        if (skill.costAP > 0 || skill.costPP > 0)
        {
            info += "소모:";
            if (skill.costAP > 0) info += $" AP {skill.costAP}";
            if (skill.costPP > 0) info += $" PP {skill.costPP}";
        }

        return info;
    }

    // 시뮬레이션 모드용 동기 버전 (WebGL 성능 최적화)
    public void CheckPassiveSkillBeforeSkillUseSync(Character actionCharacter, Character user, List<Character> targets, Skill skill, PassiveSkillResult result)
    {
        bool bCheckPassiveSkill = false;

        // 자신에게 세팅된 Action 순회, PP 스킬을 찾고, 조건을 체크한다. 
        foreach (var action in actionCharacter.availableActions)
        {
            // action name으로 스킬 정보를 가져옴. 
            Skill myPassiveSkill = SkillManager.Instance.GetSkillByName(action.action);
            if (myPassiveSkill == null || myPassiveSkill.costPP <= 0)
            {
                continue;
            }

            // 스킬의 조건을 체크한다. 
            // 가드 스킬
            if(myPassiveSkill.checkPhase == "guard")
            {
                foreach (SkillEffect mySkillEffect in myPassiveSkill.effects)
                {
                    if (mySkillEffect.type == "guard")
                    {
                        foreach (SkillEffect effect in skill.effects)
                        {
                            if (effect.type == "damage")
                            {
                                bCheckPassiveSkill = PassiveGuard(actionCharacter, targets[0],skill, myPassiveSkill, effect, mySkillEffect, result);
                            }
                        }                        
                    }
                }
            }
            // 자신이 스킬을 사용 하기전
            else if(myPassiveSkill.checkPhase == "before_skill_use_self")
            {
                if(user == actionCharacter)
                {
                    foreach (SkillEffect mySkillEffect in myPassiveSkill.effects)
                    {
                        if (mySkillEffect.type == "sure_hit")
                        {
                            result.isSureHit = true;                        
                            bCheckPassiveSkill = true;
                        }

                    }                    
                }
            }            
            // 아군이 스킬을 사용 하기전
            else if(myPassiveSkill.checkPhase == "before_skill_use_ally")
            {
                if(actionCharacter != user && actionCharacter.isPlayer == user.isPlayer)
                {
                    foreach (SkillEffect mySkillEffect in myPassiveSkill.effects)
                    {
                        if (mySkillEffect.type == "buff")
                        {
                            user.AddBuff(mySkillEffect.stat, mySkillEffect.value, mySkillEffect.duration);
                        }
                        else if (mySkillEffect.type == "enchant")
                        {
                            result.enchantEffects.Add(mySkillEffect);                            
                        }
                    }
                }
            }

            if(bCheckPassiveSkill)
            {
                actionCharacter.stats.passivePoint -= myPassiveSkill.costPP;
                return;
            }
        }
    }

    public IEnumerator CheckPassiveSkillBeforeSkillUse(Character actionCharacter, Character user, List<Character> targets, Skill skill, PassiveSkillResult result)
    {
        bool bCheckPassiveSkill = false;

        // 자신에게 세팅된 Action 순회, PP 스킬을 찾고, 조건을 체크한다. 
        foreach (var action in actionCharacter.availableActions)
        {
            // action name으로 스킬 정보를 가져옴. 
            Skill myPassiveSkill = SkillManager.Instance.GetSkillByName(action.action);
            if (myPassiveSkill == null || myPassiveSkill.costPP <= 0)
            {
                continue;
            }

            // 스킬의 조건을 체크한다. 
            // 가드 스킬
            if(myPassiveSkill.checkPhase == "guard")
            {
                foreach (SkillEffect mySkillEffect in myPassiveSkill.effects)
                {
                    if (mySkillEffect.type == "guard")
                    {
                        foreach (SkillEffect effect in skill.effects)
                        {
                            if (effect.type == "damage")
                            {
                                bCheckPassiveSkill = PassiveGuard(actionCharacter, targets[0],skill, myPassiveSkill, effect, mySkillEffect, result);
                            }
                        }                        
                    }
                }
            }
            // 자신이 스킬을 사용 하기전
            else if(myPassiveSkill.checkPhase == "before_skill_use_self")
            {
                if(user == actionCharacter)
                {
                    foreach (SkillEffect mySkillEffect in myPassiveSkill.effects)
                    {
                        if (mySkillEffect.type == "sure_hit")
                        {
                            result.isSureHit = true;                        
                            bCheckPassiveSkill = true;
                        }

                    }                    
                }
            }            
            // 아군이 스킬을 사용 하기전
            else if(myPassiveSkill.checkPhase == "before_skill_use_ally")
            {
                if(actionCharacter != user && actionCharacter.isPlayer == user.isPlayer)
                {
                    foreach (SkillEffect mySkillEffect in myPassiveSkill.effects)
                    {
                        if (mySkillEffect.type == "buff")
                        {
                            user.AddBuff(mySkillEffect.stat, mySkillEffect.value, mySkillEffect.duration);
                        }
                        else if (mySkillEffect.type == "enchant")
                        {
                            result.enchantEffects.Add(mySkillEffect);                            

                            if(BattleLogManager.Instance != null)
                            {
                                // 인챈트 헀다고 로그에 추가. 패시브 스킬 이름은 파스텔톤 파란계통으로 표시. 
                                BattleLogManager.Instance.AddLog($" {actionCharacter.characterName}의 <color=#FFA500>{myPassiveSkill.name}</color> <color=#00FF00>[{mySkillEffect.stat} +{mySkillEffect.value} 인챈트 적용!]</color>");
                            }
                        }
                    }
                }
            }

            if(bCheckPassiveSkill)
            {
                actionCharacter.stats.passivePoint -= myPassiveSkill.costPP;
                
                // UI에 스킬 이름 표시
                BattleManager.Instance.ShowSkillName(actionCharacter.isPlayer, myPassiveSkill.name);

                if (BattleLogManager.Instance != null)
                {
                    BattleLogManager.Instance.AddLog($"{actionCharacter.characterName}이(가) <color=#87CEEB>{myPassiveSkill.name}</color>를 발동했습니다.");
                    BattleLogManager.Instance.AddLog($"{actionCharacter.characterName}의 <color=#FFA500>PP가 {myPassiveSkill.costPP} 소모되었습니다.</color> (남은 PP: <color=#90EE90>{actionCharacter.stats.passivePoint}</color>)");
                }

                yield break;
            }
        }

        yield break;
    }

    public bool PassiveGuard(Character actionCharacter, Character target, Skill skill, Skill myPassiveSkill, SkillEffect effect, SkillEffect mySkillEffect, PassiveSkillResult result)
    {
        if (result.isGuard) // 다른 캐릭터가 이미 가드한 경우
        {
            return false;
        }

        if (target == null || actionCharacter.isPlayer != target.isPlayer) // 타겟이 없거나 같은 진영이 아닌 경우
        {
            return false;
        }

        if (mySkillEffect.damageType != effect.damageType) // 데미지 타입이 다른 경우
        {
            return false;
        }
        
        if(myPassiveSkill.traits.Contains("ranged"))
        {
            if(!skill.traits.Contains("ranged"))
            {
                return false;
            }
        }
    
        // 다른 아군 캐릭터인지 체크
        if (mySkillEffect.target == "ally" && target == actionCharacter)
        {
            return false;
        }

        // 자신인지 체크
        if (mySkillEffect.target == "self" && target != actionCharacter)
        {
            return false;
        }

        result.isGuard = true;
        result.guardLevel = mySkillEffect.guardLevel;
        result.passiveCharacter = actionCharacter;

        // 가드 액션 실행행
        actionCharacter.ActionGuard(target);

        return true;
    }

    
    // 시뮬레이션 모드용 동기 버전 (WebGL 성능 최적화)
    public void CheckPassiveSkillAfterSkillUseSync(Character actionCharacter, Character user, List<Character> targets, Skill skill, PassiveSkillResult result)
    {
        bool bCheckPassiveSkill = false;    

        // 자신에게 세팅된 Action 순회, PP 스킬을 찾고, 조건을 체크한다. 
        foreach (var action in actionCharacter.availableActions)
        {
            // action name으로 스킬 정보를 가져옴. 
            Skill myPassiveSkill = SkillManager.Instance.GetSkillByName(action.action);
            if (myPassiveSkill == null || myPassiveSkill.costPP <= 0)
            {
                continue;
            }

            if(myPassiveSkill.checkPhase == "after_skill_use_ally")
            {
                if(actionCharacter == user)
                {
                    return;
                }

                if(myPassiveSkill.target == "chase" && targets.Count > 0 && targets[0].hp > 0 && targets[0].isPlayer != actionCharacter.isPlayer)
                {                    
                    bCheckPassiveSkill = true;
                }                
            }            

            if(bCheckPassiveSkill)
            {
                actionCharacter.stats.passivePoint -= myPassiveSkill.costPP;
                
                // 시뮬레이션 모드에서는 동기 버전으로 스킬 사용
                if (BattleManager.Instance.isSimulationMode)
                {
                    actionCharacter.UseSkillSync(myPassiveSkill.name, targets);
                }
                else
                {
                    // 일반 모드에서는 코루틴으로 처리 (하지만 동기 버전에서는 호출하지 않음)
                    StartCoroutine(actionCharacter.UseSkill(myPassiveSkill.name, targets));
                }

                return;
            }
        }
    }

    public IEnumerator CheckPassiveSkillAfterSkillUse(Character actionCharacter, Character user, List<Character> targets, Skill skill, PassiveSkillResult result)
    {
        bool bCheckPassiveSkill = false;    

        // 자신에게 세팅된 Action 순회, PP 스킬을 찾고, 조건을 체크한다. 
        foreach (var action in actionCharacter.availableActions)
        {
            // action name으로 스킬 정보를 가져옴. 
            Skill myPassiveSkill = SkillManager.Instance.GetSkillByName(action.action);
            if (myPassiveSkill == null || myPassiveSkill.costPP <= 0)
            {
                continue;
            }

            if(myPassiveSkill.checkPhase == "after_skill_use_ally")
            {
                if(actionCharacter == user)
                {
                    yield break;
                }

                if(myPassiveSkill.target == "chase" && targets.Count > 0 && targets[0].hp > 0 && targets[0].isPlayer != actionCharacter.isPlayer)
                {                    
                    bCheckPassiveSkill = true;
                }                
            }            

            if(bCheckPassiveSkill)
            {
                actionCharacter.stats.passivePoint -= myPassiveSkill.costPP;
                
                // UI에 스킬 이름 표시
                BattleManager.Instance.ShowSkillName(actionCharacter.isPlayer, myPassiveSkill.name);

                if (BattleLogManager.Instance != null)
                {
                    BattleLogManager.Instance.AddLog($"{actionCharacter.characterName}이(가) <color=#87CEEB>{myPassiveSkill.name}</color>를 발동했습니다.");
                    BattleLogManager.Instance.AddLog($"{actionCharacter.characterName}의 <color=#FFA500>PP가 {myPassiveSkill.costPP} 소모되었습니다.</color> (남은 PP: <color=#90EE90>{actionCharacter.stats.passivePoint}</color>)");
                }

                // UseSkill은 코루틴이므로 yield return으로 완료까지 대기
                yield return StartCoroutine(actionCharacter.UseSkill(myPassiveSkill.name, targets));

                yield break;
            }
        }
    }
}

