using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 타겟 필터링 인터페이스
/// Condition2에서 사용: 조건에 맞는 캐릭터만 남김
/// </summary>
public interface ITargetFilter
{
    List<Character> Filter(List<Character> candidates, Character self);
}

public enum SelectType
{
    Single,
    Multiple,
    Row, 
    Column,
    All
}
/// <summary>
/// 타겟 선택 인터페이스
/// Condition1에서 사용: 필터링된 리스트에서 최종 타겟 1명 선택
/// </summary>
public interface ITargetSelector
{
    List<Character> Select(List<Character> candidates, Character self, SelectType selectType = SelectType.Single, int selectCount = 1);
}

public class SelectorHelper
{
    public static List<Character> GetRowTargets(List<Character> candidates, Character target)
    {
        // 우서 조건에 맞는 첫번째 타겟을 찾고, 그 다음에 그 타겟의 행 전체를 타겟팅한다. 
        // 타겟이 1이면, 1,4,7 위치를 타겟팅한다. 2이면, 2,5,8 위치를 타겟팅한다. 3이면, 3,6,9 위치를 타겟팅한다. 
        if (target == null)
        {
            Debug.LogError("GetRowTargets: Target is null");
            return null;
        }

        List<Character> targets = new List<Character>();
        if (target.position == 1 || target.position == 4)
        {
            targets.AddRange(candidates.Where(c => c.position == 1 || c.position == 4).ToList());
        }
        else if (target.position == 2 || target.position == 5)
        {
            targets.AddRange(candidates.Where(c => c.position == 2 || c.position == 5).ToList());
        }
        else if (target.position == 3 || target.position == 6)
        {
            targets.AddRange(candidates.Where(c => c.position == 3 || c.position == 6).ToList());
        }
        return targets;
    }

    public static List<Character> GetColumnTargets(List<Character> candidates, Character target)
    {
        // 우서 조건에 맞는 첫번째 타겟을 찾고, 그 다음에 그 타겟의 열 전체를 타겟팅한다. 
        // 타겟이 1~3 사이면 1,2,3 위치를 타겟팅한다. 4~6이면 4,5,6 위치를 타겟팅한다. 
        if (target == null)
        {
            Debug.LogError("GetColumnTargets: Target is null");
            return null;
        }

        List<Character> targets = new List<Character>();
        if(target.position <= 3)
        {
            targets.AddRange(candidates.Where(c => c.position <= 3).ToList());
        }
        else
        {
            targets.AddRange(candidates.Where(c => c.position > 3).ToList());
        }
        return targets;
    }

}



// ==================================================================================
// Condition2 Filters (필터링)
// ==================================================================================

/// <summary>
/// HP 비율 필터 (이하/이상)
/// </summary>
public class HPRatioFilter : ITargetFilter
{
    private float threshold;
    private bool isAbove; // true: 이상, false: 이하

    public HPRatioFilter(float threshold, bool isAbove)
    {
        this.threshold = threshold;
        this.isAbove = isAbove;
    }

    public List<Character> Filter(List<Character> candidates, Character self)
    {
        return candidates.Where(c =>
        {
            float ratio = c.hp / c.stats.GetHPValue();
            return isAbove ? ratio >= threshold : ratio <= threshold;
        }).ToList();
    }
}

/// <summary>
/// AP 필터 (이하/이상)
/// </summary>
public class APFilter : ITargetFilter
{
    private int threshold;
    private bool isAbove;

    public APFilter(int threshold, bool isAbove)
    {
        this.threshold = threshold;
        this.isAbove = isAbove;
    }

    public List<Character> Filter(List<Character> candidates, Character self)
    {
        return candidates.Where(c =>
            isAbove ? c.stats.actionPoint >= threshold : c.stats.actionPoint <= threshold
        ).ToList();
    }
}

/// <summary>
/// PP 필터
/// </summary>
public class PPFilter : ITargetFilter
{
    private int threshold;
    private bool isAbove;

    public PPFilter(int threshold, bool isAbove)
    {
        this.threshold = threshold;
        this.isAbove = isAbove;
    }

    public List<Character> Filter(List<Character> candidates, Character self)
    {
        return candidates.Where(c =>
            isAbove ? c.stats.passivePoint >= threshold : c.stats.passivePoint <= threshold
        ).ToList();
    }
}

/// <summary>
/// 대열 필터 (전열/후열)
/// </summary>
public class FormationFilter : ITargetFilter
{
    private bool isFrontRow; // true: 전열(1,2,3), false: 후열(4,5,6)

