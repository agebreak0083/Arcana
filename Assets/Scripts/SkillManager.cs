using System.Collections.Generic;
using UnityEngine;
using Arcana.Tactics;

/// <summary>
/// 스킬 데이터를 로드하고 관리하는 매니저
/// </summary>
[DefaultExecutionOrder(-100)]
public class SkillManager : MonoBehaviour
{
    private Dictionary<string, List<Skill>> skillsByClass = new Dictionary<string, List<Skill>>();
    private List<Skill> allSkills = new List<Skill>();

    public static SkillManager Instance { get; private set; }

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

    // ID로 스킬 가져오기
    public Skill GetSkillById(string id)
    {
        return allSkills.Find(s => s.id == id);
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
    public void ApplySkillEffects(Skill skill, Character user, Character target)
    {
        if (skill == null || user == null) return;

        // 각 효과 적용
        foreach (SkillEffect effect in skill.effects)
        {
            ApplyEffect(effect, user, target, skill);
        }
    }

    // 개별 효과 적용
    private void ApplyEffect(SkillEffect effect, Character user, Character target, Skill skill)
    {
        switch (effect.type)
        {
            case "damage":
                if (target != null)
                {
                    // Skill.power 대신 effect.value를 사용 (JSON 구조상 power 필드가 없음)
                    bool isCritical;
                    float damage = CalculateDamage(effect.value, user, target, effect.damageType, out isCritical);
                    target.TakeDamage(damage, isCritical);
                    Debug.Log($"{target.characterName}에게 {damage} 데미지!{(isCritical ? " (크리티컬!)" : "")}");

                    // 전투 로그에 데미지 기록
                    if (BattleLogManager.Instance != null)
                    {
                        BattleLogManager.Instance.LogDamage(target.characterName, damage);
                    }
                }
                break;

            case "heal":
                if (target != null)
                {
                    target.Heal(effect.value);
                    Debug.Log($"{target.characterName}의 HP {effect.value} 회복!");

                    // 전투 로그에 회복 기록
                    if (BattleLogManager.Instance != null)
                    {
                        BattleLogManager.Instance.LogHeal(target.characterName, effect.value);
                    }
                }
                break;

            case "buff":
                Debug.Log($"{effect.stat} +{effect.value}% 버프 적용! (지속: {effect.duration}턴)");
                // TODO: 실제 버프 시스템 구현
                break;

            case "debuff":
                Debug.Log($"{effect.stat} {effect.value}% 디버프 적용! (지속: {effect.duration}턴)");
                // TODO: 실제 디버프 시스템 구현
                break;

            case "status":
                Debug.Log($"{effect.statusName} 상태이상 부여! (확률: {effect.chance}%)");
                // TODO: 상태이상 시스템 구현
                break;

            case "restore_pp":
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
    private float CalculateDamage(float skillPower, Character user, Character target, string damageType, out bool isCritical)
    {
        isCritical = false;

        // 1. 공격력 및 방어력 계산
        float attackValue = 0f;
        float defenseValue = 0f;

        if (damageType == "magical")
        {
            attackValue = user.stats.GetMagicalAttackValue();
            defenseValue = target.stats.GetMagicalDefenseValue();
        }
        else // physical or default
        {
            attackValue = user.stats.GetPhysicalAttackValue();
            defenseValue = target.stats.GetPhysicalDefenseValue();
        }

        // 2. 기본 데미지 공식: (공격력 - 방어력) x (위력/100)
        // 방어력이 공격력보다 높으면 최소 1 데미지 보장
        float baseDamage = Mathf.Max(1f, attackValue - defenseValue);

        // 스킬 위력이 0이면 데미지도 0 (버프/디버프 스킬 등)
        if (skillPower <= 0) return 0;

        float finalDamage = baseDamage * (skillPower / 100f);

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
}

