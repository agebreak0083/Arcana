using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Arcana.Tactics.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TacticsSceneBuilder : MonoBehaviour
{
    [ContextMenu("Create Tactics UI Screen Prefab")]
    public void CreateTacticsUIScreen()
    {
#if UNITY_EDITOR
        // Load Font
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/PretendardVariable SDF.asset");
        if (fontAsset == null) Debug.LogWarning("Font not found at Assets/TextMesh Pro/Fonts/PretendardVariable SDF.asset");

        // 1. Canvas & Root
        GameObject rootObj = new GameObject("TacticsUIScreen");
        RectTransform rootRect = rootObj.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(1920, 1080);

        Canvas canvas = rootObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = rootObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        rootObj.AddComponent<GraphicRaycaster>();

        // 2. Background
        GameObject bgObj = CreateImageObject("Background", rootObj.transform, new Color32(17, 24, 39, 255)); // #111827
        Stretch(bgObj.GetComponent<RectTransform>());

        // 3. Main Vertical Layout (Root Layout)
        GameObject mainLayout = CreateObject("MainLayout", rootObj.transform);
        Stretch(mainLayout.GetComponent<RectTransform>());

        VerticalLayoutGroup mainVlg = mainLayout.AddComponent<VerticalLayoutGroup>();
        mainVlg.padding = new RectOffset(40, 40, 20, 20);
        mainVlg.spacing = 20;
        mainVlg.childForceExpandWidth = true;
        mainVlg.childForceExpandHeight = false;
        mainVlg.childControlWidth = true;
        mainVlg.childControlHeight = true;

        // =================================================================================================
        // 3.1 Header Area
        // =================================================================================================
        GameObject headerObj = CreateObject("Header", mainLayout.transform);
        LayoutElement headerLe = headerObj.AddComponent<LayoutElement>();
        headerLe.minHeight = 60;
        headerLe.preferredHeight = 60;

        VerticalLayoutGroup headerVlg = headerObj.AddComponent<VerticalLayoutGroup>();
        headerVlg.spacing = 10;
        headerVlg.childControlHeight = true;
        headerVlg.childControlWidth = true;

        // Header Title
        CreateText("HeaderTitle", headerObj.transform, "ARCANA CODE : 작전 코딩 설정", 36, new Color32(45, 212, 191, 255), FontStyles.Bold, fontAsset); // Cyan

        // Underline
        GameObject underline = CreateImageObject("Underline", headerObj.transform, new Color32(45, 212, 191, 255));
        LayoutElement lineLe = underline.AddComponent<LayoutElement>();
        lineLe.minHeight = 2;
        lineLe.preferredHeight = 2;

        // =================================================================================================
        // 3.2 Body Area (Left + Right Panels)
        // =================================================================================================
        GameObject bodyObj = CreateObject("Body", mainLayout.transform);
        LayoutElement bodyLe = bodyObj.AddComponent<LayoutElement>();
        bodyLe.flexibleHeight = 1; // Take remaining height
        bodyLe.minHeight = 400; // Ensure minimum height

        HorizontalLayoutGroup bodyHlg = bodyObj.AddComponent<HorizontalLayoutGroup>();
        bodyHlg.spacing = 40;
        bodyHlg.childForceExpandWidth = false; // Let panels define their width
        bodyHlg.childForceExpandHeight = true;
        bodyHlg.childControlWidth = true;
        bodyHlg.childControlHeight = true;

        // -------------------------------------------------------------------------------------------------
        // Left Panel (Unit Info + Formation)
        // -------------------------------------------------------------------------------------------------
        GameObject leftPanel = CreateObject("LeftPanel", bodyObj.transform);
        LayoutElement leftLe = leftPanel.AddComponent<LayoutElement>();
        leftLe.preferredWidth = 500; // Fixed width for left panel

        VerticalLayoutGroup leftVlg = leftPanel.AddComponent<VerticalLayoutGroup>();
        leftVlg.spacing = 20;
        leftVlg.childForceExpandHeight = false;
        leftVlg.childControlHeight = true;
        leftVlg.childForceExpandWidth = true;
        leftVlg.childControlWidth = true;

        // Unit Info Panel
        GameObject unitInfoPanel = CreateImageObject("UnitInfoPanel", leftPanel.transform, new Color32(31, 41, 55, 255)); // #1F2937
        VerticalLayoutGroup unitInfoVlg = unitInfoPanel.AddComponent<VerticalLayoutGroup>();
        unitInfoVlg.padding = new RectOffset(20, 20, 20, 20);
        unitInfoVlg.spacing = 15;
        unitInfoVlg.childControlHeight = true;
        unitInfoVlg.childForceExpandHeight = false;
        unitInfoVlg.childControlWidth = true;
        unitInfoVlg.childForceExpandWidth = true;

        CreateText("Title", unitInfoPanel.transform, "부대 정보", 30, new Color32(252, 211, 77, 255), FontStyles.Bold, fontAsset);
        CreateInfoRow(unitInfoPanel.transform, "부대명:", "코드 브레이커 A", new Color32(156, 163, 175, 255), Color.white, fontAsset);
        GameObject costRow = CreateInfoRow(unitInfoPanel.transform, "부대 코스트 (현재/최대):", "9 / 15", new Color32(156, 163, 175, 255), new Color32(45, 212, 191, 255), fontAsset);
        Transform costValue = costRow.transform.Find("Value");
        if (costValue != null) costValue.name = "TotalCostText";
        CreateInfoRow(unitInfoPanel.transform, "랭킹 모드:", "코스트 제한 (Lv.10)", new Color32(156, 163, 175, 255), new Color32(248, 113, 113, 255), fontAsset);

        // Formation Grid Panel
        GameObject formationPanel = CreateImageObject("FormationGridPanel", leftPanel.transform, new Color32(31, 41, 55, 255));
        VerticalLayoutGroup formVlg = formationPanel.AddComponent<VerticalLayoutGroup>();
        formVlg.padding = new RectOffset(20, 20, 20, 20);
        formVlg.spacing = 10;
        formVlg.childControlHeight = true;
        formVlg.childForceExpandHeight = false;
        formVlg.childControlWidth = true;
        formVlg.childForceExpandWidth = true;

        CreateText("Title", formationPanel.transform, "부대 편성 (Front 3 / Back 3)", 30, new Color32(252, 211, 77, 255), FontStyles.Bold, fontAsset);
        CreateText("FrontLabel", formationPanel.transform, "전열 (Front)", 20, new Color32(156, 163, 175, 255), FontStyles.Bold, fontAsset);

        // Front Row
        GameObject frontRow = CreateObject("FrontRow", formationPanel.transform);
        HorizontalLayoutGroup frontHlg = frontRow.AddComponent<HorizontalLayoutGroup>();
        frontHlg.spacing = 10;
        frontHlg.childControlWidth = false;
        frontHlg.childControlHeight = false;
        frontHlg.childAlignment = TextAnchor.MiddleLeft;
        LayoutElement frontLe = frontRow.AddComponent<LayoutElement>();
        frontLe.minHeight = 150;
        frontLe.preferredHeight = 150;

        // Back Row Label
        CreateText("BackLabel", formationPanel.transform, "후열 (Back)", 20, new Color32(156, 163, 175, 255), FontStyles.Bold, fontAsset);

        // Back Row
        GameObject backRow = CreateObject("BackRow", formationPanel.transform);
        HorizontalLayoutGroup backHlg = backRow.AddComponent<HorizontalLayoutGroup>();
        backHlg.spacing = 10;
        backHlg.childControlWidth = false;
        backHlg.childControlHeight = false;
        backHlg.childAlignment = TextAnchor.MiddleLeft;
        LayoutElement backLe = backRow.AddComponent<LayoutElement>();
        backLe.minHeight = 150;
        backLe.preferredHeight = 150;

        // Instantiate Slots
        GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/FormationSlotPrefab.prefab");
        if (slotPrefab != null)
        {
            for (int i = 0; i < 3; i++) { GameObject slot = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, frontRow.transform); slot.name = $"Slot_{i}"; }
            for (int i = 3; i < 6; i++) { GameObject slot = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, backRow.transform); slot.name = $"Slot_{i}"; }
        }

        // Reset Button
        GameObject resetBtn = CreateButton("ResetButton", formationPanel.transform, "부대 초기화", new Color32(127, 29, 29, 255), fontAsset);
        LayoutElement resetLe = resetBtn.AddComponent<LayoutElement>();
        resetLe.minHeight = 50;
        resetLe.preferredHeight = 50;

        // -------------------------------------------------------------------------------------------------
        // Right Panel (Detail + Coding)
        // -------------------------------------------------------------------------------------------------
        GameObject rightPanel = CreateObject("RightPanel", bodyObj.transform);
        LayoutElement rightLe = rightPanel.AddComponent<LayoutElement>();
        rightLe.flexibleWidth = 1; // Take remaining width

        VerticalLayoutGroup rightVlg = rightPanel.AddComponent<VerticalLayoutGroup>();
        rightVlg.spacing = 20;
        rightVlg.childForceExpandHeight = false;
        rightVlg.childControlHeight = true;
        rightVlg.childForceExpandWidth = true;
        rightVlg.childControlWidth = true;

        // Detail Panel (Load from Prefab)
        GameObject detailPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/DetailPanelPrefab.prefab");
        GameObject detailPanel;

        if (detailPrefab != null)
        {
            detailPanel = (GameObject)PrefabUtility.InstantiatePrefab(detailPrefab, rightPanel.transform);
            LayoutElement detailLe = detailPanel.GetComponent<LayoutElement>();
            if (detailLe == null) detailLe = detailPanel.AddComponent<LayoutElement>();
            detailLe.minHeight = 280;
            detailLe.preferredHeight = 280;
            detailLe.flexibleHeight = 0;
        }
        else
        {
            Debug.LogWarning("DetailPanelPrefab not found. Please create it using DetailPanelBuilder.");
            // Fallback: Create a simple placeholder
            detailPanel = CreateImageObject("DetailPanel", rightPanel.transform, new Color32(31, 41, 55, 255));
            LayoutElement detailLe = detailPanel.AddComponent<LayoutElement>();
            detailLe.minHeight = 280;
            detailLe.preferredHeight = 280;
            detailLe.flexibleHeight = 0;
        }


        // Coding Panel
        GameObject codingPanel = CreateImageObject("CodingPanel", rightPanel.transform, new Color32(17, 24, 39, 255));
        LayoutElement codingLe = codingPanel.AddComponent<LayoutElement>();
        codingLe.flexibleHeight = 1;

        VerticalLayoutGroup codingVlg = codingPanel.AddComponent<VerticalLayoutGroup>();
        codingVlg.padding = new RectOffset(20, 20, 20, 20);
        codingVlg.spacing = 15;
        codingVlg.childControlHeight = false;
        codingVlg.childForceExpandHeight = true;
        codingVlg.childControlWidth = true;
        codingVlg.childForceExpandWidth = true;

        // Coding Header
        GameObject codingHeader = CreateObject("CodingHeader", codingPanel.transform);
        HorizontalLayoutGroup headerHlg = codingHeader.AddComponent<HorizontalLayoutGroup>();
        headerHlg.childControlWidth = true; headerHlg.childForceExpandWidth = false;
        headerHlg.childControlHeight = false; headerHlg.childForceExpandHeight = false;
        headerHlg.spacing = 20;

        GameObject titleObj = CreateText("Title", codingHeader.transform, "Raina - 작전 코딩", 30, new Color32(45, 212, 191, 255), FontStyles.Bold, fontAsset);
        titleObj.AddComponent<LayoutElement>().flexibleWidth = 1;

        GameObject helpBox = CreateImageObject("HelpBox", codingHeader.transform, new Color32(31, 41, 55, 255));
        helpBox.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 50);
        helpBox.AddComponent<Outline>().effectColor = new Color32(75, 85, 99, 255);
        HorizontalLayoutGroup helpHlg = helpBox.AddComponent<HorizontalLayoutGroup>();
        helpHlg.padding = new RectOffset(15, 15, 5, 5); // Reduced padding
        helpHlg.childControlWidth = true; helpHlg.childControlHeight = true;

        // Compact Help Text
        GameObject helpTextObj = CreateText("HelpText", helpBox.transform, "AND(&&) 관계: 조건 1과 조건 2를 모두 만족할 때 실행", 20, new Color32(252, 211, 77, 255), FontStyles.Normal, fontAsset);
        helpTextObj.GetComponent<RectTransform>().sizeDelta = new Vector2(350, 0); // Height controlled by layout

        // Table Header
        GameObject tableHeader = CreateImageObject("TableHeader", codingPanel.transform, new Color32(31, 41, 55, 255));
        tableHeader.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 30);
        LayoutElement tableHeaderLe = tableHeader.AddComponent<LayoutElement>();
        tableHeaderLe.minHeight = 30; tableHeaderLe.preferredHeight = 30; // Reduced height
        HorizontalLayoutGroup tableHlg = tableHeader.AddComponent<HorizontalLayoutGroup>();
        tableHlg.padding = new RectOffset(10, 10, 0, 0);
        tableHlg.spacing = 10;
        tableHlg.childAlignment = TextAnchor.MiddleLeft;
        tableHlg.childControlWidth = false; tableHlg.childForceExpandWidth = false;

        CreateText("No", tableHeader.transform, "No.", 14, new Color32(156, 163, 175, 255), FontStyles.Bold, fontAsset).GetComponent<RectTransform>().sizeDelta = new Vector2(50, 30);
        CreateText("Skill", tableHeader.transform, "사용 스킬", 14, new Color32(156, 163, 175, 255), FontStyles.Bold, fontAsset).GetComponent<RectTransform>().sizeDelta = new Vector2(200, 30);
        CreateText("Cond1", tableHeader.transform, "조건 1", 14, new Color32(156, 163, 175, 255), FontStyles.Bold, fontAsset).GetComponent<RectTransform>().sizeDelta = new Vector2(250, 30);
        CreateText("Cond2", tableHeader.transform, "조건 2 (AND)", 14, new Color32(156, 163, 175, 255), FontStyles.Bold, fontAsset).GetComponent<RectTransform>().sizeDelta = new Vector2(300, 30);

        // Coding Scroll View
        GameObject codingScroll = CreateScrollView("CodingScrollView", codingPanel.transform, false);
        codingScroll.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 240);
        codingScroll.AddComponent<LayoutElement>().flexibleHeight = 1;
        ScrollRect codingSr = codingScroll.GetComponent<ScrollRect>();
        VerticalLayoutGroup codingContentVlg = codingSr.content.gameObject.AddComponent<VerticalLayoutGroup>();
        codingContentVlg.spacing = 5;
        codingContentVlg.childControlWidth = false; codingContentVlg.childForceExpandWidth = false;
        codingContentVlg.childControlHeight = true; codingContentVlg.childForceExpandHeight = false;
        codingContentVlg.childAlignment = TextAnchor.UpperLeft;

        GameObject rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/TacticRowPrefab.prefab");
        if (rowPrefab != null) { for (int i = 1; i <= 6; i++) { GameObject row = (GameObject)PrefabUtility.InstantiatePrefab(rowPrefab, codingSr.content); row.name = $"Row_{i}"; row.GetComponent<TacticRowUI>().indexText.text = i.ToString(); } }

        // =================================================================================================
        // 3.3 Character Pool Area (Bottom)
        // =================================================================================================
        GameObject poolPanel = CreateImageObject("CharacterPoolPanel", mainLayout.transform, new Color32(31, 41, 55, 255));
        LayoutElement poolLe = poolPanel.AddComponent<LayoutElement>();
        poolLe.minHeight = 220; // Increased height for better view
        poolLe.preferredHeight = 220;

        VerticalLayoutGroup poolVlg = poolPanel.AddComponent<VerticalLayoutGroup>();
        poolVlg.padding = new RectOffset(20, 20, 20, 20);
        poolVlg.spacing = 10;

        // Removed Title Text as requested

        GameObject poolScroll = CreateScrollView("PoolScrollView", poolPanel.transform, true);
        poolScroll.AddComponent<LayoutElement>().flexibleHeight = 1;

        ScrollRect poolSr = poolScroll.GetComponent<ScrollRect>();
        GridLayoutGroup poolGrid = poolSr.content.gameObject.AddComponent<GridLayoutGroup>();
        poolGrid.cellSize = new Vector2(150, 150);
        poolGrid.spacing = new Vector2(10, 10);
        poolGrid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        poolGrid.constraintCount = 1;
        poolGrid.childAlignment = TextAnchor.MiddleLeft;

        // 4. Condition Modal
        GameObject modalRoot = CreateImageObject("ConditionModal", rootObj.transform, new Color32(0, 0, 0, 200));
        Stretch(modalRoot.GetComponent<RectTransform>());
        ConditionModalUI modalUI = modalRoot.AddComponent<ConditionModalUI>();

        GameObject popupBody = CreateImageObject("PopupBody", modalRoot.transform, new Color32(17, 24, 39, 255));
        popupBody.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 800);
        popupBody.AddComponent<Outline>().effectColor = new Color32(45, 212, 191, 255);
        popupBody.AddComponent<HorizontalLayoutGroup>();

        GameObject catScroll = CreateScrollView("CategoryScroll", popupBody.transform, false);
        catScroll.AddComponent<LayoutElement>().preferredWidth = 300;
        ScrollRect catSr = catScroll.GetComponent<ScrollRect>();
        VerticalLayoutGroup catVlg = catSr.content.gameObject.AddComponent<VerticalLayoutGroup>();
        catVlg.spacing = 2; catVlg.childControlWidth = true; catVlg.childForceExpandWidth = true;

        GameObject detScroll = CreateScrollView("DetailScroll", popupBody.transform, false);
        detScroll.AddComponent<LayoutElement>().flexibleWidth = 1;
        ScrollRect detSr = detScroll.GetComponent<ScrollRect>();
        VerticalLayoutGroup detVlg = detSr.content.gameObject.AddComponent<VerticalLayoutGroup>();
        detVlg.spacing = 2; detVlg.childControlWidth = true; detVlg.childForceExpandWidth = true;

        GameObject closeBtn = CreateButton("CloseButton", modalRoot.transform, "X", Color.clear, fontAsset);
        RectTransform closeRect = closeBtn.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 1); closeRect.anchorMax = new Vector2(1, 1);
        closeRect.anchoredPosition = new Vector2(-50, -50); closeRect.sizeDelta = new Vector2(50, 50);
        closeBtn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 30;

        modalUI.categoryContainer = catSr.content;
        modalUI.detailContainer = detSr.content;
        modalUI.closeBtn = closeBtn.GetComponent<Button>();
        modalRoot.SetActive(false);

        // Save as Prefab
        string folderPath = "Assets/Prefabs/UI";
        if (!System.IO.Directory.Exists(folderPath)) System.IO.Directory.CreateDirectory(folderPath);
        string prefabPath = $"{folderPath}/TacticsUIScreen.prefab";
        PrefabUtility.SaveAsPrefabAsset(rootObj, prefabPath);
        Debug.Log($"TacticsUIScreen Prefab created at {prefabPath}");

        DestroyImmediate(rootObj);