    public FormationFilter(bool isFrontRow)
    {
        this.isFrontRow = isFrontRow;
    }

    public List<Character> Filter(List<Character> candidates, Character self)
    {
        return candidates.Where(c =>
            isFrontRow ? c.position <= 3 : c.position > 3
        ).ToList();
    }
}

/// <summary>
/// 전후열에 선 필터 (같은 열에 전열과 후열 캐릭터가 모두 있는 경우)
/// 예: position 1(전열)과 4(후열)이 같은 열, position 2(전열)과 5(후열)이 같은 열, position 3(전열)과 6(후열)이 같은 열
/// </summary>
public class FrontBackRowFilter : ITargetFilter
{
    public List<Character> Filter(List<Character> candidates, Character self)
    {
        List<Character> filtered = new List<Character>();
        
        // 각 열(column)별로 확인
        // 열 1: position 1(전열)과 4(후열)
        // 열 2: position 2(전열)과 5(후열)
        // 열 3: position 3(전열)과 6(후열)
        for (int col = 1; col <= 3; col++)
        {
            int frontPos = col;      // 전열: 1, 2, 3
            int backPos = col + 3;    // 후열: 4, 5, 6
            
            // 같은 열에 전열과 후열 캐릭터가 모두 있는지 확인
            bool hasFront = candidates.Any(c => c.position == frontPos && c.hp > 0);
            bool hasBack = candidates.Any(c => c.position == backPos && c.hp > 0);
            
            // 둘 다 있으면 해당 열의 모든 캐릭터를 필터링 결과에 추가
            if (hasFront && hasBack)
            {
                Debug.Log($"FrontBackRowFilter: 열 {col}에 전열과 후열 캐릭터가 모두 있습니다.");
                
                filtered.AddRange(candidates.Where(c => c.position == frontPos || c.position == backPos));
            }
        }
        
        return filtered;
    }
}

// ==================================================================================
// Condition1 Selectors (선택)
// ==================================================================================

/// <summary>
/// 위치 기반 선택 (자신의 앞 적)
/// </summary>
public class PositionBasedSelector : ITargetSelector
{
    public List<Character> Select(List<Character> candidates, Character self, SelectType selectType = SelectType.Single, int selectCount = 1)
    {
        if (candidates.Count == 0) return null;

        List<Character> selected = new List<Character>();

        for(int i = 0; i < selectCount; i++)        
        {
            // 1. 우선 전열(1,2,3)에서 자신의 앞의 적을 찾고
            int targetPosition = ((self.position - 1) % 3) + 1;
            var target = candidates.Find(c => c.position == targetPosition);

            // 2. 없으면 전열의 다른 적
            if (target == null)
            {
                target = candidates.Find(c => c.position <= 3);
            }

            // 3. 없으면 후열의 자신의 앞의 적
            if (target == null)
            {
                int backTargetPosition = targetPosition + 3;
                target = candidates.Find(c => c.position == backTargetPosition);
            }

            // 4. 없으면 후열의 아무나
            if (target == null)
            {
                target = candidates.Find(c => c.position > 3);
            }

            // 5. 그래도 없으면 첫 번째
            if (target == null && candidates.Count > 0)
            {
                target = candidates[0];
            }
            
            selected.Add(target);
            candidates.Remove(target);
        }
        
        return selected;      
    }
}

/// <summary>
/// HP 최소/최대 선택
/// </summary>
public class HPBasedSelector : ITargetSelector
{
    private bool selectLowest; // true: 최소, false: 최대

    public HPBasedSelector(bool selectLowest)
    {
        this.selectLowest = selectLowest;
    }

    public List<Character> Select(List<Character> candidates, Character self, SelectType selectType = SelectType.Single, int selectCount = 1)
    {
        if (candidates.Count == 0) return null;

        List<Character> selected = new List<Character>();
        Character target = null;
        switch (selectType)
        {
            case SelectType.Single:
                selected.Add(selectLowest
                    ? candidates.OrderBy(c => c.hp).First()
                    : candidates.OrderByDescending(c => c.hp).First());
                break;
            case SelectType.Multiple:
                selected.AddRange(selectLowest
                    ? candidates.OrderBy(c => c.hp).Take(selectCount)
                    : candidates.OrderByDescending(c => c.hp).Take(selectCount));
                break;
            case SelectType.Row:
                target = candidates.OrderBy(c => c.hp).First();
                selected.AddRange(SelectorHelper.GetRowTargets(candidates, target));
                break;
            case SelectType.Column:
                target = candidates.OrderBy(c => c.hp).First();
                selected.AddRange(SelectorHelper.GetColumnTargets(candidates, target));
                break;
            case SelectType.All:
                selected.AddRange(candidates);
                break;
        }
        return selected;
    }
}



