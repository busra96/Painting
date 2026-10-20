using System;
using System.Collections.Generic;
using UnityEngine;

namespace PaintingGame.Atelier
{
    // Routes the pointer to the selected tool. This is the atelier's only per-frame input loop; tools and
    // UI react to events instead of polling.
    [DefaultExecutionOrder(20)]
    public sealed class PaintToolController : MonoBehaviour
    {
        public AtelierConfig config;
        public ImpastoPaintSurface surface;
        public Camera mainCamera;
        public ColorSelection palette;
        public ToolCursorView cursor;
        public bool usePointerEvents;
        public bool externalInventoryPersistence;

        public TubeInventory Inventory { get; private set; }
        public StrokeHistory History { get; private set; }
        public PaintToolDefinition CurrentTool => tool != null ? tool.Definition : null;
        public PaintColorDefinition CurrentColor => context != null ? context.Color : null;
        public bool IsStroking { get; private set; }
        public int StrokeCount { get; private set; }
        public float SizeMultiplier => context != null ? context.SizeMultiplier : 1;
        public float Pressure => context != null ? context.Pressure : .75f;
        public void SetControls(float size, float pressure)
        {
            EndStroke();
            context.SizeMultiplier = Mathf.Clamp(size,.45f,1.6f);
            context.Pressure = Mathf.Clamp01(pressure);
            if (cursor != null) cursor.SetSizeMultiplier(context.SizeMultiplier,CurrentTool,surface);
        }
        // Blocks painting while a popup or the finish flow owns the screen.
        public bool InputLocked { get; set; }

        public event Action<PaintToolDefinition> ToolChanged;
        public event Action<PaintColorDefinition> ColorChanged;
        public event Action<PaintToolDefinition> StrokeBegan;
        public event Action<PaintToolDefinition> StrokeEnded;
        public event Action<PaintColorDefinition> TubeEmpty;

        private readonly Dictionary<PaintToolDefinition, PaintTool> tools = new Dictionary<PaintToolDefinition, PaintTool>();
        private PaintToolContext context;
        private PaintTool tool;
        private int pointerId;
        private bool touchPointer;
        private Vector3 previous;

        private void Awake()
        {
            Inventory = new TubeInventory(config.colors, externalInventoryPersistence ? null : "Atelier.Tubes");
            context = new PaintToolContext(surface, Inventory);
            History = new StrokeHistory(surface, Inventory);
        }

        private void OnEnable() { if (palette != null) palette.Changed += OnPaletteChanged; }
        private void OnDisable()
        {
            if (palette != null) palette.Changed -= OnPaletteChanged;
            EndStroke();
            Inventory?.SaveIfNeeded();
        }

        private void Start()
        {
            UpdateVisualScale();
            SelectColor(palette != null ? config.ColorFor(palette.ColorType) : config.colors.Count > 0 ? config.colors[0] : null);
            SelectTool(config.defaultTool != null ? config.defaultTool : config.tools.Count > 0 ? config.tools[0] : null);
        }

        private void OnApplicationPause(bool paused) { if (paused) { EndStroke(); Inventory.SaveIfNeeded(); } }
        private void OnApplicationFocus(bool focused) { if (!focused) EndStroke(); }
        private void OnDestroy() => History?.Clear();

        public void SelectTool(PaintToolDefinition definition)
        {
            if (definition == null || CurrentTool == definition) return;
            EndStroke();
            if (!tools.TryGetValue(definition, out PaintTool next))
            {
                next = definition.CreateTool();
                next.Bind(context);
                tools.Add(definition, next);
            }
            tool = next;
            if (cursor != null) cursor.SetTool(definition, surface);
            ToolChanged?.Invoke(definition);
        }

        public void SelectColor(PaintColorDefinition color)
        {
            if (color == null || context.Color == color) return;
            EndStroke();
            context.Color = color;
            if (cursor != null) cursor.SetPaint(color.pigment);
            if (palette != null && palette.ColorType != color.paletteSlot) palette.SelectColor(color.paletteSlot);
            ColorChanged?.Invoke(color);
        }

        public bool Undo()
        {
            EndStroke();
            return History.Undo();
        }

