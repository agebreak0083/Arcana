# 유니티 작전 코딩(Tactics Coding) 씬 제작 가이드

이 문서는 `TacticsCodingUI.html`의 디자인과 기능을 유니티 UGUI로 완벽하게 구현하기 위한 상세 가이드입니다.
제공된 스크립트(`TacticsManager.cs`, `CharacterCardUI.cs` 등)와 연결하여 작동하는 씬을 구성합니다.

---

## 0. 공통 디자인 리소스 준비 (Colors & Fonts)

유니티에서 UI를 구성하기 전에 미리 팔레트를 알아두면 편리합니다.

| 용도 | Hex Code | 비고 |
| :--- | :--- | :--- |
| **배경 (Background)** | `#111827` | Very Dark Blue |
| **패널 (Panel)** | `#1F2937` | Dark Gray Blue |
| **강조 (Teal)** | `#2DD4BF` | Teal-400 (타이틀, 코스트) |
| **강조 (Yellow)** | `#FCD34D` | Yellow-300 (중요 정보, 선택) |
| **텍스트 (Basic)** | `#FFFFFF` | White |
| **텍스트 (Sub)** | `#9CA3AF` | Gray-400 |
| **슬롯 (Empty)** | `#374151` | Gray-700 |

*   **Font**: `Inter` 또는 `Noto Sans KR` (TMP 폰트 에셋 생성 권장)

---

## 1. UI 프리팹(Prefab) 제작

`Assets/Prefabs/UI` 폴더를 만들고 다음 프리팹들을 생성하세요.

### 1.1 CharacterCardPrefab (캐릭터 카드)
*   **Root Object**: `Image` (이름: `CharacterCard`)
    *   **Size**: 140 x 140
    *   **Color**: `#1F2937`
    *   **Component**: `CharacterCardUI.cs` 추가
*   **자식 구조**:
    1.  **Portrait** (`Image`):
        *   Anchor: Stretch/Stretch (Left/Top/Right/Bottom: 2)
        *   Color: White (스프라이트 할당용)
    2.  **Overlay** (`Image`):
        *   Anchor: Stretch/Bottom (Height: 40)
        *   Color: `#000000` (Alpha 200)
    3.  **NameText** (`TextMeshPro - Text (UI)`):
        *   Parent: Overlay
        *   Anchor: Stretch/Top (Top: 5, Height: 20)
        *   Font Size: 14 (Bold), Color: White, Align: Center
    4.  **ClassText** (`TextMeshPro - Text (UI)`):
        *   Parent: Overlay
        *   Anchor: Stretch/Bottom (Bottom: 5, Height: 15)
        *   Font Size: 11, Color: `#2DD4BF`, Align: Center
    5.  **CostBadge** (`Image`):
        *   Anchor: Top/Left (Pos: 0, 0), Size: 30x30
        *   Color: `#10B981` (Cost 1), `#3B82F6` (Cost 2) 등 (스크립트 제어 가능)
        *   **CostText** (TMP): Center, Font Size 16 (Bold), Color: White
    6.  **SelectedHighlight** (`Image`):
        *   Anchor: Stretch/Stretch
        *   Color: Transparent, **Outline** 컴포넌트 추가 (Color: `#FCD34D`, Width: 4)
        *   기본 상태: `SetActive(false)`
    7.  **DeployedOverlay** (`Image`):
        *   Anchor: Stretch/Stretch
        *   Color: `#000000` (Alpha 150)
        *   기본 상태: `SetActive(false)`

**[Inspector 연결]**: `CharacterCardUI` 컴포넌트에 위에서 만든 Portrait, NameText, ClassText, CostText(Badge안의 텍스트), SelectedHighlight, DeployedOverlay를 연결합니다.

### 1.2 FormationSlotPrefab (부대 배치 슬롯)
*   **Root Object**: `Image` (이름: `FormationSlot`)
    *   **Size**: 160 x 160
    *   **Color**: `#374151` (Empty Color)
    *   **Component**: `FormationSlotUI.cs` 추가
