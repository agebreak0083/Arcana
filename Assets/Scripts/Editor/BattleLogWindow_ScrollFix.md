# 전투 로그 윈도우 스크롤 수정 완료

## 🔧 수정 내용

### 1. BattleLogWindowCreator.cs 수정
**문제**: Content와 LogText의 레이아웃 설정이 잘못되어 스크롤이 작동하지 않음

**해결**:
- Content의 초기 높이를 500으로 설정
- VerticalLayoutGroup의 `childControlHeight`를 `true`로 변경
- LayoutElement의 `minHeight`를 50으로 설정하여 최소 높이 보장
- RectTransform 중복 설정 제거

```csharp
// 변경 전
contentRect.sizeDelta = new Vector2(0, 0);
contentLayout.childControlHeight = false;
logTextLayout.flexibleHeight = 1;

// 변경 후
contentRect.sizeDelta = new Vector2(0, 500); // 초기 높이 설정
contentLayout.childControlHeight = true;
logTextLayout.minHeight = 50;
```

### 2. BattleLogManager.cs 수정
**문제**: 레이아웃 업데이트가 완료되기 전에 스크롤 위치를 변경하여 자동 스크롤이 작동하지 않음

**해결**:
- `ScrollToBottom()` 메서드를 코루틴으로 변경
- 레이아웃 재계산을 강제로 수행
- 프레임 대기 후 스크롤 위치 변경

```csharp
private IEnumerator ScrollToBottomCoroutine()
{
    // 레이아웃이 완전히 업데이트될 때까지 대기
    yield return new WaitForEndOfFrame();
    
    // Canvas 강제 업데이트
    Canvas.ForceUpdateCanvases();
    
    // Content의 레이아웃 강제 재계산
    if (scrollRect.content != null)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
    }
    
    // 한 프레임 더 대기
    yield return null;
    
    // 스크롤을 맨 아래로
    scrollRect.verticalNormalizedPosition = 0f;
}
```

## ✅ 수정 후 기능

### 1. 자동 스크롤 (Auto Scroll)
- ✅ 새 로그 추가 시 자동으로 맨 아래로 스크롤
- ✅ 레이아웃 재계산 후 스크롤하여 정확한 위치 보장
- ✅ 코루틴을 사용하여 안정적인 스크롤 동작

### 2. 수동 스크롤 (Manual Scroll)
- ✅ 마우스 드래그로 스크롤 가능
- ✅ 스크롤바 드래그로 스크롤 가능
- ✅ 마우스 휠로 스크롤 가능

## 🎯 사용 방법

### Prefab 재생성
기존 BattleLogWindow를 삭제하고 다시 생성하세요:

1. Hierarchy에서 기존 `BattleLogWindow` 삭제
2. `GameObject` → `UI` → `Battle Log Window` 클릭
3. 새로운 BattleLogWindow가 생성됩니다

### 테스트
게임을 실행하면:
- 로그가 추가될 때마다 자동으로 맨 아래로 스크롤됩니다
- 마우스로 위쪽 로그를 확인할 수 있습니다
- 새 로그가 추가되면 다시 맨 아래로 자동 스크롤됩니다

## 📊 기술적 세부사항

### ContentSizeFitter + VerticalLayoutGroup
- ContentSizeFitter가 Content의 높이를 자동으로 조정
- VerticalLayoutGroup이 자식 요소(LogText)의 높이를 제어
- LogText의 preferredHeight가 텍스트 내용에 따라 자동 계산됨

### 스크롤 동작 순서
1. 로그 메시지 추가
2. 텍스트 업데이트
3. LayoutRebuilder로 TextMeshProUGUI 레이아웃 재계산
4. WaitForEndOfFrame으로 프레임 종료 대기
5. Canvas.ForceUpdateCanvases() 호출
6. Content 레이아웃 재계산
7. 한 프레임 더 대기
8. verticalNormalizedPosition을 0으로 설정 (맨 아래)

## ⚠️ 주의사항

1. **기존 Prefab 교체 필요**: 수정사항을 적용하려면 기존 BattleLogWindow를 삭제하고 새로 생성해야 합니다.

2. **TextMeshPro 필수**: TextMeshProUGUI를 사용하므로 TextMeshPro 패키지가 필요합니다.

3. **코루틴 사용**: BattleLogManager가 MonoBehaviour를 상속하므로 코루틴 사용이 가능합니다.

## 🐛 문제 해결

### 여전히 스크롤이 안 된다면:
1. ScrollRect 컴포넌트의 `Vertical` 옵션이 체크되어 있는지 확인
2. Viewport에 Mask 컴포넌트가 있는지 확인
3. Content의 ContentSizeFitter가 `Vertical Fit: Preferred Size`로 설정되어 있는지 확인
4. LogText의 LayoutElement가 있는지 확인

### 스크롤바가 보이지 않는다면:
- ScrollRect의 `Vertical Scrollbar Visibility`를 `Permanent`로 변경

### 로그가 잘린다면:
- LogText의 `Overflow Mode`가 `Overflow`로 설정되어 있는지 확인
- Content의 높이가 충분한지 확인 (ContentSizeFitter가 자동 조정)
