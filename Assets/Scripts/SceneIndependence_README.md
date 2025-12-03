# 씬 독립성 리팩토링 완료

## 📋 개요
모든 Manager에서 `DontDestroyOnLoad`를 제거하여 **BattleScene**과 **TacticsScene**을 완전히 독립적으로 만들었습니다.

## 🔧 수정된 파일들

### 1. **TacticsDataManager.cs**
- `DontDestroyOnLoad` 제거
- 싱글톤 중복 체크 제거
- 각 씬에서 독립적인 인스턴스 생성

```csharp
void Awake()
{
    // 씬마다 독립적인 인스턴스 사용
    Instance = this;
    LoadAllData();
}
```

### 2. **BattleManager.cs**
- `DontDestroyOnLoad` 제거
- 각 씬에서 독립적인 인스턴스 생성

```csharp
void Awake()
{
    // 씬마다 독립적인 인스턴스 사용
    Instance = this;
    // ... Manager 초기화
}
```

### 3. **StrategyManager.cs**
- `DontDestroyOnLoad` 제거
- 싱글톤 중복 체크 제거

### 4. **SkillManager.cs**
- `DontDestroyOnLoad` 제거
- 싱글톤 중복 체크 제거

### 5. **ClassManager.cs**
- `DontDestroyOnLoad` 제거
- 싱글톤 중복 체크 제거

### 6. **UserDataManager.cs**
- `DontDestroyOnLoad` 제거
- 각 씬에서 파일로부터 데이터 로드

### 7. **TacticsUIManager.cs**
- 불필요한 TacticsDataManager fallback 로직 제거
- 간결한 Start 메서드

## 🎯 변경 사항 요약

### Before (기존)
- Manager들이 `DontDestroyOnLoad`로 씬 전환 시 유지됨
- 싱글톤 패턴으로 중복 인스턴스 방지
- 씬 간 데이터 공유를 메모리로 처리

### After (변경 후)
- **각 씬마다 새로운 Manager 인스턴스 생성**
- **데이터는 파일(JSON)로만 공유**
- **씬 전환 시 모든 GameObject 파괴 및 재생성**

## 📁 데이터 흐름

### TacticsScene → BattleScene
1. `TacticsUIManager.OnRunBattleClicked()`
2. `TacticsDataManager.SaveFormationToTacticsFile()` - **tactics.json 저장**
3. `SceneManager.LoadScene("BattleScene")`
4. BattleScene 로드
5. `TacticsDataManager.LoadFormationFromTacticsFile()` - **tactics.json 로드**
6. 전투 시작

### BattleScene → TacticsScene
1. 전투 종료 (승리/패배)
2. NextBattle 버튼 클릭
3. `SceneManager.LoadScene("TacticsScene")`
4. TacticsScene 로드
5. `TacticsDataManager.LoadFormationFromTacticsFile()` - **tactics.json 로드**
6. UI 복원

## ✅ 장점

1. **씬 독립성**: 각 씬이 완전히 독립적으로 동작
2. **메모리 관리**: 씬 전환 시 모든 리소스 정리
3. **디버깅 용이**: 각 씬을 개별적으로 테스트 가능
4. **데이터 안정성**: 파일 기반 데이터 공유로 안정성 향상
5. **확장성**: 새로운 씬 추가 시 기존 씬에 영향 없음

## ⚠️ 주의사항

1. **데이터 저장 필수**: 씬 전환 전 반드시 데이터를 파일에 저장해야 함
2. **로드 시간**: 각 씬에서 Manager 초기화 및 데이터 로드 시간 발생
3. **파일 의존성**: tactics.json 파일이 손상되면 데이터 손실 가능

## 🔄 씬 전환 흐름도

```
TacticsScene
    ↓ (SaveFormationToTacticsFile)
tactics.json 저장
    ↓ (LoadScene)
BattleScene
    ↓ (LoadFormationFromTacticsFile)
tactics.json 로드
    ↓ (전투 진행)
전투 종료
    ↓ (LoadScene)
TacticsScene
    ↓ (LoadFormationFromTacticsFile)
tactics.json 로드
```

## 📝 추가 작업 필요 사항

- [ ] 전투 결과 저장 (승리/패배, 획득 보상 등)
- [ ] 에러 처리 강화 (파일 로드 실패 시)
- [ ] 로딩 화면 추가 (씬 전환 시)
