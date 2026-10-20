using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PaintingGame.Atelier
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class GalleryPaintInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public GalleryGameController game;
        private bool pressed;
        private int pointer;
        private Vector2 screen;
        private Camera eventCamera;
        private RectTransform area;
        public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold = false;
        public void OnBeginDrag(PointerEventData e) { }
        public void OnPointerDown(PointerEventData e)
        {
            if (pressed || e.button != PointerEventData.InputButton.Left || game == null || !game.Ready) return;
            eventCamera = e.pressEventCamera;
            if (!Point(e.position, out var world) || !game.PrepareCanvas()) return;
            pointer = e.pointerId; screen = e.position;
            pressed = game.tools.BeginGesture(world);
            if (pressed) StartCoroutine(PaintHeld());
        }
        public void OnDrag(PointerEventData e)
        {
            if (!pressed || e.pointerId != pointer) return;
            screen = e.position;
            if (!Point(screen, out _)) End();
        }
        public void OnPointerUp(PointerEventData e)
        {
            if (!pressed || e.pointerId != pointer) return;
            if (Point(e.position, out var world)) game.tools.ContinueGesture(world, 0);
            End();
        }
        public void OnEndDrag(PointerEventData e) { if (e.pointerId == pointer) End(); }
        private bool Point(Vector2 position, out Vector3 world)
        {
            if (area == null) area = (RectTransform)transform;
            world = default;
            return RectTransformUtility.RectangleContainsScreenPoint(area, position, eventCamera) &&
                Helper.ScreenToPaper(game.tools.mainCamera, position, game.paper.transform.position.z, out world) && game.tools.surface.Contains(world);
        }
        private IEnumerator PaintHeld()
        {
            while (pressed)
            {
                if (!game.CanPaint || !Point(screen, out var world)) { End(); yield break; }
                game.tools.ContinueGesture(world, Time.unscaledDeltaTime);
                yield return null;
            }
        }
        private void End() { pressed = false; StopAllCoroutines(); if (game != null && game.tools != null) game.tools.EndStroke(); }
        private void OnDisable() { if (pressed) End(); }
        private void OnApplicationFocus(bool focused) { if (!focused) End(); }
    }
}