#endif
    }

    private GameObject CreateObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.AddComponent<RectTransform>();
        return go;
    }

    private GameObject CreateImageObject(string name, Transform parent, Color color)
    {
        GameObject go = CreateObject(name, parent);
        Image img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    private GameObject CreateText(string name, Transform parent, string content, int fontSize, Color color, FontStyles style = FontStyles.Normal, TMP_FontAsset font = null)
    {
        GameObject go = CreateObject(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Left;
        if (font != null) tmp.font = font;

        return go;
    }

    private GameObject CreateButton(string name, Transform parent, string text, Color color, TMP_FontAsset font = null)
    {
        GameObject go = CreateImageObject(name, parent, color);
        Button btn = go.AddComponent<Button>();

        GameObject textObj = CreateText("Text", go.transform, text, 14, Color.white, FontStyles.Normal, font);
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        Stretch(textObj.GetComponent<RectTransform>());

        return go;
    }

    private GameObject CreateScrollView(string name, Transform parent, bool isHorizontal)
    {
        GameObject scrollObj = CreateImageObject(name, parent, new Color32(0, 0, 0, 50)); // Slight dark bg
        scrollObj.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 150);
        ScrollRect sr = scrollObj.AddComponent<ScrollRect>();

        GameObject viewport = CreateObject("Viewport", scrollObj.transform);
        Stretch(viewport.GetComponent<RectTransform>());

        // Use RectMask2D instead of Mask for better text compatibility and performance
        viewport.AddComponent<RectMask2D>();

        GameObject content = CreateObject("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 300); // Default height

        // Add ContentSizeFitter to enable scrolling
        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();

        if (isHorizontal)
        {
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            sr.horizontal = true;
            sr.vertical = false;

            // For horizontal, anchor to left
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(0, 1);
            contentRect.pivot = new Vector2(0, 0.5f);
        }
        else
        {
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.horizontal = false;
            sr.vertical = true;

            // For vertical, anchor to top
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
        }

        sr.viewport = viewport.GetComponent<RectTransform>();
        sr.content = contentRect;
        sr.scrollSensitivity = 20;

        return scrollObj;
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private GameObject CreateInfoRow(Transform parent, string label, string value, Color labelColor, Color valueColor, TMP_FontAsset font = null)
    {
        GameObject row = CreateObject("InfoRow", parent);
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true;
        hlg.childForceExpandWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandHeight = true;

        // Row Height
        LayoutElement le = row.AddComponent<LayoutElement>();
        le.minHeight = 35; // Increased height for larger text
        le.preferredHeight = 35;

        // Label
        GameObject labelObj = CreateText("Label", row.transform, label, 20, labelColor, FontStyles.Normal, font); // Size 20
        labelObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
        LayoutElement labelLe = labelObj.AddComponent<LayoutElement>();
        labelLe.minWidth = 140; // Increased label width

        // Value
        GameObject valueObj = CreateText("Value", row.transform, value, 20, valueColor, FontStyles.Bold, font); // Size 20
        valueObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Right;
        LayoutElement valueLe = valueObj.AddComponent<LayoutElement>();
        valueLe.flexibleWidth = 1;

        return row;
    }
}
