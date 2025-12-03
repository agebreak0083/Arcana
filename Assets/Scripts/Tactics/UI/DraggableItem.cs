using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arcana.Tactics.UI
{
    public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public object data; // CharacterData or int (slotIndex)
        public Image dragImageSource; // Image to show while dragging
        public bool isDraggable = true;

        private GameObject _dragObject;
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isDraggable || data == null) return;

            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
                // Find root canvas to ensure drag object is on top
                Canvas root = _canvas;
                while (root != null && root.transform.parent != null)
                {
                    var parentCanvas = root.transform.parent.GetComponent<Canvas>();
                    if (parentCanvas != null) root = parentCanvas;
                    else break;
                }
                if (root != null) _canvas = root;
            }

            // Create a drag visual
            _dragObject = new GameObject("DragObject");
            _dragObject.transform.SetParent(_canvas.transform, false);
            _dragObject.transform.SetAsLastSibling();

            var image = _dragObject.AddComponent<Image>();
            if (dragImageSource != null)
            {
                image.sprite = dragImageSource.sprite;
                image.color = dragImageSource.color;
                // Ensure the sprite is visible even if source is disabled (though source should be enabled if draggable)
                image.preserveAspect = dragImageSource.preserveAspect;
            }
            image.raycastTarget = false;

            // Match size
            var rt = _dragObject.GetComponent<RectTransform>();
            var sourceRt = dragImageSource != null ? dragImageSource.rectTransform : GetComponent<RectTransform>();
            rt.sizeDelta = sourceRt.sizeDelta;

            // Position at mouse
            _dragObject.transform.position = eventData.position;

            _canvasGroup.alpha = 0.6f;
            _canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragObject != null)
            {
                _dragObject.transform.position = eventData.position;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_dragObject != null) Destroy(_dragObject);
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }
    }
}
