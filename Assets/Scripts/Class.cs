using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ClassStats
{
    public string hp;                   // HP 등급 (S~E)
    public string physicalAttack;       // 물리공격 등급
    public string physicalDefense;      // 물리방어 등급
    public float physicalDefenseBuff;  // 물리방어 버프 수치
    public string magicalAttack;        // 마법공격 등급
    public string magicalDefense;       // 마법방어 등급
    public float magicalDefenseBuff;    // 마법방어 버프 수치
    public string accuracy;             // 명중 등급
    public string evasion;              // 회피 등급
    public string criticalRate;         // 치명타율 등급
    public string guardRate;            // 가드율 등급
    public string actionSpeed;          // 행동속도 등급
    public int actionPoint;             // 행동 포인트
    public int passivePoint;            // 패시브 포인트

    // 등급을 수치로 변환 (S=6, A=5, B=4, C=3, D=2, E=1)
    public int GetStatValue(string grade)
    {
        switch (grade.ToUpper())
        {
            case "S": return 6;
            case "A": return 5;
            case "B": return 4;
            case "C": return 3;
            case "D": return 2;
            case "E": return 1;
            case "F": return 0;
            default: return 0; // 기본값 0
        }
    }

    // 등급별 실제 스탯 수치 계산 (레벨 1 기준)
    public float GetHPValue() => GetStatValue(hp) * 100f;                      // HP: 100~600
    public float GetPhysicalAttackValue() => 40f + GetStatValue(physicalAttack) * 10f; // 물리공격: 50~100
    public float GetPhysicalDefenseValue() => GetStatValue(physicalDefense) * 5f; // 물리방어: 5~30
    public float GetMagicalAttackValue() => 40f + GetStatValue(magicalAttack) * 10f;    // 마법공격: 50~100
    public float GetMagicalDefenseValue() => GetStatValue(magicalDefense) * 5f;   // 마법방어: 5~30
    public float GetAccuracyValue() => 80f + GetStatValue(accuracy) * 3f;         // 명중: 83~98%
    public float GetEvasionValue() => GetStatValue(evasion) * 6f;                 // 회피: 6~36%
    public float GetCriticalRateValue() => GetStatValue(criticalRate) * 2f;       // 치명타: 2~12%
    public float GetGuardRateValue() => GetStatValue(guardRate) * 5f;             // 가드: 5~30%
    public float GetActionSpeedValue() => 50f + GetStatValue(actionSpeed) * 10f;  // 행동속도: 60~110

    /// <summary>
    /// ClassStats 객체의 복사본을 생성 (Deep Copy)
    /// </summary>
    public ClassStats Clone()
    {
        return new ClassStats
        {
            hp = this.hp,
            physicalAttack = this.physicalAttack,
            physicalDefense = this.physicalDefense,
            magicalAttack = this.magicalAttack,
            magicalDefense = this.magicalDefense,
            accuracy = this.accuracy,
            evasion = this.evasion,
            criticalRate = this.criticalRate,
            guardRate = this.guardRate,
            actionSpeed = this.actionSpeed,
            actionPoint = this.actionPoint,
            passivePoint = this.passivePoint
        };
    }
}

[Serializable]
public class CharacterClass
{
    public string name;                 // 직업 이름
    public string description;          // 직업 설명
    public List<string> advantage;      // 유리한 상성 직업 목록
    public ClassStats stats;            // 스테이터스
}

[Serializable]
public class ClassCollection
{
    public List<CharacterClass> classes;

    public ClassCollection()
    {
        classes = new List<CharacterClass>();
    }
}