        public void ClearCanvas()
        {
            EndStroke();
            if (surface == null || !surface.IsReady) return;
            // ClearPaint does not journal tiles, so earlier undo steps would restore a partial canvas: drop them.
            History.Clear();
            surface.ClearPaint();
            surface.SyncAlbedo();
            StrokeCount = 0;
        }

        private void OnPaletteChanged(ColorType slot) => SelectColor(config.ColorFor(slot));

        private void Update()
        {
            if (usePointerEvents) return;
            if (surface == null || !surface.IsReady || tool == null) return;
            if (InputLocked) { EndStroke(); return; }
            if (!IsStroking)
            {
                if (Helper.PointerPressed(out int id, out Vector2 screen) && !Helper.IsOverUI(id) &&
                    ToCanvas(screen, id >= 0, out Vector3 point) && surface.Contains(point))
                    BeginStroke(id, point);
            }
            else if (!Helper.ReadPointer(pointerId, out Vector2 current, out bool released)) EndStroke();
            else
            {
                if (ToCanvas(current, touchPointer, out Vector3 here))
                {
                    tool.Drag(previous, here, Time.deltaTime);
                    previous = here;
                    if (cursor != null) cursor.Follow(here, tool.Direction, true);
                }
                if (released) EndStroke();
            }
            Inventory.FlushNotifications();
        }

        public bool BeginGesture(Vector3 point)
        {
            if (InputLocked || IsStroking || tool == null || surface == null || !surface.IsReady ||
                surface.IsSpreading || !surface.Contains(point)) return false;
            BeginStroke(-1, point);
            Inventory.FlushNotifications();
            return true;
        }

        public void ContinueGesture(Vector3 point, float seconds)
        {
            if (!IsStroking) return;
            if (InputLocked || !surface.Contains(point)) { EndStroke(); return; }
            tool.Drag(previous, point, Mathf.Clamp(seconds, 0, 0.05f));
            previous = point;
            if (cursor != null) cursor.Follow(point, tool.Direction, true);
            Inventory.FlushNotifications();
        }

        private void BeginStroke(int id, Vector3 point)
        {
            UpdateVisualScale();
            pointerId = id;
            touchPointer = id >= 0;
            previous = point;
            IsStroking = true;
            History.Record();
            context.BeginStroke();
            tool.Begin(point);
            if (cursor != null) cursor.Follow(point, Vector2.zero, true);
            StrokeBegan?.Invoke(tool.Definition);
        }

        public void EndStroke()
        {
            if (!IsStroking) return;
            IsStroking = false;
            tool.End(previous);
            StrokeCount++;
            History.Trim();
            Inventory.FlushNotifications();
            if (cursor != null) cursor.Follow(previous, tool.Direction, false);
            StrokeEnded?.Invoke(tool.Definition);
            if (tool.Definition.UsesPaint && context.OutOfPaintThisStroke) TubeEmpty?.Invoke(context.Color);
        }

        // The workshop camera can stretch the paper non-uniformly; measure it so tools look right on screen.
        private void UpdateVisualScale()
        {
            if (mainCamera == null || surface == null) return;
            Vector3 origin = surface.transform.position;
            Vector3 o = mainCamera.WorldToScreenPoint(origin);
            float px = (mainCamera.WorldToScreenPoint(origin + Vector3.right) - o).magnitude;
            float py = (mainCamera.WorldToScreenPoint(origin + Vector3.up) - o).magnitude;
            if (px <= 0.0001f || py <= 0.0001f) return;
            Vector2 scale = new Vector2(1, py / px);
            if (scale == surface.VisualScale) return;
            surface.VisualScale = scale;
            if (cursor != null) cursor.SetVisualScale(scale, CurrentTool, surface);
        }

        private bool ToCanvas(Vector2 screen, bool touch, out Vector3 point)
        {
            if (!Helper.ScreenToPaper(mainCamera, screen, surface.transform.position.z, out point)) return false;
            // Lift the contact point above the finger on touch screens so the paint stays visible.
            if (touch) point.y += config.touchLift * surface.CanvasWidth / surface.VisualScale.y;
            return true;
        }
    }
}
