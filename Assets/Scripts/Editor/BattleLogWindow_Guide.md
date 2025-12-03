# 전투 로그 윈도우 사용 가이드

## 📋 개요
전투 중 발생하는 모든 이벤트를 실시간으로 표시하는 전투 로그 윈도우 시스템입니다.

## 🎯 기능
- **실시간 로그 출력**: 공격, 데미지, 회복, 턴/라운드 정보 표시
- **자동 스크롤**: 새로운 로그가 추가되면 자동으로 맨 아래로 스크롤
- **색상 구분**: 각 이벤트 타입별로 다른 색상으로 표시
- **타임스탬프**: 각 로그에 시간 정보 포함

## 🛠️ 설치 방법

### 1. Prefab 생성
Unity 에디터에서:
1. 상단 메뉴 `GameObject` → `UI` → `Battle Log Window` 클릭
2. Hierarchy에 `BattleLogWindow` 오브젝트가 생성됩니다
3. 자동으로 `Assets/Prefabs/UI/BattleLogWindow.prefab`로 저장됩니다

### 2. 씬에 배치
- 생성된 Prefab을 Battle 씬의 Canvas에 배치하거나
- 이미 Hierarchy에 생성된 BattleLogWindow를 그대로 사용

### 3. 설정 확인
BattleLogWindow 오브젝트의 `BattleLogManager` 컴포넌트에서:
- `Scroll Rect`: ScrollView 참조 (자동 설정됨)
- `Log Text`: 로그 텍스트 참조 (자동 설정됨)
- `Max Log Lines`: 최대 로그 라인 수 (기본값: 50)

## 📊 로그 타입 및 색상

### 라운드 시작
```
[14:30:25] === 라운드 1 시작 ===
```
- 색상: 마젠타 (#FF00FF)

### 턴 시작
```
[14:30:26] --- Hina1의 턴 (Round 1 - Turn 1) ---
```
- 색상: 오렌지 (#FFA500)

### 공격
```
[14:30:27] Hina1이(가) Hina2을(를) 가드 슬래시(으)로 공격했습니다.
```
- 공격자: 골드 (#FFD700)
- 대상: 빨강 (#FF6B6B)
- 스킬: 하늘색 (#87CEEB)

### 데미지
```
[14:30:28] Hina2이(가) 50의 데미지를 입었습니다.
```
- 대상: 빨강 (#FF6B6B)
- 데미지: 진한 빨강 (#FF4444)

### 회복
```
[14:30:29] Hina1이(가) 25의 HP를 회복했습니다.
```
- 대상: 연두색 (#90EE90)
- 회복량: 초록색 (#00FF00)

## 💻 프로그래밍 사용법

### 기본 로그 추가
```csharp
BattleLogManager.Instance.AddLog("커스텀 메시지");
```

### 공격 로그
```csharp
BattleLogManager.Instance.LogAttack("공격자", "대상", "스킬명");
```

### 데미지 로그
```csharp
BattleLogManager.Instance.LogDamage("대상", 50f);
```

### 회복 로그
```csharp
BattleLogManager.Instance.LogHeal("대상", 25f);
```

### 턴/라운드 로그
```csharp
BattleLogManager.Instance.LogTurnStart("캐릭터명", 라운드, 턴);
BattleLogManager.Instance.LogRoundStart(라운드);
```

### 로그 초기화
```csharp
BattleLogManager.Instance.ClearLog();
```

## 🎨 UI 구조
```
BattleLogWindow (400x500, 우측 상단)
├── TitleBar (높이: 40px)
│   └── TitleText ("전투 로그")
└── ScrollView
    ├── Viewport (Mask)
    │   └── Content (Auto-resize)
    │       └── LogText (TextMeshPro)
    └── Scrollbar Vertical
        └── Handle
```

## 📝 커스터마이징

### 위치 변경
`BattleLogWindow`의 RectTransform에서:
- `Anchored Position`: 위치 조정
- 현재: 우측 상단 (-20, -20)

### 크기 변경
`BattleLogWindow`의 RectTransform에서:
- `Size Delta`: 크기 조정
- 현재: (400, 500)

### 색상 변경
`BattleLogManager.cs`의 각 Log 메서드에서 HTML 색상 코드 수정

### 최대 로그 라인 수 변경
Inspector에서 `Max Log Lines` 값 조정 (기본: 50)

## ⚠️ 주의사항
1. BattleLogManager는 Singleton 패턴으로 구현되어 있습니다
2. 씬에 하나의 BattleLogWindow만 존재해야 합니다
3. TextMeshPro가 프로젝트에 설치되어 있어야 합니다

## 🔧 통합 상태
다음 스크립트에 이미 통합되어 있습니다:
- ✅ BattleManager.cs - 라운드/턴 시작 로그
- ✅ Character.cs - 공격 로그
- ✅ SkillManager.cs - 데미지/회복 로그

## 📞 문제 해결
- **로그가 표시되지 않음**: BattleLogManager.Instance가 null인지 확인
- **스크롤이 작동하지 않음**: ScrollRect 컴포넌트 설정 확인
- **색상이 표시되지 않음**: TextMeshPro 사용 여부 확인 (일반 Text는 Rich Text 활성화 필요)