*   **자식 구조**:
    1.  **EmptyState** (`GameObject` - Empty):
        *   **PlusText** (TMP): Center, Text "+", Size 40, Color `#9CA3AF`
        *   **PosText** (TMP): Bottom Center, Text "Front 1", Size 14 -> **`FormationSlotUI`의 `slotLabel` 연결**
    2.  **FilledState** (`GameObject` - Empty, 기본 `SetActive(false)`):
        *   **Portrait** (`Image`): Stretch/Stretch -> **`FormationSlotUI`의 `characterPortrait` 연결**
        *   **CostText** (TMP): Top/Left, Size 14, Bold, Color White (배경 이미지 필요 시 추가) -> **`FormationSlotUI`의 `charCostText` 연결**
        *   **InfoOverlay** (`Image`): Bottom Stretch, Height 30, Color Black(Alpha 200)
        *   **NameText** (TMP): Parent InfoOverlay, Center, Size 14 -> **`FormationSlotUI`의 `charNameText` 연결**
    3.  **ActiveHighlight** (`Image`):
        *   Anchor: Stretch/Stretch
        *   Color: Transparent, **Outline** (Color: `#2DD4BF`, Width: 4)
        *   기본 `SetActive(false)` -> **`FormationSlotUI`의 `activeHighlight` 연결**

**[Inspector 연결]**: `FormationSlotUI` 컴포넌트에 다음을 연결합니다.
*   **Empty State Object**: 1번 EmptyState
*   **Filled State Object**: 2번 FilledState
*   **Slot Label**: 1번의 PosText
*   **Character Portrait**: 2번의 Portrait
*   **Char Name Text**: 2번의 NameText
*   **Char Cost Text**: 2번의 CostText
*   **Active Highlight**: 3번 ActiveHighlight

### 1.3 TacticRowPrefab (작전 코딩 한 줄)
*   **Root Object**: `Image` (이름: `TacticRow`)
    *   **Size**: Width 1000 (Stretch), Height 60
    *   **Color**: `#1F2937`
    *   **Component**: `TacticRowUI.cs` 추가
*   **자식 구조** (Horizontal Layout Group 권장):
    1.  **IndexText** (TMP): Width 50, Center, Color `#FCD34D`
    2.  **SkillNameText** (TMP): Width 200, Left Align, Color `#2DD4BF`
    3.  **Condition1Btn** (`Button`):
        *   Image Color: `#111827` (Border `#4B5563`)
        *   **Text** (TMP): "조건 없음", Size 14, Color White
    4.  **AndText** (TMP): Width 50, Center, Text "AND", Color `#FCD34D`, Size 10
    5.  **Condition2Btn** (`Button`):
        *   (Condition1Btn과 동일 스타일)

**[Inspector 연결]**: `TacticRowUI`에 IndexText, SkillNameText, Condition1Btn(및 텍스트), Condition2Btn(및 텍스트) 연결.

---

## 2. 씬(Scene) 구성 단계

### 2.1 Canvas 설정
1.  **Canvas** 생성 -> `Canvas Scaler` 설정
    *   UI Scale Mode: `Scale With Screen Size`
    *   Reference Resolution: `1920 x 1080`
2.  **Background** (`Image`): Canvas 자식, Stretch/Stretch, Color `#111827`.

### 2.2 레이아웃 잡기 (Horizontal Split)
1.  **MainLayout** (Empty): Canvas 자식, Stretch/Stretch (Padding: Left/Right 40, Top/Bottom 40).
    *   `Horizontal Layout Group` (Spacing: 40, Child Force Expand: Width 체크).
2.  **LeftPanel** (Empty): `Layout Element` (Preferred Width: 600).
3.  **RightPanel** (Empty): `Layout Element` (Preferred Width: 1200).

### 2.3 Left Panel 구성 (부대 정보 & 풀)
`LeftPanel` 아래에 다음을 순서대로 배치 (`Vertical Layout Group` 사용, Spacing 20):

1.  **UnitInfoPanel** (`Image`):
    *   Color: `#1F2937`, `Rounded Corners` (선택 사항)
    *   **Title**: "부대 정보" (Color `#FCD34D`)
    *   **CostText**: "0 / 15" (Color `#2DD4BF`, Size 24) -> **`TacticsManager`의 `currentCostText`에 연결**
2.  **FormationGridPanel** (`Image`):
    *   Color: `#1F2937`
    *   **GridContainer** (Empty):
        *   `Grid Layout Group`: Cell Size 160x160, Spacing 10, Constraint: Fixed Column Count 3.
        *   **중요**: 이 아래에 `FormationSlotPrefab` 6개를 미리 생성해 둡니다. (이름: Slot_0 ~ Slot_5)
        *   **`TacticsManager`의 `formationGridContainer`에 이 GridContainer 연결**
