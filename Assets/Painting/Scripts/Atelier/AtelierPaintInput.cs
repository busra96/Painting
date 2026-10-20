using UnityEngine;
using UnityEngine.EventSystems;

namespace PaintingGame.Atelier
{
    // A gesture belongs to the paint area's Graphic, just as button gestures belong to the UI.
    [RequireComponent(typeof(RectTransform))]
    public sealed class AtelierPaintInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public AtelierShopController owner;
        private RectTransform area;
        private bool pressed;
        private int pointerId;
        private Vector2 screenPosition;
        private Camera eventCamera;

        private void Awake() => area = (RectTransform)transform;
        public void OnInitializePotentialDrag(PointerEventData data) => data.useDragThreshold = false;

        public void OnPointerDown(PointerEventData data)
        {
            if (pressed || data.button != PointerEventData.InputButton.Left || owner == null ||
                !TryPoint(data.position, data.pressEventCamera, out Vector3 point) || !owner.TryBeginPainting()) return;
            pointerId = data.pointerId; screenPosition = data.position; eventCamera = data.pressEventCamera;
            pressed = true;
            owner.painter.BeginStroke(point);
        }

        public void OnBeginDrag(PointerEventData data) { }
        public void OnDrag(PointerEventData data)
        {
            if (!pressed || data.pointerId != pointerId) return;
            screenPosition = data.position;
            if (!TryPoint(screenPosition, eventCamera, out _)) EndGesture();
        }

        private void Update()
        {
            if (!pressed) return;
            if (owner == null || !owner.CanPaint || !TryPoint(screenPosition, eventCamera, out Vector3 point))
            { EndGesture(); return; }
            owner.painter.ContinueStroke(point, Mathf.Min(Time.unscaledDeltaTime, 0.05f));
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (!pressed || data.pointerId != pointerId) return;
            // Include short drags that start and finish between two Update calls.
            if (TryPoint(data.position, eventCamera, out Vector3 point)) owner.painter.ContinueStroke(point, 0f);
            EndGesture();
        }

        public void OnEndDrag(PointerEventData data)
        { if (pressed && data.pointerId == pointerId) EndGesture(); }

        private bool TryPoint(Vector2 screen, Camera uiCamera, out Vector3 world)
        {
            world = default;
            if (owner == null || !owner.Ready) return false;
            if (area == null) area = (RectTransform)transform;
            return RectTransformUtility.RectangleContainsScreenPoint(area, screen, uiCamera) &&
                Helper.ScreenToPaper(owner.painter.paintCamera, screen, owner.paper.transform.position.z, out world) &&
                owner.painter.surface.Contains(world);
        }

        private void EndGesture()
        {
            pressed = false;
            if (owner != null && owner.painter != null) owner.painter.EndStroke();
        }

        private void OnDisable() => EndGesture();
        private void OnApplicationFocus(bool focused) { if (!focused) EndGesture(); }
    }
}
