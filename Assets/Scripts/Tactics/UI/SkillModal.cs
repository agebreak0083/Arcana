using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Arcana.Tactics.Data;

namespace Arcana.Tactics.UI
{
    public class SkillModal : MonoBehaviour
    {
        [Header("UI References")]
        public Button closeBtn;
        public GameObject SkillBtnPrefab;
        public Transform skillButtonContainer; // 스킬 버튼들이 생성될 컨테이너

        private TacticsUIManager _manager;
        private CharacterData _currentCharacter;
        private System.Action<Skill> _onSkillSelected;

        private void Awake()
        {

        }

        public void Setup(TacticsUIManager manager)
        {
            _manager = manager;

            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(Close);
            }

            Close();
        }

        /// <summary>
        /// 스킬 모달 열기
        /// </summary>
        /// <param name="character">스킬을 선택할 캐릭터</param>
        /// <param name="onSkillSelected">스킬 선택 시 호출될 콜백</param>
        public void Open(CharacterData character, System.Action<Skill> onSkillSelected)
        {
            _currentCharacter = character;
            _onSkillSelected = onSkillSelected;

            CreateSkillButtons();
            gameObject.SetActive(true);
        }

        public void Open()
        {
            // 기본 Open (호환성 유지)
            if (_currentCharacter != null)
            {
                CreateSkillButtons();
            }
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 스킬 버튼 생성
        /// </summary>
        private void CreateSkillButtons()
        {
            // 기존 버튼들 제거
            if (skillButtonContainer != null)
            {
                foreach (Transform child in skillButtonContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            if (_currentCharacter == null)
            {
                Debug.LogWarning("SkillModal: No character selected!");
                return;
            }

            if (SkillBtnPrefab == null)
            {
                Debug.LogError("SkillModal: SkillBtnPrefab is not assigned!");
                return;
            }

            if (skillButtonContainer == null)
            {
                Debug.LogError("SkillModal: skillButtonContainer is not assigned!");
                return;
            }

            // 캐릭터의 스킬 목록 가져오기
            if (_currentCharacter.skills == null || _currentCharacter.skills.Count == 0)
            {
                Debug.LogWarning($"SkillModal: Character {_currentCharacter.characterName} has no skills!");
                return;
            }

            // 각 스킬에 대해 버튼 생성
            foreach (var skill in _currentCharacter.skills)
            {
                CreateSkillButton(skill);
            }

            Debug.Log($"SkillModal: Created {_currentCharacter.skills.Count} skill buttons for {_currentCharacter.characterName}");
        }

        /// <summary>
        /// 개별 스킬 버튼 생성
        /// </summary>
        private void CreateSkillButton(Skill skill)
        {
            GameObject btnObj = Instantiate(SkillBtnPrefab, skillButtonContainer);

            // 버튼 컴포넌트 가져오기
            Button btn = btnObj.GetComponent<Button>();
            if (btn == null)
            {
                btn = btnObj.AddComponent<Button>();
            }

            // 자식 텍스트 컴포넌트 찾기
            TextMeshProUGUI skillNameText = null;
            TextMeshProUGUI skillDescText = null;
            TextMeshProUGUI skillDamageText = null;

            // "SkillNameText"와 "SkillDescText" 찾기
            foreach (Transform child in btnObj.transform)
            {
                if (child.name == "SkillNameText")
                {
                    skillNameText = child.GetComponent<TextMeshProUGUI>();
                }
                else if (child.name == "SkillDescText")
                {
                    skillDescText = child.GetComponent<TextMeshProUGUI>();
                }
                else if (child.name == "SkillDamageText")
                {                    
                    skillDamageText = child.GetComponent<TextMeshProUGUI>();
                }
            }

            // 텍스트 설정
            if (skillNameText != null)
            {
                skillNameText.text = skill.name;

                // 코스트 정보 표시. AP/PP가 0이면 표시하지 않음
                if (skill.costAP > 0)
                {
                    skillNameText.text += $" ({skill.costAP} AP)";
                }
                if (skill.costPP > 0)
                {
                    skillNameText.text += $" ({skill.costPP} PP)";
                }
            }

            if (skillDescText != null)
            {
                // 스킬 설명 또는 코스트 정보 표시
                string desc = skill.description ?? skill.type;
                skillDescText.text = desc;
            }

            if (skillDamageText != null)
            {
                skillDamageText.text = "대미지 " +  skill.effects.Find(e => e.type == "damage")?.value.ToString() ?? "0";                
            }

            // 버튼 클릭 이벤트 등록
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnSkillButtonClicked(skill));
        }

        /// <summary>
        /// 스킬 버튼 클릭 시 호출
        /// </summary>
        private void OnSkillButtonClicked(Skill skill)
        {
            Debug.Log($"SkillModal: Skill selected - {skill.name}");

            // 콜백 호출
            _onSkillSelected?.Invoke(skill);

            // 모달 닫기
            Close();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
