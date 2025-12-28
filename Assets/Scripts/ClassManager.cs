using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class ClassManager : MonoBehaviour
{
    private ClassCollection classCollection;

    public static ClassManager Instance;

    void Awake()
    {
        // 씬마다 독립적인 인스턴스 사용
        Instance = this;
        LoadClasses();
    }

    // JSON 파일에서 직업 데이터 로드
    private void LoadClasses()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Table/ClassList");

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
                Debug.Log($"{characterClass.name} - AP:{characterClass.stats.actionPoint} PP:{characterClass.stats.passivePoint}");
            }
        }
        else
        {
            Debug.LogError("직업 데이터를 파싱하는데 실패했습니다!");
            classCollection = new ClassCollection();
        }
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
        info += $"AP: {characterClass.stats.actionPoint}  PP: {characterClass.stats.passivePoint}\n\n";
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

    // 직업의 스킬 목록을 Skill 객체로 가져오기
    public List<Skill> GetClassSkills(string className)
    {
        List<Skill> skills = new List<Skill>();

        if (SkillManager.Instance == null)
        {
            Debug.LogWarning("SkillManager가 초기화되지 않았습니다.");
            return skills;
        }

        // SkillManager에서 클래스 이름으로 스킬 가져오기
        skills = SkillManager.Instance.GetSkillsByClassName(className);

        return skills;
    }
}

