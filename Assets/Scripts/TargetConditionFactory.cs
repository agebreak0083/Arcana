using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// 조건 문자열을 파싱하여 적절한 필터/선택기를 생성하는 Factory
/// </summary>
public static class TargetConditionFactory
{
    /// <summary>
    /// Condition2 문자열을 파싱하여 필터 생성
    /// </summary>
    public static ITargetFilter CreateFilter(string condition)
    {
        if (string.IsNullOrEmpty(condition) || condition == "조건 없음")
            return null;

        // HP 비율 필터
        if (condition.Contains("HP") && condition.Contains("%"))
        {
            var match = Regex.Match(condition, @"HP.*?(\d+)%\s*(이하|이상)");
            if (match.Success)
            {
                float threshold = float.Parse(match.Groups[1].Value) / 100f;
                bool isAbove = match.Groups[2].Value == "이상";
                return new HPRatioFilter(threshold, isAbove);
            }
        }

        // AP 필터
        if (condition.Contains("AP가"))
        {
            var match = Regex.Match(condition, @"AP가\s*(\d+)\s*(이하|이상)");
            if (match.Success)
            {
                int threshold = int.Parse(match.Groups[1].Value);
                bool isAbove = match.Groups[2].Value == "이상";
                return new APFilter(threshold, isAbove);
            }

            // "AP가 0인"
            if (condition.Contains("AP가 0"))
            {
                return new APFilter(0, false); // 0 이하 = 0
            }
        }

        // PP 필터
        if (condition.Contains("PP가"))
        {
            var match = Regex.Match(condition, @"PP가\s*(\d+)\s*(이하|이상)");
            if (match.Success)
            {
                int threshold = int.Parse(match.Groups[1].Value);
                bool isAbove = match.Groups[2].Value == "이상";
                return new PPFilter(threshold, isAbove);
            }

            if (condition.Contains("PP가 0"))
            {
                return new PPFilter(0, false);
            }
        }

        // 대열 필터
        // 전후열에 선 필터 (같은 열에 전열과 후열 캐릭터가 모두 있는 경우)
        if (condition.Contains("전후열에 선"))
        {
            return new FrontBackRowFilter();
        }
        if (condition.Contains("전열"))
        {
            return new FormationFilter(true);
        }
        if (condition.Contains("후열"))
        {
            return new FormationFilter(false);
        }

        // 편성 인원 (ex. 적이 2명 이상 / 아군이 3명 이하)
        {
            // 한글 문자를 포함한 패턴 매칭
            var match = Regex.Match(condition, @"([가-힣]+)이 (\d+)명 (이상|이하)");
            if (match.Success)
            {
                string target = match.Groups[1].Value;
                int count = int.Parse(match.Groups[2].Value);
                bool isAbove = match.Groups[3].Value == "이상";
                return new PersonCountFilter(target, count, isAbove);
            }
        }

        return null;
    }

    /// <summary>
    /// Condition1 문자열을 파싱하여 선택기 생성
    /// </summary>
    public static ITargetSelector CreateSelector(string condition)
    {
        if (string.IsNullOrEmpty(condition) || condition == "조건 없음")
            return new PositionBasedSelector(); // 기본: 위치 기반

        // HP 관련 선택
        if (condition.Contains("HP가 가장 낮은"))
        {
            return new HPBasedSelector(true);
        }
        if (condition.Contains("HP가 가장 높은"))
        {
            return new HPBasedSelector(false);
        }
        if (condition.Contains("HP 비율이 가장 낮은"))
        {
            return new HPRatioSelector(true);
        }
        if (condition.Contains("HP 비율이 가장 높은"))
        {
            return new HPRatioSelector(false);
        }

        // AP 관련 선택
        if (condition.Contains("AP가 가장 낮은"))
        {
            return new APBasedSelector(true);
        }
        if (condition.Contains("AP가 가장 높은"))
        {
            return new APBasedSelector(false);
        }

        // 스탯 기반 선택
        if (condition.Contains("물리 공격력이 가장 높은"))
        {
            return new StatBasedSelector(StatBasedSelector.StatType.PhysicalAttack, false);
        }
        if (condition.Contains("물리 공격력이 가장 낮은"))
        {
            return new StatBasedSelector(StatBasedSelector.StatType.PhysicalAttack, true);
        }
        if (condition.Contains("마법 공격력이 가장 높은"))
        {
            return new StatBasedSelector(StatBasedSelector.StatType.MagicalAttack, false);
        }
        if (condition.Contains("마법 공격력이 가장 낮은"))
        {
            return new StatBasedSelector(StatBasedSelector.StatType.MagicalAttack, true);
        }
        if (condition.Contains("물리 방어력이 가장 높은"))
        {
            return new StatBasedSelector(StatBasedSelector.StatType.PhysicalDefense, false);
        }
        if (condition.Contains("물리 방어력이 가장 낮은"))
        {
            return new StatBasedSelector(StatBasedSelector.StatType.PhysicalDefense, true);
        }
        if (condition.Contains("마법 방어력이 가장 높은"))
        {
            return new StatBasedSelector(StatBasedSelector.StatType.MagicalDefense, false);
        }
        if (condition.Contains("마법 방어력이 가장 낮은"))
        {
            return new StatBasedSelector(StatBasedSelector.StatType.MagicalDefense, true);
        }
        if (condition.Contains("행동 속도가 가장 높은"))
        {
            return new StatBasedSelector(StatBasedSelector.StatType.ActionSpeed, false);
        }
        if (condition.Contains("행동 속도가 가장 낮은"))
        {
            return new StatBasedSelector(StatBasedSelector.StatType.ActionSpeed, true);
        }
        if (condition.Contains("치명타율이 가장 높은"))
        {
            return new StatBasedSelector(StatBasedSelector.StatType.CriticalRate, false);
        }
        if (condition.Contains("치명타율이 가장 낮은"))
        {
            return new StatBasedSelector(StatBasedSelector.StatType.CriticalRate, true);
        }

        // 편성 인원 조건 (Condition1에서도 사용 가능)
        {
            var match = Regex.Match(condition, @"([가-힣]+)이 (\d+)명 (이상|이하)");
            if (match.Success)
            {
                // 필터를 적용한 후 기본 선택기를 사용하는 선택기 반환
                string target = match.Groups[1].Value;
                int count = int.Parse(match.Groups[2].Value);
                bool isAbove = match.Groups[3].Value == "이상";
                var filter = new PersonCountFilter(target, count, isAbove);
                return new FilterBasedSelector(filter);
            }
        }

        // 기마 계열 우선 선택
        if (condition.Contains("기마 계열"))
        {
            return new CavalryClassSelector();
        }

        // 인원수가 가장 많은 열의 [적/아군] 우선
        if (condition.Contains("인원수가 가장 많은 열") || condition.Contains("인원이 가장 많은 열"))
        {
            bool isEnemy = condition.Contains("적");
            return new MostPopulatedColumnSelector(isEnemy);
        }

        // 인원수가 가장 적은 열의 [적/아군] 우선
        if (condition.Contains("인원수가 가장 적은 열") || condition.Contains("인원이 가장 적은 열"))
        {
            bool isEnemy = condition.Contains("적");
            return new LeastPopulatedColumnSelector(isEnemy);
        }

        // TODO: 병종, 대열 등 추가 선택기 구현

        Debug.LogWarning($"[TargetConditionFactory] 미구현 Condition1: {condition}");
        return new PositionBasedSelector(); // 기본값
    }
}
