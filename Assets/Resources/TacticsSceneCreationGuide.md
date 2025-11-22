# 유니티 작전 코딩(Tactics Coding) 씬 제작 가이드

이 문서는 `TacticsCodingUI.html`의 기능을 유니티 UGUI로 구현하기 위한 제작 가이드입니다. 작성된 스크립트를 사용하여 씬을 구성하는 방법을 설명합니다.

## 1. 사전 준비 (스크립트 및 데이터)

이미 작성된 다음 스크립트들이 프로젝트에 있어야 합니다:
- `Assets/Scripts/Tactics/Data/`
  - `TacticsDatabase.cs`
  - `CharacterData.cs`
  - `TacticsPlan.cs`
- `Assets/Scripts/Tactics/UI/`
  - `CharacterCardUI.cs`
  - `FormationSlotUI.cs`
  - `TacticRowUI.cs`
  - `ConditionModalUI.cs`
- `Assets/Scripts/Tactics/TacticsManager.cs`

### 1.1 캐릭터 데이터 생성
1. Project 창에서 우클릭 -> `Create` -> `Arcana` -> `Character Data`를 선택합니다.
2. `Raina`, `Elara`, `Sera`, `Miel`, `Kai` 등의 데이터를 생성합니다.
3. 각 데이터의 Inspector에서 이름, 클래스, 코스트, 스킬 등을 입력합니다.
   - 예: Raina -> Cost: 3, Skills: [Assault Lance (AP), Guard (PP), ...]

---

## 2. UI 프리팹(Prefab) 제작

### 2.1 Character Card Prefab (캐릭터 풀 목록용)
1. **Image** (배경/초상화)를 생성합니다.
2. 자식으로 **Text (TMP)** 3개를 생성하여 이름, 클래스, 코스트를 표시할 위치를 잡습니다.
3. 선택 효과를 위한 **Image** (SelectedHighlight)와 배치됨 표시를 위한 **Image** (DeployedOverlay, 반투명 검정)를 추가하고 비활성화해둡니다.
4. 최상위 오브젝트에 `CharacterCardUI` 컴포넌트를 추가하고 각 UI 요소를 연결합니다.
5. 이를 프리팹으로 저장합니다 (`CharacterCardPrefab`).

### 2.2 Formation Slot Prefab (부대 편성 슬롯용)
1. **Image** (배경)를 생성합니다.
2. **Empty State** 그룹(GameObject)을 만들고, "+" 텍스트와 "Front/Back" 텍스트를 넣습니다.
3. **Filled State** 그룹(GameObject)을 만들고, 캐릭터 초상화(Image), 이름(Text), 코스트(Text)를 넣습니다.
4. 활성 상태 표시를 위한 **Image** (ActiveHighlight, 테두리 등)를 추가합니다.
5. 최상위 오브젝트에 `FormationSlotUI` 컴포넌트를 추가하고 각 요소를 연결합니다.
6. 이를 프리팹으로 저장합니다 (`FormationSlotPrefab`).

### 2.3 Tactic Row Prefab (작전 코딩 한 줄)
1. 가로로 배치된 패널(Image)을 만듭니다.
2. **Text** (순번), **Text** (스킬명)을 배치합니다.
3. **Button** 2개를 배치하여 각각 조건 1, 조건 2를 표시하게 합니다. 버튼 내부 텍스트를 연결합니다.
4. 최상위 오브젝트에 `TacticRowUI` 컴포넌트를 추가하고 연결합니다.
5. 이를 프리팹으로 저장합니다 (`TacticRowPrefab`).

---

## 3. 씬(Scene) 구성

### 3.1 Canvas 및 레이아웃
1. **Canvas**를 생성하고 `Canvas Scaler`를 설정합니다 (예: 1920x1080).
2. 전체를 좌/우로 나눌 **Horizontal Layout Group** (또는 빈 오브젝트로 앵커링)을 만듭니다.

#### 좌측 패널 (Left Panel)
- **Unit Info**: 부대 정보 텍스트 배치.
- **Formation Grid**: 
  - `Grid Layout Group` 컴포넌트 추가 (Cell Size: 150x150 등, Constraint: Fixed Column Count 3).
  - 이 안에 `FormationSlotPrefab`을 6개 미리 생성해 둡니다 (인스펙터에서 순서대로 0~5번 슬롯이 됨).
- **Character Pool**:
  - `Scroll Rect` 생성.
  - Content 자식에 `Grid Layout Group` 추가.
  - `TacticsManager`가 여기에 `CharacterCardPrefab`을 생성하게 됩니다.

#### 우측 패널 (Right Panel)
- **Character Detail**: 선택된 캐릭터 정보를 보여줄 패널. (초상화, 이름, 스탯 등 UI 배치).
- **Coding Panel**:
  - 상단 타이틀.
  - **Coding List**: `Scroll Rect` -> Content에 `Vertical Layout Group` 추가.
  - `TacticsManager`가 여기에 `TacticRowPrefab`을 생성합니다.

### 3.2 모달 (Modal)
- Canvas의 가장 하단(가장 위에 그려짐)에 **Condition Modal** 패널을 만듭니다.
- 반투명 배경(전체 화면)과 중앙 팝업창을 만듭니다.
- 팝업창 내부를 좌우로 나누어 **Category List** (Scroll View)와 **Detail List** (Scroll View)를 배치합니다.
- 카테고리 아이템과 세부 조건 아이템을 위한 간단한 버튼 프리팹을 만들거나, `ConditionModalUI` 스크립트 내에서 동적으로 생성할 버튼 프리팹을 연결합니다.
- 최상위 모달 오브젝트에 `ConditionModalUI`를 추가하고 연결합니다.

---

## 4. 매니저 연결 (TacticsManager)

1. 빈 GameObject (`GameManager`)를 생성하고 `TacticsManager` 컴포넌트를 추가합니다.
2. **Data**:
   - `Available Characters`: 1.1에서 만든 캐릭터 데이터들을 리스트에 할당합니다.
   - `Max Cost`: 15로 설정.
3. **UI Containers**:
   - `Character Pool Container`: 좌측 패널의 Pool Content 연결.
   - `Formation Grid Container`: 좌측 패널의 Grid 오브젝트(슬롯 6개가 자식으로 있는) 연결.
   - `Coding List Container`: 우측 패널의 Coding List Content 연결.
4. **UI Prefabs**:
   - 2번에서 만든 프리팹들을 연결합니다.
5. **UI Components**:
   - `Condition Modal`: 3.2에서 만든 모달 연결.
   - 나머지 텍스트, 버튼, 패널 등 씬에 배치된 UI 요소들을 알맞게 연결합니다.

## 5. 실행 및 테스트

1. Play 버튼을 누릅니다.
2. 캐릭터 풀에 캐릭터들이 로드되는지 확인합니다.
3. 캐릭터를 클릭하면 상세 정보가 뜨는지 확인합니다.
4. 빈 슬롯을 클릭하여 캐릭터가 배치되는지 확인합니다.
5. 배치된 캐릭터의 작전 목록(스킬들)이 우측에 뜨는지 확인합니다.
6. 조건을 클릭하여 모달이 뜨고, 조건을 변경하면 반영되는지 확인합니다.

---
*이 가이드는 `TacticsCodingUI.html`의 기능을 유니티로 이식하기 위한 구조적 설계를 바탕으로 작성되었습니다.*
