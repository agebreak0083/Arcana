using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arcana.Tactics.UI
{
    public class CharacterPoolPanel : MonoBehaviour, IDropHandler
    {
        private TacticsUIManager _manager;

        Scrollbar scrollbar;
        ScrollRect scrollRect;

        private void Awake()
        {
            var img = GetComponent<Image>();
            if (img != null && img.sprite == null)
            {
                img.raycastTarget = true; // Ensure it receives raycasts
            }

            scrollbar = GetComponentInChildren<Scrollbar>();
            scrollRect = GetComponentInChildren<ScrollRect>();

            // ScrollBar를 ScrollRect에 연결
            if (scrollRect != null && scrollbar != null)
            {
                // 가로 스크롤이므로 horizontalScrollbar에 할당
                scrollRect.horizontalScrollbar = scrollbar;
                scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            }
            else
            {
                if (scrollRect == null)
                    Debug.LogWarning("CharacterPoolPanel: ScrollRect not found!");
                if (scrollbar == null)
                    Debug.LogWarning("CharacterPoolPanel: Scrollbar not found!");
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
