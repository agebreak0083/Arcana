using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.InferenceEngine.Tokenization.PostProcessors.Templating;

namespace Arcana.Tactics.UI
{
    public class ConditionModalUI : MonoBehaviour
    {
        [Header("UI References")]
        public Transform categoryContainer;
        public Transform detailContainer;
        public GameObject categoryItemPrefab; // Button with Text
        public GameObject detailItemPrefab;   // Button with Text
        public GameObject modalRoot;
        public Button closeBtn;

        private TacticsManager _manager;
        private string _selectedCategory;

        // Loaded data
        private Dictionary<string, List<string>> _conditions = new Dictionary<string, List<string>>();

        private void Awake()
        {
            if (modalRoot == null) modalRoot = gameObject;
        }

        public void Setup(TacticsManager manager)
        {
            _manager = manager;
            if (modalRoot == null) modalRoot = gameObject;

            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(Close);
            }

            LoadConditions();
            Close();
        }

        private void LoadConditions()
        {
            _conditions.Clear();
            TextAsset asset = Resources.Load<TextAsset>("Table/TacticsData");
            if (asset != null)
            {
                TacticsDataWrapper wrapper = JsonUtility.FromJson<TacticsDataWrapper>(asset.text);
                if (wrapper != null && wrapper.categories != null)
                {
                    foreach (var cat in wrapper.categories)
                    {
                        if (!string.IsNullOrEmpty(cat.name) && cat.conditions != null)
                        {
                            _conditions[cat.name] = new List<string>(cat.conditions);
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("Failed to load TacticsData.json");
                // Fallback to TacticsDatabase if needed, or just leave empty
                foreach (var kvp in TacticsDatabase.Conditions)
                {
                    _conditions[kvp.Key] = kvp.Value;
                }
            }
        }

        public void Open()
        {
            modalRoot.SetActive(true);
            RenderCategories();
            ClearDetails();
        }

        public void Close()
        {
            modalRoot.SetActive(false);
        }

        private void RenderCategories()
        {
            // Clear existing
            foreach (Transform child in categoryContainer) Destroy(child.gameObject);

            // Add "No Condition" special category/button
            CreateCategoryButton("조건 없음 (초기화)", () => _manager.OnConditionSelected(TacticsDatabase.DEFAULT_CONDITION), true);

            foreach (var cat in _conditions.Keys)
            {
                CreateCategoryButton(cat.Replace("_", " "), () => SelectCategory(cat), false);
            }
        }

        private void CreateCategoryButton(string label, UnityEngine.Events.UnityAction onClick, bool isSpecial)
        {
            GameObject go;
            if (categoryItemPrefab != null)
            {
                go = Instantiate(categoryItemPrefab, categoryContainer);
            }
            else
            {
                go = CreateDefaultButtonObject(categoryContainer);
            }

            var btn = go.GetComponent<Button>();
            var txt = go.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = label;

            if (isSpecial)
            {
                var img = go.GetComponent<Image>();
                if (img) img.color = new Color(0.7f, 0.2f, 0.2f); // Reddish
            }

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(onClick);
        }

        private void SelectCategory(string category)
        {
            _selectedCategory = category;
            RenderDetails(category);
        }

        private void RenderDetails(string category)
        {
            foreach (Transform child in detailContainer) Destroy(child.gameObject);

            if (_conditions.TryGetValue(category, out var conditions))
            {
                foreach (var cond in conditions)
                {
                    GameObject go;
                    if (detailItemPrefab != null)
                    {
                        go = Instantiate(detailItemPrefab, detailContainer);
                    }
                    else
                    {
                        go = CreateDefaultButtonObject(detailContainer);
                    }

                    var btn = go.GetComponent<Button>();
                    var txt = go.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null) txt.text = cond;

                    string conditionText = cond; // Capture for lambda
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => _manager.OnConditionSelected(conditionText));
                }
            }
        }

        private GameObject CreateDefaultButtonObject(Transform parent)
        {
            GameObject go = new GameObject("Button");
            go.transform.SetParent(parent);
            go.AddComponent<RectTransform>();

            Image img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f);

            Button btn = go.AddComponent<Button>();

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(go.transform);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 14;
            tmp.color = Color.white;

            return go;
        }

        private void ClearDetails()
        {
            foreach (Transform child in detailContainer) Destroy(child.gameObject);
        }

        [System.Serializable]
        private class TacticsDataWrapper
        {
            public CategoryData[] categories;
        }

        [System.Serializable]
        private class CategoryData
        {
            public string name;
            public string[] conditions;
        }
    }
}
