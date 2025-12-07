using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Arcana.Tactics.Data;

namespace Arcana.Tactics.UI
{
    public class FormationSlotUI : MonoBehaviour, IPointerClickHandler, IDropHandler, IPointerDownHandler
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

        private TacticsUIManager _manager;
        private CharacterData _currentCharacter;
        private DraggableItem _draggable;

        public void Setup(TacticsUIManager manager, int index)
        {
            _manager = manager;
            slotIndex = index;

            _draggable = GetComponent<DraggableItem>();
            if (_draggable == null) _draggable = gameObject.AddComponent<DraggableItem>();
            _draggable.data = slotIndex;
            _draggable.dragImageSource = characterPortrait;

            Debug.Log($"[FormationSlotUI] Setup slot {index} - DraggableItem: {_draggable != null}, dragImageSource: {characterPortrait != null}");

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

            if (_draggable != null)
            {
                _draggable.isDraggable = (character != null);
                //Debug.Log($"[FormationSlotUI] Slot {slotIndex} UpdateState - Character: {character?.characterName ?? "null"}, isDraggable: {_draggable.isDraggable}, sprite: {characterPortrait.sprite != null}");
            }
        }

        public void SetActiveHighlight(bool active)
        {
            if (activeHighlight != null) activeHighlight.SetActive(active);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_manager != null)
            {
                _manager.OnFormationSlotClicked(slotIndex);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Selection is handled in OnPointerDown
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (_manager == null) return;

            if (eventData.pointerDrag == null) return;

            var draggable = eventData.pointerDrag.GetComponent<DraggableItem>();
            if (draggable != null)
            {
                if (draggable.data is CharacterData charData)
                {
                    // Character Pool -> Slot
                    _manager.OnCharacterDroppedOnSlot(charData, slotIndex);
                }
                else if (draggable.data is int sourceSlotIndex)
                {
                    // Slot -> Slot
                    _manager.OnSlotDroppedOnSlot(sourceSlotIndex, slotIndex);
                }
            }
        }
    }
}
