using System;
using System.Collections.Generic;
using UnityEngine;

namespace PaintingGame.Atelier
{
    public sealed class AtelierCanvasPainter : MonoBehaviour
    {
        public ImpastoPaintSurface surface;
        public Camera paintCamera;
        public AtelierTool Tool { get; private set; } = AtelierTool.RoundBrush;
        public Color PaintColor { get; private set; } = AtelierRules.Colors[0];
        public float Radius { get; private set; } = 0.34f;
        public float Pressure { get; private set; } = 0.7f;
        public float UsedPaint { get; private set; }
        public int UsedTools { get; private set; }
        public int Revision { get; private set; }
        public bool AllowInput { get; set; }
        public bool CanUndo => history.Count > 0 && !holding;
        public float Remaining => Mathf.Max(0f, AtelierRules.PaintAllowance - UsedPaint);

        private sealed class Stroke
        {
            public ImpastoPaintSurface.PaintState state;
            public float used;
            public int tools;
        }
        private readonly List<Stroke> history = new List<Stroke>(8);
        private bool holding, changed, hasPrevious;
        private Vector3 previous, lastStamp;
        private bool IsStamp => Tool == AtelierTool.Flower || Tool == AtelierTool.Star;
        private bool IsMovingPaint => Tool == AtelierTool.Press || Tool == AtelierTool.Knife || Tool == AtelierTool.WideBlade;

        public void SetTool(AtelierTool tool) { EndStroke(); Tool = tool; }
        public void SetColor(Color color) { EndStroke(); PaintColor = color; }
        public void SetRadius(float radius) { EndStroke(); Radius = Mathf.Clamp(radius, 0.10f, 0.70f); }
        public void SetPressure(float pressure) { EndStroke(); Pressure = Mathf.Clamp01(pressure); }

        public void BeginStroke(Vector3 world)
        {
            EndStroke();
            if (surface == null || !surface.IsReady || !surface.Contains(world)) return;
            surface.ContinuePaintState(null);
            var state = surface.CapturePaintState();
            if (state == null) return;
            history.Add(new Stroke { state = state, used = UsedPaint, tools = UsedTools });
            holding = hasPrevious = true; changed = false;
            previous = lastStamp = world;
            if (Tool != AtelierTool.WideBlade) ApplyAt(world, Vector2.up, IsStamp ? 1.4f : 0.24f, 1f);
            TrimHistory();
        }

        public void ContinueStroke(Vector3 world, float deltaTime)
        {
            if (!holding || !surface.Contains(world)) return;
            if (!hasPrevious) { previous = world; hasPrevious = true; }
            float distance = Vector3.Distance(previous, world);
            Vector2 direction = world - previous;
            if (direction.sqrMagnitude > 0.000001f) direction.Normalize();
            if (Tool == AtelierTool.WideBlade)
            {
                if (surface.ArtistWideBlade(previous.y, world.y, Pressure))
                { changed = true; UsedTools |= 1 << (int)Tool; Revision++; }
            }
            else if (IsStamp)
            {
                if (Vector3.Distance(lastStamp, world) >= Radius * 2.8f)
                { ApplyAt(world, direction, 1.4f, 1f); lastStamp = world; }
            }
            else
            {
                int steps = Mathf.Clamp(Mathf.CeilToInt(distance / Mathf.Max(0.035f, Radius * 0.28f)), 1, 24);
                float dose = Mathf.Max(distance * (Tool == AtelierTool.Tube ? 1.9f : 1.15f), deltaTime * 1.4f);
                for (int i = 1; i <= steps; i++)
                    ApplyAt(Vector3.Lerp(previous, world, i / (float)steps), direction, dose / steps,
                        Mathf.Clamp01(deltaTime * 18f / steps + distance / steps * 1.5f));
            }
            previous = world;
        }

        private void ApplyAt(Vector3 world, Vector2 direction, float dose, float pressureStep)
        {
            bool applied;
            if (IsMovingPaint)
                applied = surface.ArtistPress(world, Radius * (Tool == AtelierTool.Knife ? 1.65f : 1.25f),
                    pressureStep * Pressure, Tool == AtelierTool.Knife ? direction : Vector2.zero);
            else
            {
                dose = Mathf.Min(Remaining, dose * Mathf.Lerp(0.6f, 1.4f, Pressure));
                ImpastoPaintSurface.ArtistBrush shape = ImpastoPaintSurface.ArtistBrush.Round;
                if (Tool == AtelierTool.FlatBrush) shape = ImpastoPaintSurface.ArtistBrush.Flat;
                if (Tool == AtelierTool.FanBrush) shape = ImpastoPaintSurface.ArtistBrush.Fan;
                if (Tool == AtelierTool.Flower) shape = ImpastoPaintSurface.ArtistBrush.Flower;
                if (Tool == AtelierTool.Star) shape = ImpastoPaintSurface.ArtistBrush.Star;
                float angle = direction.sqrMagnitude > 0.01f ? Mathf.Atan2(direction.y, direction.x) + Mathf.PI * 0.5f : 0;
                float radius = Radius * (IsStamp ? 1.7f : Tool == AtelierTool.Tube ? 0.65f : 1f);
                applied = surface.ArtistDab(world, PaintColor, dose, radius, angle, shape);
                if (applied) UsedPaint += dose;
            }
            if (!applied) return;
            changed = true; UsedTools |= 1 << (int)Tool; Revision++;
        }

        public void EndStroke()
        {
            if (!holding) return;
            holding = hasPrevious = false;
            if (!changed && history.Count > 0)
            {
                surface.ReleasePaintState(history[history.Count - 1].state);
                history.RemoveAt(history.Count - 1);
                surface.ContinuePaintState(history.Count > 0 ? history[history.Count - 1].state : null);
            }
            TrimHistory();
        }

        private void LateUpdate() => TrimHistory();

        private void TrimHistory()
        {
            long bytes = 0;
            foreach (Stroke stroke in history) bytes += stroke.state.EstimatedBytes;
            while (history.Count > 1 && (history.Count > 8 || bytes > 24L * 1024 * 1024))
            {
                bytes -= history[0].state.EstimatedBytes;
                surface.ReleasePaintState(history[0].state); history.RemoveAt(0);
            }
        }

        public void Undo()
        {
            EndStroke();
            if (!CanUndo) return;
            Stroke stroke = history[history.Count - 1];
            if (!surface.RestorePaintState(stroke.state)) return;
            UsedPaint = stroke.used; UsedTools = stroke.tools;
            surface.ReleasePaintState(stroke.state); history.RemoveAt(history.Count - 1);
            surface.ContinuePaintState(history.Count > 0 ? history[history.Count - 1].state : null);
            Revision++;
        }

        public void ClearCanvas()
        {
            EndStroke(); ForgetHistory(); surface.ClearPaint();
            UsedPaint = 0; UsedTools = 0; Revision++;
        }

        public void RestoreBudget(float used, int tools)
        { UsedPaint = Mathf.Clamp(used, 0, AtelierRules.PaintAllowance); UsedTools = tools; Revision++; }

        public void ForgetHistory()
        {
            foreach (Stroke stroke in history) if (surface != null) surface.ReleasePaintState(stroke.state);
            history.Clear();
        }

        private void OnDisable() => EndStroke();
        private void OnDestroy() => ForgetHistory();
    }
}
