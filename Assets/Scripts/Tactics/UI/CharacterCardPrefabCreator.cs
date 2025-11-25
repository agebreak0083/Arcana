// 2025-11-24 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Arcana.Tactics.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CharacterCardPrefabCreator : MonoBehaviour
{
    // ==================================================================================
    // 1.1 CharacterCardPrefab
    // ==================================================================================
    [ContextMenu("Create CharacterCardPrefab")]
    private void CreateCharacterCardPrefab()
    {
#if UNITY_EDITOR
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/BMHANNAPro SDF.asset");
#else
        TMP_FontAsset fontAsset = null;
#endif

        // Create the root object
        GameObject characterCard = new GameObject("CharacterCard");
        RectTransform rootRectTransform = characterCard.AddComponent<RectTransform>();
        rootRectTransform.sizeDelta = new Vector2(140, 140);
        Image rootImage = characterCard.AddComponent<Image>();
        rootImage.color = new Color32(31, 41, 55, 255); // #1F2937

        // Add Component
        CharacterCardUI cardUI = characterCard.AddComponent<CharacterCardUI>();

        // Create Portrait
        GameObject portrait = new GameObject("Portrait");
        portrait.transform.SetParent(characterCard.transform);
        RectTransform portraitRectTransform = portrait.AddComponent<RectTransform>();
        portraitRectTransform.anchorMin = new Vector2(0.02f, 0.02f);
        portraitRectTransform.anchorMax = new Vector2(0.98f, 0.98f);
        portraitRectTransform.offsetMin = Vector2.zero;
        portraitRectTransform.offsetMax = Vector2.zero;
        Image portraitImage = portrait.AddComponent<Image>();
        portraitImage.color = Color.white;
        cardUI.portraitImage = portraitImage;

        // Create Overlay
        GameObject overlay = new GameObject("Overlay");
        overlay.transform.SetParent(characterCard.transform);
        RectTransform overlayRectTransform = overlay.AddComponent<RectTransform>();
        overlayRectTransform.anchorMin = new Vector2(0, 0);
        overlayRectTransform.anchorMax = new Vector2(1, 0);
        overlayRectTransform.sizeDelta = new Vector2(0, 40);
        Image overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = new Color32(0, 0, 0, 200); // #000000 Alpha 200

        // Create NameText
        GameObject nameText = new GameObject("NameText");
        nameText.transform.SetParent(overlay.transform);
        RectTransform nameTextRectTransform = nameText.AddComponent<RectTransform>();
        nameTextRectTransform.anchorMin = new Vector2(0, 1);
        nameTextRectTransform.anchorMax = new Vector2(1, 1);
        nameTextRectTransform.offsetMin = new Vector2(0, -20);
        nameTextRectTransform.offsetMax = new Vector2(0, -5);
        TextMeshProUGUI nameTextTMP = nameText.AddComponent<TextMeshProUGUI>();
        nameTextTMP.fontSize = 14;
        nameTextTMP.fontStyle = FontStyles.Bold;
        nameTextTMP.color = Color.white;
        nameTextTMP.alignment = TextAlignmentOptions.Center;
        if (fontAsset != null) nameTextTMP.font = fontAsset;
        cardUI.nameText = nameTextTMP;

        // Create ClassText
        GameObject classText = new GameObject("ClassText");
        classText.transform.SetParent(overlay.transform);
        RectTransform classTextRectTransform = classText.AddComponent<RectTransform>();
        classTextRectTransform.anchorMin = new Vector2(0, 0);
        classTextRectTransform.anchorMax = new Vector2(1, 0);
        classTextRectTransform.offsetMin = new Vector2(0, 5);
        classTextRectTransform.offsetMax = new Vector2(0, 15);
        TextMeshProUGUI classTextTMP = classText.AddComponent<TextMeshProUGUI>();
        classTextTMP.fontSize = 11;
        classTextTMP.color = new Color32(45, 212, 191, 255); // #2DD4BF
        classTextTMP.alignment = TextAlignmentOptions.Center;
        if (fontAsset != null) classTextTMP.font = fontAsset;
        cardUI.classText = classTextTMP;

        // Create CostBadge
        GameObject costBadge = new GameObject("CostBadge");
        costBadge.transform.SetParent(characterCard.transform);
        RectTransform costBadgeRectTransform = costBadge.AddComponent<RectTransform>();
        costBadgeRectTransform.anchorMin = new Vector2(0, 1);
        costBadgeRectTransform.anchorMax = new Vector2(0, 1);
        costBadgeRectTransform.sizeDelta = new Vector2(30, 30);
        costBadgeRectTransform.anchoredPosition = new Vector2(15, -15); // Adjusted for corner
        Image costBadgeImage = costBadge.AddComponent<Image>();
        costBadgeImage.color = new Color32(16, 185, 129, 255); // #10B981

        GameObject costText = new GameObject("CostText");
        costText.transform.SetParent(costBadge.transform);
        RectTransform costTextRect = costText.AddComponent<RectTransform>();
        costTextRect.anchorMin = Vector2.zero;
        costTextRect.anchorMax = Vector2.one;
        costTextRect.offsetMin = Vector2.zero;
        costTextRect.offsetMax = Vector2.zero;
        TextMeshProUGUI costTextTMP = costText.AddComponent<TextMeshProUGUI>();
        costTextTMP.fontSize = 16;
        costTextTMP.fontStyle = FontStyles.Bold;
        costTextTMP.color = Color.white;
        costTextTMP.alignment = TextAlignmentOptions.Center;
        if (fontAsset != null) costTextTMP.font = fontAsset;
        cardUI.costText = costTextTMP;

        // Create SelectedHighlight
        GameObject selectedHighlight = new GameObject("SelectedHighlight");
        selectedHighlight.transform.SetParent(characterCard.transform);
        RectTransform selectedHighlightRectTransform = selectedHighlight.AddComponent<RectTransform>();
        selectedHighlightRectTransform.anchorMin = new Vector2(0, 0);
        selectedHighlightRectTransform.anchorMax = new Vector2(1, 1);
        selectedHighlightRectTransform.offsetMin = Vector2.zero;
        selectedHighlightRectTransform.offsetMax = Vector2.zero;
        Image selectedHighlightImage = selectedHighlight.AddComponent<Image>();
        selectedHighlightImage.color = new Color(0, 0, 0, 0); // Transparent
        Outline outline = selectedHighlight.AddComponent<Outline>();
        outline.effectColor = new Color32(252, 211, 77, 255); // #FCD34D
        outline.effectDistance = new Vector2(4, 4);
        selectedHighlight.SetActive(false);
        cardUI.selectedHighlight = selectedHighlight;

        // Create DeployedOverlay
        GameObject deployedOverlay = new GameObject("DeployedOverlay");
        deployedOverlay.transform.SetParent(characterCard.transform);
        RectTransform deployedOverlayRectTransform = deployedOverlay.AddComponent<RectTransform>();
        deployedOverlayRectTransform.anchorMin = new Vector2(0, 0);
        deployedOverlayRectTransform.anchorMax = new Vector2(1, 1);
        deployedOverlayRectTransform.offsetMin = Vector2.zero;
        deployedOverlayRectTransform.offsetMax = Vector2.zero;
        Image deployedOverlayImage = deployedOverlay.AddComponent<Image>();
        deployedOverlayImage.color = new Color32(0, 0, 0, 150); // #000000 Alpha 150
        deployedOverlay.SetActive(false);
        cardUI.deployedOverlay = deployedOverlay;

        // Save as prefab
        SavePrefab(characterCard, "CharacterCardPrefab");
    }

    // ==================================================================================
    // 1.2 FormationSlotPrefab
    // ==================================================================================
    [ContextMenu("Create FormationSlotPrefab")]
    private void CreateFormationSlotPrefab()
    {
#if UNITY_EDITOR
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/BMHANNAPro SDF.asset");
#else
        TMP_FontAsset fontAsset = null;
#endif

        // Root
        GameObject formationSlot = new GameObject("FormationSlot");
        RectTransform rootRect = formationSlot.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(160, 160);
        Image rootImage = formationSlot.AddComponent<Image>();
        rootImage.color = new Color32(55, 65, 81, 255); // #374151

        FormationSlotUI slotUI = formationSlot.AddComponent<FormationSlotUI>();

        // 1. EmptyState
        GameObject emptyState = new GameObject("EmptyState");
        emptyState.transform.SetParent(formationSlot.transform);
        RectTransform emptyRect = emptyState.AddComponent<RectTransform>();
        emptyRect.anchorMin = Vector2.zero;
        emptyRect.anchorMax = Vector2.one;
        emptyRect.offsetMin = Vector2.zero;
        emptyRect.offsetMax = Vector2.zero;
        slotUI.emptyStateObject = emptyState;

        // PlusText
        GameObject plusText = new GameObject("PlusText");
        plusText.transform.SetParent(emptyState.transform);
        RectTransform plusRect = plusText.AddComponent<RectTransform>();
        plusRect.anchorMin = Vector2.zero;
        plusRect.anchorMax = Vector2.one;
        plusRect.offsetMin = Vector2.zero;
        plusRect.offsetMax = Vector2.zero;
        TextMeshProUGUI plusTMP = plusText.AddComponent<TextMeshProUGUI>();
        plusTMP.text = "+";
        plusTMP.fontSize = 40;
        plusTMP.color = new Color32(156, 163, 175, 255); // #9CA3AF
        plusTMP.alignment = TextAlignmentOptions.Center;
        if (fontAsset != null) plusTMP.font = fontAsset;

        // PosText
        GameObject posText = new GameObject("PosText");
        posText.transform.SetParent(emptyState.transform);
        RectTransform posRect = posText.AddComponent<RectTransform>();
        posRect.anchorMin = new Vector2(0, 0);
        posRect.anchorMax = new Vector2(1, 0.3f);
        posRect.offsetMin = Vector2.zero;
        posRect.offsetMax = Vector2.zero;
        TextMeshProUGUI posTMP = posText.AddComponent<TextMeshProUGUI>();
        posTMP.text = "Front 1";
        posTMP.fontSize = 14;
        posTMP.color = new Color32(156, 163, 175, 255); // #9CA3AF
        posTMP.alignment = TextAlignmentOptions.Bottom;
        if (fontAsset != null) posTMP.font = fontAsset;
        slotUI.slotLabel = posTMP;

        // 2. FilledState
        GameObject filledState = new GameObject("FilledState");
        filledState.transform.SetParent(formationSlot.transform);
        RectTransform filledRect = filledState.AddComponent<RectTransform>();
        filledRect.anchorMin = Vector2.zero;
        filledRect.anchorMax = Vector2.one;
        filledRect.offsetMin = Vector2.zero;
        filledRect.offsetMax = Vector2.zero;
        filledState.SetActive(false);
        slotUI.filledStateObject = filledState;

        // Portrait
        GameObject portrait = new GameObject("Portrait");
        portrait.transform.SetParent(filledState.transform);
        RectTransform portRect = portrait.AddComponent<RectTransform>();
        portRect.anchorMin = Vector2.zero;
        portRect.anchorMax = Vector2.one;
        portRect.offsetMin = Vector2.zero;
        portRect.offsetMax = Vector2.zero;
        Image portImage = portrait.AddComponent<Image>();
        portImage.color = Color.white;
        slotUI.characterPortrait = portImage;

        // CostText
        GameObject costObj = new GameObject("CostText");
        costObj.transform.SetParent(filledState.transform);
        RectTransform costRect = costObj.AddComponent<RectTransform>();
        costRect.anchorMin = new Vector2(0, 1);
        costRect.anchorMax = new Vector2(0, 1);
        costRect.pivot = new Vector2(0, 1);
        costRect.anchoredPosition = new Vector2(5, -5);
        costRect.sizeDelta = new Vector2(50, 20);
        TextMeshProUGUI costTMP = costObj.AddComponent<TextMeshProUGUI>();
        costTMP.text = "3C";
        costTMP.fontSize = 14;
        costTMP.fontStyle = FontStyles.Bold;
        costTMP.color = Color.white;
        costTMP.alignment = TextAlignmentOptions.TopLeft;
        if (fontAsset != null) costTMP.font = fontAsset;
        slotUI.charCostText = costTMP;

        // InfoOverlay
        GameObject infoOverlay = new GameObject("InfoOverlay");
        infoOverlay.transform.SetParent(filledState.transform);
        RectTransform infoRect = infoOverlay.AddComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0, 0);
        infoRect.anchorMax = new Vector2(1, 0);
        infoRect.sizeDelta = new Vector2(0, 30);
        Image infoImage = infoOverlay.AddComponent<Image>();
        infoImage.color = new Color32(0, 0, 0, 200);

        // NameText
        GameObject nameText = new GameObject("NameText");
        nameText.transform.SetParent(infoOverlay.transform);
        RectTransform nameRect = nameText.AddComponent<RectTransform>();
        nameRect.anchorMin = Vector2.zero;
        nameRect.anchorMax = Vector2.one;
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;
        TextMeshProUGUI nameTMP = nameText.AddComponent<TextMeshProUGUI>();
        nameTMP.fontSize = 14;
        nameTMP.color = Color.white;
        nameTMP.alignment = TextAlignmentOptions.Center;
        if (fontAsset != null) nameTMP.font = fontAsset;
        slotUI.charNameText = nameTMP;

        // 3. ActiveHighlight
        GameObject activeHighlight = new GameObject("ActiveHighlight");
        activeHighlight.transform.SetParent(formationSlot.transform);
        RectTransform activeRect = activeHighlight.AddComponent<RectTransform>();
        activeRect.anchorMin = Vector2.zero;
        activeRect.anchorMax = Vector2.one;
        activeRect.offsetMin = Vector2.zero;
        activeRect.offsetMax = Vector2.zero;
        Image activeImage = activeHighlight.AddComponent<Image>();
        activeImage.color = Color.clear;
        Outline activeOutline = activeHighlight.AddComponent<Outline>();
        activeOutline.effectColor = new Color32(45, 212, 191, 255); // #2DD4BF
        activeOutline.effectDistance = new Vector2(4, 4);
        activeHighlight.SetActive(false);
        slotUI.activeHighlight = activeHighlight;

        SavePrefab(formationSlot, "FormationSlotPrefab");
    }

    // ==================================================================================
    // 1.3 TacticRowPrefab
    // ==================================================================================
    [ContextMenu("Create TacticRowPrefab")]
    private void CreateTacticRowPrefab()
    {
#if UNITY_EDITOR
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/BMHANNAPro SDF.asset");
#else
        TMP_FontAsset fontAsset = null;
#endif

        // Root
        GameObject tacticRow = new GameObject("TacticRow");
        RectTransform rootRect = tacticRow.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(1000, 60);
        Image rootImage = tacticRow.AddComponent<Image>();
        rootImage.color = new Color32(31, 41, 55, 255); // #1F2937

        TacticRowUI rowUI = tacticRow.AddComponent<TacticRowUI>();

        // Horizontal Layout Group
        HorizontalLayoutGroup hlg = tacticRow.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = 10;
        hlg.padding = new RectOffset(10, 10, 0, 0);
        hlg.childAlignment = TextAnchor.MiddleLeft;

        // 1. IndexText
        GameObject indexText = new GameObject("IndexText");
        indexText.transform.SetParent(tacticRow.transform);
        RectTransform indexRect = indexText.AddComponent<RectTransform>();
        indexRect.sizeDelta = new Vector2(50, 60);
        TextMeshProUGUI indexTMP = indexText.AddComponent<TextMeshProUGUI>();
        indexTMP.text = "1";
        indexTMP.fontSize = 18;
        indexTMP.fontStyle = FontStyles.Bold;
        indexTMP.color = new Color32(252, 211, 77, 255); // #FCD34D
        indexTMP.alignment = TextAlignmentOptions.Center;
        if (fontAsset != null) indexTMP.font = fontAsset;
        rowUI.indexText = indexTMP;

        // 2. SkillNameText
        GameObject skillText = new GameObject("SkillNameText");
        skillText.transform.SetParent(tacticRow.transform);
        RectTransform skillRect = skillText.AddComponent<RectTransform>();
        skillRect.sizeDelta = new Vector2(200, 60);
        TextMeshProUGUI skillTMP = skillText.AddComponent<TextMeshProUGUI>();
        skillTMP.text = "Skill Name";
        skillTMP.fontSize = 16;
        skillTMP.color = new Color32(45, 212, 191, 255); // #2DD4BF
        skillTMP.alignment = TextAlignmentOptions.Left;
        if (fontAsset != null) skillTMP.font = fontAsset;
        rowUI.skillNameText = skillTMP;

        // 3. Condition1Btn
        GameObject cond1BtnObj = CreateConditionButton("Condition1Btn", tacticRow.transform, fontAsset);
        rowUI.condition1Btn = cond1BtnObj.GetComponent<Button>();
        rowUI.condition1Text = cond1BtnObj.GetComponentInChildren<TextMeshProUGUI>();

        // 4. AndText
        GameObject andText = new GameObject("AndText");
        andText.transform.SetParent(tacticRow.transform);
        RectTransform andRect = andText.AddComponent<RectTransform>();
        andRect.sizeDelta = new Vector2(50, 60);
        TextMeshProUGUI andTMP = andText.AddComponent<TextMeshProUGUI>();
        andTMP.text = "AND";
        andTMP.fontSize = 12;
        andTMP.fontStyle = FontStyles.Bold;
        andTMP.color = new Color32(252, 211, 77, 255); // #FCD34D
        andTMP.alignment = TextAlignmentOptions.Center;
        if (fontAsset != null) andTMP.font = fontAsset;

        // 5. Condition2Btn
        GameObject cond2BtnObj = CreateConditionButton("Condition2Btn", tacticRow.transform, fontAsset);
        rowUI.condition2Btn = cond2BtnObj.GetComponent<Button>();
        rowUI.condition2Text = cond2BtnObj.GetComponentInChildren<TextMeshProUGUI>();

        SavePrefab(tacticRow, "TacticRowPrefab");
    }

    private GameObject CreateConditionButton(string name, Transform parent, TMP_FontAsset font)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent);
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(250, 40);

        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color32(17, 24, 39, 255); // #111827
        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color32(75, 85, 99, 255); // #4B5563
        outline.effectDistance = new Vector2(1, -1);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 0);
        textRect.offsetMax = new Vector2(-10, 0);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "조건 없음";
        tmp.fontSize = 14;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        if (font != null) tmp.font = font;

        return btnObj;
    }

    private void SavePrefab(GameObject obj, string name)
    {
        string folderPath = "Assets/Prefabs/UI";
        if (!System.IO.Directory.Exists(folderPath))
        {
            System.IO.Directory.CreateDirectory(folderPath);
        }

        string prefabPath = $"{folderPath}/{name}.prefab";

#if UNITY_EDITOR
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
        Debug.Log($"Prefab created at {prefabPath}");
#endif
        DestroyImmediate(obj);
    }
}