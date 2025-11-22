using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Arcana.Tactics.Data;

namespace Arcana.Tactics.UI
{
    public class FormationSlotUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("Configuration")]
        public int slotIndex; // 0-5

        [Header("UI References")]
        public GameObject emptyStateObject;
        public GameObject filledStateObject;
        public TextMeshProUGUI slotLabel; // "Front 1", etc.
        
        [Header("Filled State UI")]
        public Image characterPortrait;
        public TextMeshProUGUI charNameText;
        public TextMeshProUGUI charCostText;
        public GameObject activeHighlight; // Shows when this slot is currently being edited/selected

        private TacticsManager _manager;
        private CharacterData _currentCharacter;

        public void Setup(TacticsManager manager, int index)
        {
            _manager = manager;
            slotIndex = index;
            UpdateState(null);
        }

        public void UpdateState(CharacterData character)
        {
            _currentCharacter = character;

            if (character == null)
            {
                emptyStateObject.SetActive(true);
                filledStateObject.SetActive(false);
                string pos = slotIndex < 3 ? "Front" : "Back";
                int num = (slotIndex % 3) + 1;
                slotLabel.text = $"{pos} {num}";
            }
            else
            {
                emptyStateObject.SetActive(false);
                filledStateObject.SetActive(true);
                
                if (character.portrait != null) characterPortrait.sprite = character.portrait;
                charNameText.text = character.characterName.Split(' ')[0];
                charCostText.text = $"{character.cost}C";
            }
        }

        public void SetActiveHighlight(bool active)
        {
            if (activeHighlight != null) activeHighlight.SetActive(active);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_manager != null)
            {
                _manager.OnFormationSlotClicked(slotIndex);
            }
        }
    }
}
