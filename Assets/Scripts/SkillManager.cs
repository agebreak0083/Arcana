using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 데이터를 로드하고 관리하는 매니저
/// </summary>
[DefaultExecutionOrder(-100)]
public class SkillManager : MonoBehaviour
{
    private SkillCollection skillCollection;
    
    public static SkillManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        LoadSkills();
    }
    
    // Json 파일에서 스킬 데이터 로드
    public void LoadSkills()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Skills");
        if (jsonFile != null)
        {
            skillCollection = JsonUtility.FromJson<SkillCollection>(jsonFile.text);
            Debug.Log($"스킬 데이터 로드 완료: {skillCollection.skills.Count}개의 스킬");            
        }
        else
        {
            Debug.LogError("Skills.json 파일을 찾을 수 없습니다!");
        }
    }
    
    // ID로 스킬 가져오기
    public Skill GetSkillById(string id)
    {
        if (skillCollection == null) return null;
        return skillCollection.skills.Find(s => s.id == id);
    }
    
    // 이름으로 스킬 가져오기
    public Skill GetSkillByName(string name)
    {
        if (skillCollection == null) return null;
        return skillCollection.skills.Find(s => s.name == name);
    }
    
    // 모든 스킬 가져오기
    public List<Skill> GetAllSkills()
    {
        if (skillCollection == null) return new List<Skill>();
        return skillCollection.skills;
    }
    
    // 타입별 스킬 가져오기 (active/passive)
    public List<Skill> GetSkillsByType(string type)
    {
        if (skillCollection == null) return new List<Skill>();
        return skillCollection.skills.FindAll(s => s.type == type);
    }
    
    // 버튼 타입별 스킬 가져오기
    public List<Skill> GetSkillsByButtonType(string buttonType)
    {
        if (skillCollection == null) return new List<Skill>();
        return skillCollection.skills.FindAll(s => s.buttonType == buttonType);
    }
    
    // 스킬 사용 가능 여부 확인
    public bool CanUseSkill(Skill skill, Character character)
    {
        if (skill == null || character == null) return false;
        
        // AP 확인 (액티브 스킬만)
        if (skill.type == "active" && character.actionPoint < skill.costAP)
        {
            Debug.Log($"{character.characterName}의 AP가 부족합니다. (필요: {skill.costAP})");
            return false;
        }
        
        // PP 확인 (액티브 스킬만)
        if (skill.type == "active" && character.pp < skill.costPP)
        {
            Debug.Log($"{character.characterName}의 PP가 부족합니다. (필요: {skill.costPP})");
            return false;
        }
        
        return true;
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
                    float damage = CalculateDamage(skill.power, user, target, effect.damageType);
                    target.TakeDamage(damage);
                    Debug.Log($"{target.characterName}에게 {damage} 데미지!");
                }
                break;
                
            case "heal":
                if (target != null)
                {
                    target.Heal(effect.value);
                    Debug.Log($"{target.characterName}의 HP {effect.value} 회복!");
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
                user.pp += (int)effect.value;
                Debug.Log($"{user.characterName}의 PP +{effect.value}");
                break;
                
            default:
                Debug.Log($"효과 적용: {effect.type}");
                break;
        }
    }
    
    // 데미지 계산 (간단한 예제)
    private float CalculateDamage(float basePower, Character user, Character target, string damageType)
    {
        float damage = basePower;
        
        // 공격력 반영
        damage += user.attackPower * 0.5f;
        
        // 방어력 반영 (간단한 계산)
        // TODO: 실제 게임에 맞는 데미지 공식 적용
        
        return 50f;
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