/// <summary>
/// HP 비율 최소/최대 선택
/// </summary>
public class HPRatioSelector : ITargetSelector
{
    private bool selectLowest;

    public HPRatioSelector(bool selectLowest)
    {
        this.selectLowest = selectLowest;
    }

    public List<Character> Select(List<Character> candidates, Character self, SelectType selectType = SelectType.Single, int selectCount = 1)
    {
        if (candidates.Count == 0) return null;

        List<Character> selected = new List<Character>();
        Character target = null;
        switch (selectType)
        {
            case SelectType.Single:
                selected.Add(selectLowest
                    ? candidates.OrderBy(c => c.hp / c.stats.GetHPValue()).First()
                    : candidates.OrderByDescending(c => c.hp / c.stats.GetHPValue()).First());
                break;
            case SelectType.Multiple:
                selected.AddRange(selectLowest
                    ? candidates.OrderBy(c => c.hp / c.stats.GetHPValue()).Take(selectCount)
                    : candidates.OrderByDescending(c => c.hp / c.stats.GetHPValue()).Take(selectCount));
                break;
            case SelectType.Row:
                target = candidates.OrderBy(c => c.hp / c.stats.GetHPValue()).First();
                selected.AddRange(SelectorHelper.GetRowTargets(candidates, target));
                break;
            case SelectType.Column:
                target = candidates.OrderBy(c => c.hp / c.stats.GetHPValue()).First();
                selected.AddRange(SelectorHelper.GetColumnTargets(candidates, target));
                break;
            case SelectType.All:
                selected.AddRange(candidates);
                break;
        }
        return selected;
    }
}

/// <summary>
/// AP 최소/최대 선택
/// </summary>
public class APBasedSelector : ITargetSelector
{
    private bool selectLowest;

    public APBasedSelector(bool selectLowest)
    {
        this.selectLowest = selectLowest;
    }

    public List<Character> Select(List<Character> candidates, Character self, SelectType selectType = SelectType.Single, int selectCount = 1)
    {
        if (candidates.Count == 0) return null;
        List<Character> selected = new List<Character>();
        Character target = null;
        switch (selectType)
        {
            case SelectType.Single:
                selected.Add(selectLowest
                    ? candidates.OrderBy(c => c.stats.actionPoint).First()
                    : candidates.OrderByDescending(c => c.stats.actionPoint).First());
                break;
            case SelectType.Multiple:
                selected.AddRange(selectLowest
                    ? candidates.OrderBy(c => c.stats.actionPoint).Take(selectCount)
                    : candidates.OrderByDescending(c => c.stats.actionPoint).Take(selectCount));
                break;
            case SelectType.Row:
                target = candidates.OrderBy(c => c.stats.actionPoint).First();
                selected.AddRange(SelectorHelper.GetRowTargets(candidates, target));
                break;
            case SelectType.Column:
                target = candidates.OrderBy(c => c.stats.actionPoint).First();
                selected.AddRange(SelectorHelper.GetColumnTargets(candidates, target));
                break;
            case SelectType.All:
                selected.AddRange(candidates);
                break;
        }

        return selected;
    }
}

/// <summary>
/// 스탯 기반 선택 (물리공격, 마법공격 등)
/// </summary>
public class StatBasedSelector : ITargetSelector
{
    public enum StatType
    {
        PhysicalAttack,
        MagicalAttack,
        PhysicalDefense,
        MagicalDefense,
        ActionSpeed,
        Accuracy,
        Evasion,
        CriticalRate,
        GuardRate
    }

    private StatType statType;
    private bool selectLowest;

    public StatBasedSelector(StatType statType, bool selectLowest)
    {
        this.statType = statType;
        this.selectLowest = selectLowest;
    }

    public List<Character> Select(List<Character> candidates, Character self, SelectType selectType = SelectType.Single, int selectCount = 1)
    {
        if (candidates.Count == 0) return null;

        List<Character> selected = new List<Character>();
        Character target = null;
        switch (selectType)
        {
            case SelectType.Single:
                var ordered = selectLowest
                    ? candidates.OrderBy(c => GetStatValue(c))
                    : candidates.OrderByDescending(c => GetStatValue(c));
                selected.Add(ordered.First());
                break;
            case SelectType.Multiple:
                var orderedMultiple = selectLowest
                    ? candidates.OrderBy(c => GetStatValue(c))
                    : candidates.OrderByDescending(c => GetStatValue(c));
                selected.AddRange(orderedMultiple.Take(selectCount));
                break;
            case SelectType.Row:
                target = candidates.OrderBy(c => GetStatValue(c)).First();
                selected.AddRange(SelectorHelper.GetRowTargets(candidates, target));
                break;
            case SelectType.Column:
                target = candidates.OrderBy(c => GetStatValue(c)).First();
                selected.AddRange(SelectorHelper.GetColumnTargets(candidates, target));
                break;
            case SelectType.All:
                selected.AddRange(candidates);
                break;
        }

        return selected;
    }

