# 타겟 선택 시스템 리팩토링 완료

## 📋 구현 개요

TacticsData.json의 조건들을 처리하기 위해 **Strategy Pattern + Factory Pattern**을 사용하여 확장 가능하고 유지보수하기 쉬운 구조로 구현했습니다.

## 🏗️ 아키텍처

### 1. **TargetFilters.cs** (새 파일)
- `ITargetFilter`: Condition2용 인터페이스 (필터링)
- `ITargetSelector`: Condition1용 인터페이스 (선택)

#### 구현된 필터 (Condition2)
- `HPRatioFilter`: HP 비율 필터 (25%, 50%, 75% 이하/이상)
- `APFilter`: AP 필터 (0, 1~4 이하/이상)
- `PPFilter`: PP 필터
- `FormationFilter`: 대열 필터 (전열/후열)

#### 구현된 선택기 (Condition1)
- `PositionBasedSelector`: 위치 기반 선택 (기본 - 자신의 앞 적)
- `HPBasedSelector`: HP 최소/최대
- `HPRatioSelector`: HP 비율 최소/최대
- `APBasedSelector`: AP 최소/최대
- `StatBasedSelector`: 스탯 기반 (물리공격, 마법공격, 방어력, 속도, 치명타율 등)

### 2. **TargetConditionFactory.cs** (새 파일)
조건 문자열을 파싱하여 적절한 필터/선택기 인스턴스를 생성합니다.

- `CreateFilter(string condition)`: Condition2 → ITargetFilter
- `CreateSelector(string condition)`: Condition1 → ITargetSelector

정규식을 사용하여 유연한 패턴 매칭을 지원합니다.

### 3. **Character.cs** 수정
`GetTarget` 메서드를 간결하게 리팩토링:

```csharp
private Character GetTarget(StrategyAction action)
{
    // 1. 적 리스트 가져오기 (복사본)
    List<Character> candidates = new List<Character>(BattleManager.Instance.GetEnemyTargets(this));
    
    // 2. 사망한 캐릭터 제거
    candidates.RemoveAll(c => c == null || c.hp <= 0);
    
    // 3. Condition2 적용 (필터링)
    var filter = TargetConditionFactory.CreateFilter(action.condition2);
    if (filter != null)
        candidates = filter.Filter(candidates, this);
    
    // 4. Condition1 적용 (선택)
    var selector = TargetConditionFactory.CreateSelector(action.condition1);
    return selector.Select(candidates, this);
}
```

## ✅ 장점

1. **확장성**: 새로운 조건 추가 시 새 클래스만 만들면 됨
2. **가독성**: 각 조건이 독립적인 클래스로 분리되어 이해하기 쉬움
3. **테스트 용이성**: 각 필터/선택기를 독립적으로 테스트 가능
4. **유지보수**: 조건 로직 변경 시 해당 클래스만 수정
5. **단일 책임 원칙**: 각 클래스가 하나의 조건만 처리

## 🔧 추가 구현 필요 사항

현재 기본적인 조건들만 구현되었습니다. 다음 조건들은 필요에 따라 추가 구현 가능:

### Condition2 (필터)
- [ ] 병종 필터 (보병, 기마, 비행 등)
- [ ] 상태이상 필터 (버프, 디버프, 독, 화상 등)
- [ ] 편성 인원 조건
- [ ] 자신의 상태 조건

### Condition1 (선택)
- [ ] 병종 우선 선택
- [ ] 대열 우선 선택 (전열/후열 우선)
- [ ] 인원수 기반 선택

## 📝 사용 예시

```csharp
// TacticsData.json
{
    "condition1": "HP 비율이 가장 낮은 적 우선",
    "condition2": "HP 50% 이하▼인 적"
}

// 실행 흐름:
// 1. 모든 적 리스트 가져오기
// 2. Condition2: HP 50% 이하인 적만 필터링
// 3. Condition1: 필터링된 리스트에서 HP 비율이 가장 낮은 적 선택
```

## 🐛 현재 Lint 에러

Unity가 새 파일을 아직 컴파일하지 않아 `TargetConditionFactory`를 찾을 수 없다는 에러가 발생할 수 있습니다. Unity 에디터로 돌아가면 자동으로 컴파일되어 해결됩니다.
