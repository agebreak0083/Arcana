using System.Collections.Generic;
using System.Linq;

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

        switch (selectType)
        {
            case SelectType.Single:
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
                break;
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
                break;
            case SelectType.Column:
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
                break;
            case SelectType.Column:
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
                break;
            case SelectType.Column:
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
                break;
            case SelectType.Column:
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
