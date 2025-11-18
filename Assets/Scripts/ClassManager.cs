using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class ClassManager : MonoBehaviour
{
    private ClassCollection classCollection;
    
    public static ClassManager Instance { get; private set; }
    
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
        
        LoadClasses();
    }
    
    // JSON 파일에서 직업 데이터 로드
    private void LoadClasses()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Class");
        
        if (jsonFile == null)
        {
            Debug.LogError("Class.json 파일을 찾을 수 없습니다!");
            classCollection = new ClassCollection();
            return;
        }
        
        classCollection = JsonUtility.FromJson<ClassCollection>(jsonFile.text);
        
        if (classCollection != null && classCollection.classes != null)
        {
            Debug.Log($"직업 데이터 로드 완료: {classCollection.classes.Count}개의 직업");
            
            // 로드된 직업 정보 출력
            foreach (var characterClass in classCollection.classes)
            {
                Debug.Log($"[{characterClass.id}] {characterClass.name} - AP:{characterClass.baseAP} PP:{characterClass.basePP}");
            }
        }
        else
        {
            Debug.LogError("직업 데이터를 파싱하는데 실패했습니다!");
            classCollection = new ClassCollection();
        }
    }
    
    // ID로 직업 가져오기
    public CharacterClass GetClassById(string classId)
    {
        if (classCollection == null || classCollection.classes == null)
            return null;
            
        return classCollection.classes.Find(c => c.id == classId);
    }
    
    // 이름으로 직업 가져오기
    public CharacterClass GetClassByName(string className)
    {
        if (classCollection == null || classCollection.classes == null)
            return null;
            
        return classCollection.classes.Find(c => c.name == className);
    }
    
    // 모든 직업 가져오기
    public List<CharacterClass> GetAllClasses()
    {
        if (classCollection == null || classCollection.classes == null)
            return new List<CharacterClass>();
            
        return new List<CharacterClass>(classCollection.classes);
    }
    
    // 직업 정보를 문자열로 반환 (디버그/UI용)
    public string GetClassInfoString(CharacterClass characterClass)
    {
        if (characterClass == null)
            return "직업 정보 없음";
            
        string info = $"=== {characterClass.name} ===\n";
        info += $"{characterClass.description}\n\n";
        info += $"AP: {characterClass.baseAP}  PP: {characterClass.basePP}\n\n";
        info += "[ 스테이터스 ]\n";
        info += $"HP: {characterClass.stats.hp}\n";
        info += $"물리공격: {characterClass.stats.physicalAttack}\n";
        info += $"물리방어: {characterClass.stats.physicalDefense}\n";
        info += $"마법공격: {characterClass.stats.magicalAttack}\n";
        info += $"마법방어: {characterClass.stats.magicalDefense}\n";
        info += $"명중: {characterClass.stats.accuracy}\n";
        info += $"회피: {characterClass.stats.evasion}\n";
        info += $"치명타율: {characterClass.stats.criticalRate}\n";
        info += $"가드율: {characterClass.stats.guardRate}\n";
        info += $"행동속도: {characterClass.stats.actionSpeed}\n";
        
        return info;
    }
    
    // 직업의 실제 스탯 수치 반환 (레벨 1 기준)
    public void ApplyClassStatsToCharacter(Character character, string classId)
    {
        CharacterClass characterClass = GetClassById(classId);
        
        if (characterClass == null)
        {
            Debug.LogWarning($"직업 ID '{classId}'를 찾을 수 없습니다.");
            return;
        }
        
        // 기본 스탯 적용
        character.maxHp = characterClass.stats.GetHPValue();
        character.hp = character.maxHp;
        character.attackPower = characterClass.stats.GetPhysicalAttackValue();
        character.defense = characterClass.stats.GetPhysicalDefenseValue();
        character.magicPower = characterClass.stats.GetMagicalAttackValue();
        character.speed = characterClass.stats.GetActionSpeedValue();
        
        // AP, PP 적용
        character.actionPoint = characterClass.baseAP;
        character.passivePoint = characterClass.basePP;
        
        Debug.Log($"{character.characterName}에게 {characterClass.name} 직업 스탯 적용 완료");
    }
    
    // 직업의 스킬 ID 목록 가져오기
    public List<string> GetClassSkillIds(string classId)
    {
        CharacterClass characterClass = GetClassById(classId);
        
        if (characterClass == null || characterClass.skillIds == null)
        {
            Debug.LogWarning($"직업 ID '{classId}'의 스킬 목록을 찾을 수 없습니다.");
            return new List<string>();
        }
        
        return new List<string>(characterClass.skillIds);
    }
    
    // 직업의 스킬 목록을 Skill 객체로 가져오기
    public List<Skill> GetClassSkills(string classId)
    {
        List<string> skillIds = GetClassSkillIds(classId);
        List<Skill> skills = new List<Skill>();
        
        if (SkillManager.Instance == null)
        {
            Debug.LogWarning("SkillManager가 초기화되지 않았습니다.");
            return skills;
        }
        
        foreach (string skillId in skillIds)
        {
            Skill skill = SkillManager.Instance.GetSkillById(skillId);
            if (skill != null)
            {
                skills.Add(skill);
            }
        }
        
        return skills;
    }
}