3.  **CharacterPoolPanel** (`Image`):
    *   Color: `#1F2937`
    *   **Scroll View** 생성 (이름: `PoolScrollView`):
        *   **Content** 오브젝트에 `Grid Layout Group` (Cell Size 140x140, Spacing 10).
        *   **`TacticsManager`의 `characterPoolContainer`에 이 Content 연결**

### 2.4 Right Panel 구성 (상세 & 코딩)
`RightPanel` 아래에 다음을 순서대로 배치 (`Vertical Layout Group` 사용, Spacing 20):

1.  **DetailPanel** (`GameObject` - `TacticsManager`의 `characterDetailPanel` 연결):
    *   활성화/비활성화 제어됨.
    *   **Portrait** (`Image`), **Name**, **Class**, **Arcana**, **Speed**, **Desc** (`TMP`) 배치.
    *   **RemoveButton** (`Button`): "부대에서 제거" -> **`TacticsManager`의 `removeFromUnitBtn` 연결**.
2.  **CodingPanel** (`Image`):
    *   Color: `#1F2937`, Flexible Height 1 (남은 공간 채움).
    *   **Title** (TMP): "캐릭터 선택 대기" -> **`TacticsManager`의 `codingPanelTitle` 연결**.
    *   **CodingScrollView** (Scroll View):
        *   **Content** 오브젝트에 `Vertical Layout Group` (Spacing 5, Control Child Size Width 체크).
        *   **`TacticsManager`의 `codingListContainer`에 이 Content 연결**

### 2.5 Condition Modal (팝업)
Canvas의 최하단(가장 앞)에 배치.
1.  **ModalRoot** (`Image`): Stretch/Stretch, Color Black (Alpha 200).
    *   **`TacticsManager`의 `conditionModal` (ConditionModalUI) 컴포넌트 추가**.
2.  **PopupBody** (`Image`): Center, Size 1000x800, Color `#111827`, Border `#2DD4BF` (Outline).
3.  내부 구성:
    *   **CategoryScroll** (Left, Width 300): `Vertical Layout Group`.
    *   **DetailScroll** (Right, Width 700): `Vertical Layout Group`.
    *   **CloseButton**: 우상단 "X" 버튼.
4.  **Inspector 연결**: `ConditionModalUI`에 CategoryContainer, DetailContainer, CloseBtn 등 연결.

---

## 3. 매니저 설정 (TacticsManager)

1.  빈 GameObject 생성 (`GameManager`).
2.  `TacticsManager.cs` 컴포넌트 추가.
3.  **Inspector 세팅**:
    *   **Available Characters**: `Assets/Resources/Data/Characters` 등에 생성한 `CharacterData` 에셋들을 드래그앤드롭.
    *   **Max Cost**: 15
    *   **UI Containers**: 위에서 만든 `Pool Content`, `Formation Grid`, `Coding List Content` 연결.
    *   **UI Prefabs**: 1번에서 만든 프리팹 2개 연결 (`CharacterCardPrefab`, `TacticRowPrefab`).
    *   **UI Components**:
        *   `Condition Modal`: 2.5의 ModalRoot 오브젝트.
        *   `Current Cost Text`: 2.3의 CostText.
        *   `Coding Panel Title`: 2.4의 Title.
        *   `Character Detail Panel`: 2.4의 DetailPanel.
        *   나머지 Detail 관련 텍스트 및 버튼 연결.

---

## 4. 실행 체크리스트

1.  [ ] **캐릭터 풀 로드**: 실행 시 좌측 하단에 캐릭터 카드가 생성되는가?
2.  [ ] **배치**: 카드를 클릭(선택) 후 빈 슬롯을 클릭하면 배치가 되는가?
3.  [ ] **코스트 갱신**: 배치 시 상단 코스트(0/15)가 증가하는가?
4.  [ ] **상세 정보**: 캐릭터 선택 시 우측 상단에 정보가 뜨는가?
5.  [ ] **작전 목록**: 배치된 캐릭터 선택 시 우측 하단에 스킬 목록(코딩)이 뜨는가?
6.  [ ] **모달 작동**: 조건 버튼 클릭 시 팝업이 뜨고, 조건 선택 시 버튼 텍스트가 바뀌는가?

이 가이드를 따라 UI를 구성하면 HTML 프로토타입과 동일한 룩앤필의 유니티 씬이 완성됩니다.
