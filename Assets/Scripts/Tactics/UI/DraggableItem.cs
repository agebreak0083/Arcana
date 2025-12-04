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
        private RectTransform _dragRectTransform;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            Debug.Log($"[DraggableItem] Awake - Canvas found: {_canvas != null}, CanvasGroup: {_canvasGroup != null}");
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Debug.Log($"[DraggableItem] OnBeginDrag called - isDraggable: {isDraggable}, data: {data != null}, dragImageSource: {dragImageSource != null}");

            if (!isDraggable)
            {
                Debug.LogWarning("[DraggableItem] Not draggable!");
                return;
            }

            if (data == null)
            {
                Debug.LogWarning("[DraggableItem] Data is null!");
                return;
            }

            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
                Debug.Log($"[DraggableItem] Canvas was null, searching... Found: {_canvas != null}");

                // Find root canvas to ensure drag object is on top
                Canvas root = _canvas;
                while (root != null && root.transform.parent != null)
                {
                    var parentCanvas = root.transform.parent.GetComponent<Canvas>();
                    if (parentCanvas != null) root = parentCanvas;
                    else break;
                }
                if (root != null) _canvas = root;

                Debug.Log($"[DraggableItem] Root canvas: {_canvas != null}");
            }

            if (_canvas == null)
            {
                Debug.LogError("[DraggableItem] Canvas not found!");
                return;
            }

            // Create a drag visual
            _dragObject = new GameObject("DragObject");
            _dragObject.transform.SetParent(_canvas.transform, false);
            _dragObject.transform.SetAsLastSibling();

            Debug.Log($"[DraggableItem] Created drag object, parent: {_dragObject.transform.parent.name}");

            var image = _dragObject.AddComponent<Image>();
            if (dragImageSource != null)
            {
                image.sprite = dragImageSource.sprite;
                image.color = dragImageSource.color;
                image.preserveAspect = dragImageSource.preserveAspect;
                Debug.Log($"[DraggableItem] Drag image sprite: {image.sprite != null}, color: {image.color}");
            }
            else
            {
                Debug.LogWarning("[DraggableItem] dragImageSource is null!");
            }
            image.raycastTarget = false;

            // Match size - use rect.size instead of sizeDelta for layout-controlled elements
            _dragRectTransform = _dragObject.GetComponent<RectTransform>();
            var sourceRt = dragImageSource != null ? dragImageSource.rectTransform : GetComponent<RectTransform>();

            // Use rect.size which gives the actual rendered size, not sizeDelta which can be 0 for layout-controlled elements
            Vector2 actualSize = sourceRt.rect.size;
            _dragRectTransform.sizeDelta = actualSize;

            Debug.Log($"[DraggableItem] Source rect size: {actualSize}, sizeDelta was: {sourceRt.sizeDelta}");

            // Position at mouse using proper coordinate conversion
            UpdateDragPosition(eventData);

            _canvasGroup.alpha = 0.6f;
            _canvasGroup.blocksRaycasts = false;

            Debug.Log($"[DraggableItem] OnBeginDrag completed, drag object position: {_dragRectTransform.localPosition}");
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragObject != null)
            {
                UpdateDragPosition(eventData);
            }
            else
            {
                Debug.LogWarning("[DraggableItem] OnDrag called but _dragObject is null!");
            }
        }

        private void UpdateDragPosition(PointerEventData eventData)
        {
            if (_dragObject == null)
            {
                Debug.LogWarning("[DraggableItem] UpdateDragPosition: _dragObject is null");
                return;
            }

            if (_canvas == null)
            {
                Debug.LogWarning("[DraggableItem] UpdateDragPosition: _canvas is null");
                return;
            }

            Vector2 localPoint;
            RectTransform canvasRect = _canvas.GetComponent<RectTransform>();

            if (canvasRect == null)
            {
                Debug.LogError("[DraggableItem] Canvas has no RectTransform!");
                return;
            }

            // Convert screen point to local point in canvas
            bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint);

            if (success)
            {
                if (_dragRectTransform == null)
                    _dragRectTransform = _dragObject.GetComponent<RectTransform>();

                _dragRectTransform.localPosition = localPoint;

                // Log every 10 frames to avoid spam
                if (Time.frameCount % 10 == 0)
                {
                    Debug.Log($"[DraggableItem] Drag position updated - Screen: {eventData.position}, Local: {localPoint}, Camera: {eventData.pressEventCamera}");
                }
            }
            else
            {
                Debug.LogWarning($"[DraggableItem] ScreenPointToLocalPointInRectangle failed! Screen pos: {eventData.position}");
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Debug.Log("[DraggableItem] OnEndDrag called");

            if (_dragObject != null)
            {
                Destroy(_dragObject);
                _dragObject = null;
                _dragRectTransform = null;
            }

            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }
    }
}
