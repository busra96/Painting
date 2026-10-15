using System;
using System.Collections.Generic;
using UnityEngine;

public class Test_RaycastSpawn_CameraPerspective : MonoBehaviour
{
    public CurrentImageGridProcessor gridProcessor;
    public GameObject prefabToSpawn;
    public Camera mainCamera;
    public ColorSelection ColorSelection;
    public float positionStepX = 0.1f;
    public float positionStepY = 0.1f;
    public List<Movement> paintMovements = new List<Movement>();
    public GameObject PaintDecalParent;
    public bool isPaintAmountDone;
    public static Action OnSpawnPaintDecal;

    public int PaintCount { get; private set; }
    public float DepositedUnits { get; private set; }
    private ImpastoPaintSurface surface;
    private PaintAmount budget;
    private bool isHolding;
    private int pointerId;
    private Vector3 previousPoint;
    private Color strokeColor;
    private bool hasPreviousPoint;

    private void OnEnable() => PaintAmount.OnFinishPaint += FinishPaintAmount;
    private void OnDisable()
    {
        PaintAmount.OnFinishPaint -= FinishPaintAmount;
        EndStroke();
    }

    private void Start()
    {
        surface = ImpastoPaintSurface.Active;
        budget = FindFirstObjectByType<PaintAmount>();
        // The height-field renderer generates both pigment and its shadow. Disable the old independent stamp tools.
        if (surface != null && PaintDecalParent != null) PaintDecalParent.SetActive(false);
    }

    private void OnApplicationFocus(bool focused) { if (!focused) EndStroke(); }
    private void FinishPaintAmount()
    {
        isPaintAmountDone = true;
        EndStroke();
    }

    private void Update()
    {
        if (surface == null || !surface.IsReady || surface.IsSpreading || isPaintAmountDone || ColorSelection == null) return;
        if (!isHolding && Helper.PointerPressed(out int id, out Vector2 screen) && !Helper.IsOverUI(id) &&
            Helper.ScreenToPaper(mainCamera, screen, transform.position.z, out Vector3 point) && surface.Contains(point))
        {
            pointerId = id;
            BeginStroke(point);
        }
        if (!isHolding) return;
        if (!Helper.ReadPointer(pointerId, out Vector2 currentScreen, out bool released))
        {
            EndStroke();
            return;
        }
        if (!Helper.IsOverUI(pointerId) && Helper.ScreenToPaper(mainCamera, currentScreen, transform.position.z, out Vector3 current))
        {
            if (hasPreviousPoint)
            {
                float distance = Vector3.Distance(previousPoint, current);
                float flow = surface.settings != null ? surface.settings.PourUnitsPerSecond : 7f;
                float dose = Mathf.Max(distance * UnitsPerMeter, released ? 0 : Time.deltaTime * flow);
                DepositAlong(previousPoint, current, dose);
            }
            previousPoint = current;
            hasPreviousPoint = true;
        }
        else hasPreviousPoint = false;
        if (released) EndStroke();
    }

    private float UnitsPerMeter => surface.settings != null ? surface.settings.PourUnitsPerWorldUnit : 1.8f;

    public void BeginStroke(Vector3 point)
    {
        if (surface == null) surface = ImpastoPaintSurface.Active;
        if (surface == null || !surface.IsReady || isPaintAmountDone || !surface.Contains(point)) return;
        strokeColor = ColorSelection != null ? ColorSelection.GetOriginalColor() : Color.red;
        isHolding = true;
        hasPreviousPoint = true;
        previousPoint = point;
        DepositAlong(point, point, 0.6f);
    }

    public void DrawBetween(Vector3 from, Vector3 to)
    {
        if (surface == null) surface = ImpastoPaintSurface.Active;
        if (surface == null || !surface.IsReady || isPaintAmountDone) return;
        if (!isHolding) strokeColor = ColorSelection != null ? ColorSelection.GetOriginalColor() : Color.red;
        DepositAlong(from, to, Vector3.Distance(from, to) * UnitsPerMeter);
    }

    // Holding still pours more paint; moving distributes the same stream along a continuous unsnapped path.
    public void PourAt(Vector3 point, float units)
    {
        if (surface == null) surface = ImpastoPaintSurface.Active;
        if (surface == null || !surface.IsReady || isPaintAmountDone) return;
        if (!isHolding) strokeColor = ColorSelection != null ? ColorSelection.GetOriginalColor() : Color.red;
        DepositAlong(point, point, units);
    }

    private void DepositAlong(Vector3 from, Vector3 to, float units)
    {
        if (units <= 0 || surface.IsSpreading) return;
        if (budget == null) budget = FindFirstObjectByType<PaintAmount>();
        float distance = Vector3.Distance(from, to);
        int steps = Mathf.Clamp(Mathf.CeilToInt(distance / (surface.BrushRadius * 0.3f)), 1, 1024);
        Vector2 direction = (to - from).normalized;
        for (int i = 1; i <= steps && !isPaintAmountDone; i++)
        {
            Vector3 point = Vector3.Lerp(from, to, i / (float)steps);
            if (!surface.Contains(point)) continue;
            float granted = budget != null ? budget.Spend(units / steps) : units / steps;
            if (granted <= 0) break;
            surface.Deposit(point, strokeColor, granted, direction);
            DepositedUnits += granted;
            PaintCount++;
        }
    }

    public void EndStroke()
    {
        isHolding = false;
        hasPreviousPoint = false;
        paintMovements.Clear();
    }
}
