using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 효과 데이터
/// </summary>
[Serializable]
public class ActionEffect
{
    public string type;              // "damage", "buff", "debuff", "heal", "status", "guard", "protect", "taunt", "restore_pp"
    public float value;              // 효과 수치
    public string damageType;        // "physical", "magical"
    public string element;           // "fire", "ice", "lightning", "dark", "light", ""
    public string stat;              // "physical_defense", "magical_defense", "attack", "speed", "guard_rate"
    public string target;            // "self", "ally", "enemy", "attacker", "enemy_front_row"
    public int duration;             // 지속 턴 수
    public string statusName;        // "stun", "poison", "burn", "freeze", "sleep"
    public float chance;             // 발동 확률 (%)
    public string condition;         // "on_hit", "on_kill" 등
    public string guardLevel;        // "low", "medium", "high"
}

/// <summary>
/// 스킬(액션) 데이터
/// </summary>
[Serializable]
public class Action
{
    public string id;                // 고유 ID
    public string name;              // 스킬 이름
    public string type;              // "active", "passive"
    public string damageType;        // "physical", "magical", ""
    public float power;              // 위력
    public int hitCount;             // 히트 수
    public float accuracyRate;       // 명중 배율 (%)
    public string description;       // 설명
    public string target;            // "single_enemy", "all_enemies", "single_ally", "all_allies", "self"
    public string buttonType;        // "attack", "support", "debuff"
    public string animation;         // 애니메이션 클립 이름
    public int costAP;               // 소모 AP (Action Point)
    public int costPP;               // 소모 PP
    public string triggerTiming;     // 패시브 발동 시점: "before_hit", "after_hit", "battle_start", "before_ally_hit"
    public string triggerCondition;  // 패시브 발동 조건: "physical_attack", "magical_attack", "ranged_physical_attack"
    public List<ActionEffect> effects; // 효과 리스트
    public List<string> traits;      // 특성: "ranged", "simultaneous_activation_limit", "sure_hit"
}

/// <summary>
/// 스킬 컬렉션
/// </summary>
[Serializable]
public class ActionCollection
{
    public List<Action> actions;
    
    public ActionCollection()
    {
        actions = new List<Action>();
    }
}

