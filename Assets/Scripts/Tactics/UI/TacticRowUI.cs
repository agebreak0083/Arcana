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
        public Button skillNameBtn;
        public Button condition1Btn;
        public TextMeshProUGUI condition1Text;
        public Button condition2Btn;
        public TextMeshProUGUI condition2Text;
        public TextMeshProUGUI ap_pp_Star; 

        private TacticsUIManager _manager;
        private int _rowIndex;
        private string _charName;

        public void Setup(TacticsUIManager manager, string charName, int rowIndex, TacticRow rowData)
        {
            _manager = manager;
            _charName = charName;
            _rowIndex = rowIndex;

            indexText.text = (rowIndex + 1).ToString();
            skillNameBtn.GetComponentInChildren<TextMeshProUGUI>().text = rowData == null ? "---" : rowData.skillName;
            skillNameBtn.onClick.RemoveAllListeners();
            skillNameBtn.onClick.AddListener(() => _manager.OnSkillNameClicked(_charName, _rowIndex));

            if (rowData == null)
            {
                skillNameBtn.GetComponentInChildren<TextMeshProUGUI>().color = new Color(0.5f, 0.5f, 0.5f); // Gray
                ap_pp_Star.text = "";
            }
            else
            {
                // 스킬 정보 가져오기
                Skill skill = null;
                if (!string.IsNullOrEmpty(rowData.skillName) && rowData.skillName != "---" && SkillManager.Instance != null)
                {
                    skill = SkillManager.Instance.GetSkillByName(rowData.skillName);
                }

                // Color coding: AP (Red), PP (Blue)
                if (rowData.skillType == "AP") 
                {
                    skillNameBtn.GetComponentInChildren<TextMeshProUGUI>().color = new Color(1f, 0.4f, 0.4f); // Reddish
                    ap_pp_Star.color = new Color(1f, 0.4f, 0.4f);

                    ap_pp_Star.text = "";
                    int costAP = skill != null ? skill.costAP : 0;
                    for(int i = 0; i < costAP; i++)
                    {
                        ap_pp_Star.text += "★";
                    }
                }
                else 
                {
                    skillNameBtn.GetComponentInChildren<TextMeshProUGUI>().color = new Color(0.4f, 0.6f, 1f); // Blueish
                    ap_pp_Star.color = new Color(0.4f, 0.6f, 1f);

                    ap_pp_Star.text = "";
                    int costPP = skill != null ? skill.costPP : 0;
                    for(int i = 0; i < costPP; i++)
                    {
                        ap_pp_Star.text += "★";
                    }
                }                
            }

            condition1Btn.GetComponentInChildren<TextMeshProUGUI>().text = rowData == null? "조건 없음" : rowData.condition1;
            condition2Btn.GetComponentInChildren<TextMeshProUGUI>().text = rowData == null? "조건 없음" : rowData.condition2;

            condition1Btn.onClick.RemoveAllListeners();
            condition1Btn.onClick.AddListener(() => _manager.OnConditionClicked(_charName, _rowIndex, 1));

            condition2Btn.onClick.RemoveAllListeners();
            condition2Btn.onClick.AddListener(() => _manager.OnConditionClicked(_charName, _rowIndex, 2));
        }
    }
}
