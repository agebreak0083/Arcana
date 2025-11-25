using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Arcana.Tactics.UI
{
    public class DetailPanelBuilder : MonoBehaviour
    {
        [ContextMenu("Create Detail Panel Prefab")]
        public void CreateDetailPanelPrefab()
        {
#if UNITY_EDITOR
            // Load Font
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/PretendardVariable SDF.asset");
            if (fontAsset == null) Debug.LogWarning("Font not found at Assets/TextMesh Pro/Fonts/PretendardVariable SDF.asset");

            // Detail Panel
            GameObject detailPanel = CreateImageObject("DetailPanel", null, new Color32(31, 41, 55, 255));
            RectTransform detailRect = detailPanel.GetComponent<RectTransform>();
            detailRect.sizeDelta = new Vector2(800, 280);

            HorizontalLayoutGroup detailHlg = detailPanel.AddComponent<HorizontalLayoutGroup>();
            detailHlg.spacing = 30;
            detailHlg.padding = new RectOffset(30, 30, 30, 30);
            detailHlg.childControlHeight = true;
            detailHlg.childForceExpandHeight = true;
            detailHlg.childControlWidth = true;
            detailHlg.childForceExpandWidth = false;

            // Portrait Container
            GameObject portraitContainer = CreateObject("PortraitContainer", detailPanel.transform);
            LayoutElement portLe = portraitContainer.AddComponent<LayoutElement>();
            portLe.minWidth = 180; portLe.minHeight = 180;
            portLe.preferredWidth = 180; portLe.preferredHeight = 180;

            Image portBg = portraitContainer.AddComponent<Image>();
            portBg.color = new Color32(31, 41, 55, 255);
            Outline portOutline = portraitContainer.AddComponent<Outline>();
            portOutline.effectColor = new Color32(252, 211, 77, 255);
            portOutline.effectDistance = new Vector2(4, 4);

            GameObject portraitImgObj = CreateImageObject("PortraitImage", portraitContainer.transform, Color.white);
            Stretch(portraitImgObj.GetComponent<RectTransform>());

            // Cost Badge
            GameObject costBadge = CreateImageObject("CostBadge", portraitContainer.transform, new Color32(239, 68, 68, 255));
            RectTransform costRect = costBadge.GetComponent<RectTransform>();
            costRect.anchorMin = new Vector2(1, 1); costRect.anchorMax = new Vector2(1, 1);
            costRect.pivot = new Vector2(1, 1); costRect.anchoredPosition = Vector2.zero;
            costRect.sizeDelta = new Vector2(80, 30);
            GameObject costText = CreateText("CostText_Detail", costBadge.transform, "COST: 3", 16, Color.white, FontStyles.Bold, fontAsset);
            costText.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 30);
            costText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            // Position Label
            GameObject posLabel = CreateImageObject("PosLabel", portraitContainer.transform, new Color32(0, 0, 0, 200));
            RectTransform posRect = posLabel.GetComponent<RectTransform>();
            posRect.anchorMin = new Vector2(0, 0); posRect.anchorMax = new Vector2(1, 0);
            posRect.pivot = new Vector2(0.5f, 0); posRect.anchoredPosition = Vector2.zero;
            posRect.sizeDelta = new Vector2(0, 40);
            GameObject posText = CreateText("PosText", posLabel.transform, "전열 1", 18, Color.white, FontStyles.Bold, fontAsset);
            Stretch(posText.GetComponent<RectTransform>());
            posText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            // Info Area
            GameObject infoArea = CreateObject("InfoArea", detailPanel.transform);
            LayoutElement infoLe = infoArea.AddComponent<LayoutElement>();
            infoLe.flexibleWidth = 1;

            VerticalLayoutGroup infoVlg = infoArea.AddComponent<VerticalLayoutGroup>();
            infoVlg.spacing = 10;
            infoVlg.childControlHeight = false;
            infoVlg.childForceExpandHeight = false;
            infoVlg.childControlWidth = true;
            infoVlg.childForceExpandWidth = true;

            // Name and Class in one line
            GameObject nameClassRow = CreateObject("NameClassRow", infoArea.transform);
            HorizontalLayoutGroup nameClassHlg = nameClassRow.AddComponent<HorizontalLayoutGroup>();
            nameClassHlg.spacing = 10;
            nameClassHlg.childControlWidth = false;
            nameClassHlg.childForceExpandWidth = false;
            nameClassHlg.childControlHeight = true;
            nameClassHlg.childForceExpandHeight = false;

            GameObject nameText = CreateText("Name", nameClassRow.transform, "Raina", 32, new Color32(45, 212, 191, 255), FontStyles.Bold, fontAsset);
            GameObject slashText = CreateText("Slash", nameClassRow.transform, "/", 32, new Color32(156, 163, 175, 255), FontStyles.Normal, fontAsset);
            GameObject classText = CreateText("Class", nameClassRow.transform, "파이터", 32, Color.white, FontStyles.Bold, fontAsset);

            GameObject description = CreateText("Description", infoArea.transform, "물리 방어가 뛰어난 성능", 18, new Color32(209, 213, 219, 255), FontStyles.Normal, fontAsset);
            description.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 60);

            // Stats UI - Title
            GameObject statsTitle = CreateImageObject("StatsTitle", infoArea.transform, new Color32(139, 92, 46, 255)); // Brown
            statsTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 35);
            GameObject statsTitleText = CreateText("Text", statsTitle.transform, "스테이터스", 20, Color.white, FontStyles.Bold, fontAsset);
            Stretch(statsTitleText.GetComponent<RectTransform>());
            statsTitleText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            // AP / PP Row
            GameObject apppRow = CreateImageObject("APPPRow", infoArea.transform, new Color32(55, 48, 45, 255)); // Dark brown
            apppRow.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 35);
            HorizontalLayoutGroup apppHlg = apppRow.AddComponent<HorizontalLayoutGroup>();
            apppHlg.childControlWidth = true;
            apppHlg.childForceExpandWidth = true;
            apppHlg.childControlHeight = true;
            apppHlg.childForceExpandHeight = true;
            apppHlg.spacing = 0;

            GameObject apLabel = CreateText("APLabel", apppRow.transform, "AP", 20, new Color32(239, 68, 68, 255), FontStyles.Bold, fontAsset); // Red
            apLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            GameObject apDiamond = CreateText("APDiamond", apppRow.transform, "◆", 20, new Color32(239, 68, 68, 255), FontStyles.Normal, fontAsset);
            apDiamond.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            GameObject ppLabel = CreateText("PPLabel", apppRow.transform, "PP", 20, new Color32(96, 165, 250, 255), FontStyles.Bold, fontAsset); // Blue
            ppLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            GameObject ppDiamond = CreateText("PPDiamond", apppRow.transform, "◆", 20, new Color32(96, 165, 250, 255), FontStyles.Normal, fontAsset);
            ppDiamond.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            // Stats Grid Container
            GameObject statsGrid = CreateObject("StatsGrid", infoArea.transform);
            VerticalLayoutGroup statsVlg = statsGrid.AddComponent<VerticalLayoutGroup>();
            statsVlg.spacing = 0;
            statsVlg.childControlWidth = true;
            statsVlg.childForceExpandWidth = true;
            statsVlg.childControlHeight = false;
            statsVlg.childForceExpandHeight = false;

            // Row 1 Header (HP, 물리공격, 물리방어, 마법공격, 마법방어)
            CreateStatsHeaderRow(statsGrid.transform, new string[] { "HP", "물리공격", "물리방어", "마법공격", "마법방어" }, fontAsset);

            // Row 1 Values (Fighter stats: C, C, B, D, E)
            CreateStatsValueRow(statsGrid.transform, new string[] { "C", "C", "B", "D", "E" }, fontAsset);

            // Row 2 Header (명중, 회피, 치명타율, 가드율, 행동속도)
            CreateStatsHeaderRow(statsGrid.transform, new string[] { "명중", "회피", "치명타율", "가드율", "행동속도" }, fontAsset);

            // Row 2 Values (Fighter stats: C, E, D, S, C)
            CreateStatsValueRow(statsGrid.transform, new string[] { "C", "E", "D", "S", "C" }, fontAsset);

            GameObject removeButton = CreateButton("RemoveButton", infoArea.transform, "부대에서 제거", new Color32(127, 29, 29, 255), fontAsset);
            removeButton.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 40);

            // Save as Prefab
            string folderPath = "Assets/Prefabs/UI";
            if (!System.IO.Directory.Exists(folderPath)) System.IO.Directory.CreateDirectory(folderPath);
            string prefabPath = $"{folderPath}/DetailPanelPrefab.prefab";
            PrefabUtility.SaveAsPrefabAsset(detailPanel, prefabPath);
            Debug.Log($"DetailPanel Prefab created at {prefabPath}");

            DestroyImmediate(detailPanel);
