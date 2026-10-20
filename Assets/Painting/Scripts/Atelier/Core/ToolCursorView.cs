using UnityEngine;

namespace PaintingGame.Atelier
{
    // The tool drawn on the canvas under/above the finger. Runs only while visible (disabled otherwise).
    public sealed class ToolCursorView : MonoBehaviour
    {
        public SpriteRenderer body;
        public SpriteRenderer tip;
        [Min(1)] public float followSharpness = 28;
        [Min(1)] public float turnSharpness = 14;
        [Range(0, 40)] public float maxTilt = 14;

        private Vector3 target;
        private Vector2 direction;
        private bool pressed, follows;
        private float baseScale = 1, pressScale = 1, angle;
        private float hideAt = -1;
        private float sizeMultiplier = 1;
        public void SetSizeMultiplier(float size,PaintToolDefinition tool,ImpastoPaintSurface surface)
        { sizeMultiplier=size; if(tool!=null)SetTool(tool,surface); }

        private void Awake() { SetVisible(false); enabled = false; }

        // The cursor lives under a parent that cancels the camera's non-uniform stretch.
        public void SetVisualScale(Vector2 visualScale, PaintToolDefinition tool, ImpastoPaintSurface surface)
        {
            if (transform.parent != null) transform.parent.localScale = new Vector3(1, 1 / Mathf.Max(0.0001f, visualScale.y), 1);
            if (tool != null) SetTool(tool, surface);
        }

        public void SetTool(PaintToolDefinition tool, ImpastoPaintSurface surface)
        {
            if (body != null) body.sprite = tool.cursor;
            if (tip != null) { tip.sprite = tool.cursorTip; tip.enabled = tool.cursorTip != null; }
            follows = tool.cursorFollowsStroke;
            float worldSize = tool.WorldSize(surface) * 2 * tool.cursorScale * sizeMultiplier;
            float spriteSize = tool.cursor != null ? Mathf.Max(0.0001f, tool.cursor.bounds.size.x) : 1;
            // Parent x scale is 1, so local x size equals visual canvas units.
            baseScale = worldSize / spriteSize;
            transform.localScale = Vector3.one * baseScale;
        }

        public void HideNow()
        {
            hideAt = -1;
            SetVisible(false);
            enabled = false;
        }

        public void SetPaint(Color pigment)
        {
            if (tip != null) tip.color = pigment;
        }

        public void Follow(Vector3 point, Vector2 travel, bool down)
        {
            point.z = transform.position.z;
            if (transform.parent != null) point = transform.parent.InverseTransformPoint(point);
            point.z = transform.localPosition.z;
            if (down && !pressed) { transform.localPosition = point; pressScale = 0.82f; }
            pressed = down;
            target = point;
            if (travel.sqrMagnitude > 0.0001f) direction = travel;
            hideAt = down ? -1 : Time.time + 0.25f;
            SetVisible(true);
            enabled = true;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            transform.localPosition = Vector3.Lerp(transform.localPosition, target, 1 - Mathf.Exp(-followSharpness * dt));
            if (follows && direction != Vector2.zero)
            {
                float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90;
                angle = Mathf.LerpAngle(angle, targetAngle, 1 - Mathf.Exp(-turnSharpness * dt));
            }
            else angle = Mathf.LerpAngle(angle, maxTilt * (pressed ? 1 : 0.4f), 1 - Mathf.Exp(-turnSharpness * dt));
            transform.localRotation = Quaternion.Euler(0, 0, angle);
            // Squash on touch-down, spring back while working; shrink away after lifting.
            float goal = pressed ? 1 : 0.6f;
            pressScale = Mathf.Lerp(pressScale, goal, 1 - Mathf.Exp(-18 * dt));
            transform.localScale = Vector3.one * baseScale * pressScale;
            if (hideAt > 0 && Time.time >= hideAt)
            {
                SetVisible(false);
                enabled = false;
            }
        }

        private void SetVisible(bool visible)
        {
            if (body != null) body.enabled = visible;
            if (tip != null) tip.enabled = visible && tip.sprite != null;
        }
    }
}
