using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬(액션) 데이터를 로드하고 관리하는 매니저
/// </summary>
[DefaultExecutionOrder(-100)]
public class ActionManager : MonoBehaviour
{
    private ActionCollection actionCollection;
    
    public static ActionManager Instance { get; private set; }
    
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
        
        LoadActions();
    }
    
    // Json 파일에서 스킬 데이터 로드
    public void LoadActions()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Actions");
        if (jsonFile != null)
        {
            actionCollection = JsonUtility.FromJson<ActionCollection>(jsonFile.text);
            Debug.Log($"스킬 데이터 로드 완료: {actionCollection.actions.Count}개의 스킬");            
        }
        else
        {
            Debug.LogError("Actions.json 파일을 찾을 수 없습니다!");
        }
    }
    
    // ID로 스킬 가져오기
    public Action GetActionById(string id)
    {
        if (actionCollection == null) return null;
        return actionCollection.actions.Find(a => a.id == id);
    }
    
    // 이름으로 스킬 가져오기
    public Action GetActionByName(string name)
    {
        if (actionCollection == null) return null;
        return actionCollection.actions.Find(a => a.name == name);
    }
    
    // 모든 스킬 가져오기
    public List<Action> GetAllActions()
    {
        if (actionCollection == null) return new List<Action>();
        return actionCollection.actions;
    }
    
    // 타입별 스킬 가져오기 (active/passive)
    public List<Action> GetActionsByType(string type)
    {
        if (actionCollection == null) return new List<Action>();
        return actionCollection.actions.FindAll(a => a.type == type);
    }
    
    // 버튼 타입별 스킬 가져오기
    public List<Action> GetActionsByButtonType(string buttonType)
    {
        if (actionCollection == null) return new List<Action>();
        return actionCollection.actions.FindAll(a => a.buttonType == buttonType);
    }
    
    // 스킬 사용 가능 여부 확인
    public bool CanUseAction(Action action, Character character)
    {
        if (action == null || character == null) return false;
        
        // AP 확인 (액티브 스킬만)
        if (action.type == "active" && character.actionPoint < action.costAP)
        {
            Debug.Log($"{character.characterName}의 AP가 부족합니다. (필요: {action.costAP})");
            return false;
        }
        
        // PP 확인 (액티브 스킬만)
        if (action.type == "active" && character.pp < action.costPP)
        {
            Debug.Log($"{character.characterName}의 PP가 부족합니다. (필요: {action.costPP})");
            return false;
        }
        
        return true;
    }
    
    // 스킬 효과 적용 (예제)
    public void ApplyActionEffects(Action action, Character user, Character target)
    {
        if (action == null || user == null) return;                
        
        // 각 효과 적용
        foreach (ActionEffect effect in action.effects)
        {
            ApplyEffect(effect, user, target, action);
        }
    }    
    
    // 개별 효과 적용
    private void ApplyEffect(ActionEffect effect, Character user, Character target, Action action)
    {
        switch (effect.type)
        {
            case "damage":
                if (target != null)
                {
                    float damage = CalculateDamage(action.power, user, target, effect.damageType);
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
        
        return damage;
    }
    
    // 스킬 정보 문자열로 반환
    public string GetActionInfoString(Action action)
    {
        if (action == null) return "";
        
        string info = $"{action.name}\n";
        info += $"타입: {(action.type == "active" ? "액티브" : "패시브")}\n";
        
        if (action.power > 0)
        {
            info += $"위력: {action.power} / 히트: {action.hitCount} / 명중: {action.accuracyRate}%\n";
        }
        
        info += $"{action.description}\n";
        
        if (action.costAP > 0 || action.costPP > 0)
        {
            info += "소모:";
            if (action.costAP > 0) info += $" AP {action.costAP}";
            if (action.costPP > 0) info += $" PP {action.costPP}";
        }
        
        return info;
    }
}

