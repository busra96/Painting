using System;
using UnityEngine;

public class Movement : MonoBehaviour
{
    public PaintDecalPrefab paintDecalPrefab;
    public bool canMove;
    // Retained for existing prefab serialization.
    [SerializeField] private float movementSpeed = 1.5f;
    [SerializeField] private float distanceMultiplier;
    [SerializeField] private GameObject debugGo;
    public float secondMultiplayer;
    public float secondMultiplayer2;
    public static Action<PaintDecalPrefab> OnPaintDecalPrefabOnDestroy;

    public float RemainingDistance { get; private set; }
    public bool IsExhausted => RemainingDistance <= 0.0001f;
    private float totalDistance;
    private float initialOpacity;
    private Vector3 initialScale;
    private bool initialized;

    private SettingsContain Settings => paintDecalPrefab != null && paintDecalPrefab.OpacityChange != null
        ? paintDecalPrefab.OpacityChange.SettingsContain : null;

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;
        initialOpacity = paintDecalPrefab.cwPaintDecal2D.Opacity;
        initialScale = paintDecalPrefab.cwPaintDecal2D.Scale;
    }

    public void CalculateMaxDistance(float amount)
    {
        Initialize();
        float perDab = Settings != null ? Settings.PaintDistancePerDab : 2.8f;
        float maximum = Settings != null ? Settings.MaxPaintDistance : 14f;
        RemainingDistance = Mathf.Clamp(Mathf.Max(1f, amount) * perDab, 0.1f, Mathf.Max(0.1f, maximum));
        totalDistance = RemainingDistance;
        ApplyAppearance();
    }

    public void AddSupply(float distance)
    {
        Initialize();
        float maximum = Settings != null ? Settings.MaxPaintDistance : 14f;
        RemainingDistance = Mathf.Min(RemainingDistance + Mathf.Max(0, distance), maximum);
        totalDistance = RemainingDistance;
        ApplyAppearance();
    }

    public void Consume(float distance)
    {
        if (!canMove || distance <= 0) return;
        RemainingDistance = Mathf.Max(0, RemainingDistance - distance);
        ApplyAppearance();
    }

    private void ApplyAppearance()
    {
        float fadeFraction = Settings != null ? Settings.PaintFadeFraction : 0.25f;
        float fullness = Mathf.Clamp01(RemainingDistance / Mathf.Max(0.001f, totalDistance * fadeFraction));
        float fade = Mathf.SmoothStep(0, 1, fullness);
        paintDecalPrefab.cwPaintDecal2D.Opacity = initialOpacity * fade;
        float spread = Settings != null ? Settings.PaintSpreadMultiplier : 1.15f;
        paintDecalPrefab.cwPaintDecal2D.Scale = new Vector3(initialScale.x * Mathf.Lerp(0.65f, spread, fade),
            initialScale.y, initialScale.z);
    }

    public void DisablePaint()
    {
        canMove = false;
        if (paintDecalPrefab.CwHitNearby != null) paintDecalPrefab.CwHitNearby.enabled = false;
    }
}