    private float GetStatValue(Character c)
    {
        switch (statType)
        {
            case StatType.PhysicalAttack: return c.stats.GetPhysicalAttackValue();
            case StatType.MagicalAttack: return c.stats.GetMagicalAttackValue();
            case StatType.PhysicalDefense: return c.stats.GetPhysicalDefenseValue();
            case StatType.MagicalDefense: return c.stats.GetMagicalDefenseValue();
            case StatType.ActionSpeed: return c.stats.GetActionSpeedValue();
            case StatType.Accuracy: return c.stats.GetAccuracyValue();
            case StatType.Evasion: return c.stats.GetEvasionValue();
            case StatType.CriticalRate: return c.stats.GetCriticalRateValue();
            case StatType.GuardRate: return c.stats.GetGuardRateValue();
            default: return 0f;
        }
    }
}

/// <summary>
/// 필터를 적용한 후 기본 선택기를 사용하는 선택기
/// Condition1에서 필터 조건을 사용할 때 사용
/// </summary>
public class FilterBasedSelector : ITargetSelector
{
    private ITargetFilter filter;
    private ITargetSelector baseSelector;

    public FilterBasedSelector(ITargetFilter filter)
    {
        this.filter = filter;
        this.baseSelector = new PositionBasedSelector();
    }

    public List<Character> Select(List<Character> candidates, Character self, SelectType selectType = SelectType.Single, int selectCount = 1)
    {
        // 먼저 필터 적용
        List<Character> filtered = filter.Filter(candidates, self);
        
        if (filtered.Count == 0)
        {
            return null; // 필터 조건을 만족하지 않음
        }

        // 필터링된 리스트에서 기본 선택기로 선택
        return baseSelector.Select(filtered, self, selectType, selectCount);
    }
}

    public class PersonCountFilter : ITargetFilter
    {
        private string target;
        private int count;
        private bool isAbove;

        public PersonCountFilter(string target, int count, bool isAbove)
        {
            this.target = target;
            this.count = count;
            this.isAbove = isAbove;
        }   

        public List<Character> Filter(List<Character> candidates, Character self)
        {
            // 적이 n명 이상, 이하인 경우
            if(target == "적")
            {
                if( isAbove ? candidates.Count >= count : candidates.Count <= count) // 조건에 맞는 경우
                {
                    return candidates;
                }                
            }
            // 아군이 n명 이상, 이하인 경우
            else if(target == "아군")
            {
                // playerCharacter 중에서 hp >= 0 인 캐릭터만 찾는다.
                List<Character> playerCharacters = BattleManager.Instance.playerCharacters.Where(c => c.hp >= 0).ToList();
                if(isAbove ? playerCharacters.Count >= count : playerCharacters.Count <= count) // 조건에 맞는 경우
                {
                    return candidates;
                }                
            }

            return new List<Character>();
        }
    }

/// <summary>
/// 기마 계열 우선 선택기
/// 기마 계열(현재는 "나이트" 클래스) 캐릭터를 우선 선택하고, 없으면 일반 선택기 사용
/// </summary>
public class CavalryClassSelector : ITargetSelector
{
    private ITargetSelector baseSelector;

    public CavalryClassSelector()
    {
        this.baseSelector = new PositionBasedSelector();
    }

    public List<Character> Select(List<Character> candidates, Character self, SelectType selectType = SelectType.Single, int selectCount = 1)
    {
        if (candidates.Count == 0) return null;

        // 기마 계열 캐릭터 필터링 (현재는 "나이트" 클래스만)
        List<Character> cavalryCharacters = candidates.Where(c => 
            c != null && 
            c.hp > 0 && 
            (c.className == "나이트" || c.className == "Knight")
        ).ToList();

        // 기마 계열 캐릭터가 있으면 우선 선택
        if (cavalryCharacters.Count > 0)
        {
            return baseSelector.Select(cavalryCharacters, self, selectType, selectCount);
        }

        // 기마 계열 캐릭터가 없으면 일반 선택기 사용
        return baseSelector.Select(candidates, self, selectType, selectCount);
    }
}