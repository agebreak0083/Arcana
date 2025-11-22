using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Arcana.Tactics.Data;

namespace Arcana.Tactics.UI
{
    public class TacticRowUI : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI indexText;
        public TextMeshProUGUI skillNameText;
        public Button condition1Btn;
        public TextMeshProUGUI condition1Text;
        public Button condition2Btn;
        public TextMeshProUGUI condition2Text;

        private TacticsManager _manager;
        private int _rowIndex;
        private string _charId;

        public void Setup(TacticsManager manager, string charId, int rowIndex, TacticRow rowData)
        {
            _manager = manager;
            _charId = charId;
            _rowIndex = rowIndex;

            indexText.text = (rowIndex + 1).ToString();
            skillNameText.text = $"{rowData.skillName} ({rowData.skillType})";
            
            // Color coding for AP/PP could be added here
            if (rowData.skillType == "AP") skillNameText.color = new Color(0.4f, 1f, 0.8f); // Teal-ish
            else skillNameText.color = new Color(0.8f, 0.6f, 1f); // Purple-ish

            condition1Text.text = rowData.condition1;
            condition2Text.text = rowData.condition2;

            condition1Btn.onClick.RemoveAllListeners();
            condition1Btn.onClick.AddListener(() => _manager.OnConditionClicked(_charId, _rowIndex, 1));

            condition2Btn.onClick.RemoveAllListeners();
            condition2Btn.onClick.AddListener(() => _manager.OnConditionClicked(_charId, _rowIndex, 2));
        }
    }
}