#endif
        }

        private GameObject CreateObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent);
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
            le.minHeight = 35;
            le.preferredHeight = 35;

            // Label
            GameObject labelObj = CreateText("Label", row.transform, label, 20, labelColor, FontStyles.Normal, font);
            labelObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
            LayoutElement labelLe = labelObj.AddComponent<LayoutElement>();
            labelLe.minWidth = 140;

            // Value
            GameObject valueObj = CreateText("Value", row.transform, value, 20, valueColor, FontStyles.Bold, font);
            valueObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Right;
            LayoutElement valueLe = valueObj.AddComponent<LayoutElement>();
            valueLe.flexibleWidth = 1;

            return row;
        }

        private void CreateStatsHeaderRow(Transform parent, string[] labels, TMP_FontAsset font = null)
        {
            GameObject headerRow = CreateImageObject("StatsHeaderRow", parent, new Color32(139, 92, 46, 255)); // Brown
            headerRow.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 30);
            HorizontalLayoutGroup hlg = headerRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 0;
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandHeight = true;

            foreach (string label in labels)
            {
                GameObject cell = CreateText($"Header_{label}", headerRow.transform, label, 16, Color.white, FontStyles.Bold, font);
                cell.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            }
        }

        private void CreateStatsValueRow(Transform parent, string[] values, TMP_FontAsset font = null)
        {
            GameObject valueRow = CreateImageObject("StatsValueRow", parent, new Color32(55, 48, 45, 255)); // Dark brown
            valueRow.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 35);
            HorizontalLayoutGroup hlg = valueRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 0;
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandHeight = true;

            foreach (string value in values)
            {
                GameObject cell = CreateText($"Value_{value}", valueRow.transform, value, 20, Color.white, FontStyles.Bold, font);
                cell.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            }
        }
    }
}
