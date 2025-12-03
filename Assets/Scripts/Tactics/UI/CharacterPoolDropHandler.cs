using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arcana.Tactics.UI
{
    [RequireComponent(typeof(Image))]
    public class CharacterPoolDropHandler : MonoBehaviour, IDropHandler
    {
        private TacticsUIManager _manager;

        private void Awake()
        {
            var img = GetComponent<Image>();
            if (img.sprite == null)
            {
                img.color = new Color(0, 0, 0, 0); // Transparent
            }
        }

        public void Setup(TacticsUIManager manager)
        {
            _manager = manager;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (_manager == null) return;

            var draggable = eventData.pointerDrag.GetComponent<DraggableItem>();
            if (draggable != null && draggable.data is int sourceSlotIndex)
            {
                // Slot -> Pool : Remove from slot
                _manager.OnSlotDroppedOnPool(sourceSlotIndex);
            }
        }
    }
}
