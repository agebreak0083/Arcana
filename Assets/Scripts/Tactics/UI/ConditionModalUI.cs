using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        private TacticsManager _manager;
        private string _selectedCategory;

        public void Setup(TacticsManager manager)
        {
            _manager = manager;
            Close();
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

            foreach (var cat in TacticsDatabase.Conditions.Keys)
            {
                CreateCategoryButton(cat.Replace("_", " "), () => SelectCategory(cat), false);
            }
        }

        private void CreateCategoryButton(string label, UnityEngine.Events.UnityAction onClick, bool isSpecial)
        {
            var go = Instantiate(categoryItemPrefab, categoryContainer);
            var btn = go.GetComponent<Button>();
            var txt = go.GetComponentInChildren<TextMeshProUGUI>();
            txt.text = label;
            
            if (isSpecial)
            {
                var img = go.GetComponent<Image>();
                if(img) img.color = new Color(0.7f, 0.2f, 0.2f); // Reddish
            }

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

            if (TacticsDatabase.Conditions.TryGetValue(category, out var conditions))
            {
                foreach (var cond in conditions)
                {
                    var go = Instantiate(detailItemPrefab, detailContainer);
                    var btn = go.GetComponent<Button>();
                    var txt = go.GetComponentInChildren<TextMeshProUGUI>();
                    txt.text = cond;
                    btn.onClick.AddListener(() => _manager.OnConditionSelected(cond));
                }
            }
        }

        private void ClearDetails()
        {
            foreach (Transform child in detailContainer) Destroy(child.gameObject);
            // Show "Select Category" text if needed
        }
    }
}
