using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// 전투 로그 윈도우 Prefab을 생성하는 에디터 스크립트
/// </summary>
public class BattleLogWindowCreator
{
    [MenuItem("GameObject/UI/Battle Log Window")]
    public static void CreateBattleLogWindow()
    {
        // Canvas 찾기 또는 생성
        Canvas canvas = GameObject.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 메인 패널 생성 (우측 상단)
        GameObject logWindow = new GameObject("BattleLogWindow");
        logWindow.transform.SetParent(canvas.transform, false);

        RectTransform logWindowRect = logWindow.AddComponent<RectTransform>();
        logWindowRect.anchorMin = new Vector2(1, 1); // 우측 상단
        logWindowRect.anchorMax = new Vector2(1, 1);
        logWindowRect.pivot = new Vector2(1, 1);
        logWindowRect.anchoredPosition = new Vector2(-20, -20); // 여백
        logWindowRect.sizeDelta = new Vector2(400, 500); // 크기

        Image logWindowImage = logWindow.AddComponent<Image>();
        logWindowImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f); // 반투명 검정

        // 타이틀 바 생성
        GameObject titleBar = new GameObject("TitleBar");
        titleBar.transform.SetParent(logWindow.transform, false);

        RectTransform titleBarRect = titleBar.AddComponent<RectTransform>();
        titleBarRect.anchorMin = new Vector2(0, 1);
        titleBarRect.anchorMax = new Vector2(1, 1);
        titleBarRect.pivot = new Vector2(0.5f, 1);
        titleBarRect.anchoredPosition = Vector2.zero;
        titleBarRect.sizeDelta = new Vector2(0, 40);

        Image titleBarImage = titleBar.AddComponent<Image>();
        titleBarImage.color = new Color(0.2f, 0.2f, 0.3f, 1f);

        // 타이틀 텍스트
        GameObject titleText = new GameObject("TitleText");
        titleText.transform.SetParent(titleBar.transform, false);

        RectTransform titleTextRect = titleText.AddComponent<RectTransform>();
        titleTextRect.anchorMin = Vector2.zero;
        titleTextRect.anchorMax = Vector2.one;
        titleTextRect.offsetMin = new Vector2(10, 0);
        titleTextRect.offsetMax = new Vector2(-10, 0);

        TextMeshProUGUI titleTMP = titleText.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "전투 로그";
        titleTMP.fontSize = 20;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = Color.white;

        // 스크롤 뷰 생성
        GameObject scrollView = new GameObject("ScrollView");
        scrollView.transform.SetParent(logWindow.transform, false);

        RectTransform scrollViewRect = scrollView.AddComponent<RectTransform>();
        scrollViewRect.anchorMin = Vector2.zero;
        scrollViewRect.anchorMax = Vector2.one;
        scrollViewRect.offsetMin = new Vector2(10, 10); // 하단 여백
        scrollViewRect.offsetMax = new Vector2(-10, -50); // 상단 여백 (타이틀 바 아래)

        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        Image scrollViewImage = scrollView.AddComponent<Image>();
        scrollViewImage.color = new Color(0.05f, 0.05f, 0.05f, 0.8f);

        // Viewport 생성
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);

        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        Mask viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = Color.white;

        scrollRect.viewport = viewportRect;

        // Content 생성
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);

        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 500); // 초기 높이 설정

        ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlHeight = true;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.spacing = 0;
        contentLayout.padding = new RectOffset(10, 10, 10, 10);

        scrollRect.content = contentRect;

        // 로그 텍스트 생성
        GameObject logTextObj = new GameObject("LogText");
        logTextObj.transform.SetParent(content.transform, false);

        TextMeshProUGUI logText = logTextObj.AddComponent<TextMeshProUGUI>();
        logText.fontSize = 14;
        logText.color = Color.white;
        logText.alignment = TextAlignmentOptions.TopLeft;
        logText.enableWordWrapping = true;
        logText.overflowMode = TextOverflowModes.Overflow;
        logText.text = "전투 로그가 여기에 표시됩니다...";

        // LayoutElement 추가하여 높이 자동 조정
        LayoutElement logTextLayout = logTextObj.AddComponent<LayoutElement>();
        logTextLayout.preferredHeight = -1;
        logTextLayout.minHeight = 50;

        // Scrollbar 생성 (세로)
        GameObject scrollbar = new GameObject("Scrollbar Vertical");
        scrollbar.transform.SetParent(scrollView.transform, false);

        RectTransform scrollbarRect = scrollbar.AddComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.pivot = new Vector2(1, 0.5f);
        scrollbarRect.anchoredPosition = Vector2.zero;
        scrollbarRect.sizeDelta = new Vector2(20, 0);

        Image scrollbarImage = scrollbar.AddComponent<Image>();
        scrollbarImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        Scrollbar scrollbarComponent = scrollbar.AddComponent<Scrollbar>();
        scrollbarComponent.direction = Scrollbar.Direction.BottomToTop;

        // Scrollbar Handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(scrollbar.transform, false);

        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = new Vector2(5, 5);
        handleRect.offsetMax = new Vector2(-5, -5);

        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);

        scrollbarComponent.targetGraphic = handleImage;
        scrollbarComponent.handleRect = handleRect;

        scrollRect.verticalScrollbar = scrollbarComponent;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        // BattleLogManager 컴포넌트 추가
        BattleLogManager logManager = logWindow.AddComponent<BattleLogManager>();
        logManager.scrollRect = scrollRect;
        logManager.logText = logText;

        // 선택 및 저장
        Selection.activeGameObject = logWindow;

        // Prefab으로 저장
        string prefabPath = "Assets/Prefabs/UI/BattleLogWindow.prefab";
        string directory = System.IO.Path.GetDirectoryName(prefabPath);

        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        PrefabUtility.SaveAsPrefabAsset(logWindow, prefabPath);

        Debug.Log($"전투 로그 윈도우 Prefab이 생성되었습니다: {prefabPath}");
    }
}
#endif
