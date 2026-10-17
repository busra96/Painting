using System.Collections.Generic;
using UnityEngine;

public class Spatula : MonoBehaviour
{
    public List<PaintDecalPrefab> PaintDecalPrefabs = new List<PaintDecalPrefab>();
    private const float ColumnTolerance = 0.01f;
    private float lastY;
    private readonly List<PaintDecalPrefab> activeLayers = new List<PaintDecalPrefab>();
    private ImpastoPaintSurface surface;

    private void OnEnable()
    {
        lastY = transform.position.y;
        surface = ImpastoPaintSurface.Active;
        if (surface != null && surface.IsReady && Mathf.Abs(transform.position.x - surface.transform.position.x) < 5)
        {
            surface.StyleSpatula(this);
            surface.BeginSpread(transform.position.y + 0.05f);
        }
        else surface = null;
        Movement.OnPaintDecalPrefabOnDestroy += RemovePaintDecalList;
    }

    private void OnDisable() => Movement.OnPaintDecalPrefabOnDestroy -= RemovePaintDecalList;

    private void FixedUpdate()
    {
        if (surface != null) return;
        float travel = Mathf.Max(0, lastY - transform.position.y);
        lastY = transform.position.y;
        if (travel <= 0) return;
        activeLayers.Clear();
        for (int i = PaintDecalPrefabs.Count - 1; i >= 0; i--)
        {
            PaintDecalPrefab decal = PaintDecalPrefabs[i];
            if (decal == null) { PaintDecalPrefabs.RemoveAt(i); continue; }
            if (decal.gameObject.activeSelf) activeLayers.Add(decal);
        }
        // Newly revealed colors start on the next step; they cannot consume the same travel twice.
        foreach (PaintDecalPrefab decal in activeLayers)
        {
            decal.movement.Consume(travel);
            if (!decal.movement.IsExhausted) continue;
            RemovePaintDecalList(decal);
            Destroy(decal.gameObject);
        }
    }

    private void LateUpdate()
    {
        if (surface == null) return;
        float travel = Mathf.Max(0, lastY - transform.position.y);
        surface.SpreadTo(transform.position.y + 0.05f, travel / Mathf.Max(Time.deltaTime, 0.001f));
        lastY = transform.position.y;
    }

    public void AddPaintDecalList(PaintDecalPrefab decal)
    {
        if (decal == null || decal.movement == null || PaintDecalPrefabs.Contains(decal)) return;
        PaintDecalPrefab previous = GetHighestPriorityDecal(decal);
        if (previous != null && previous.ColorType == decal.ColorType)
        {
            decal.movement.AddSupply(previous.movement.RemainingDistance);
            PaintDecalPrefabs.Remove(previous);
            previous.movement.DisablePaint();
            previous.gameObject.SetActive(false);
            Destroy(previous.gameObject);
        }
        PaintDecalPrefabs.Add(decal);
        RefreshColumn(decal.transform.position.x);
    }

    public PaintDecalPrefab GetHighestPriorityDecal(PaintDecalPrefab reference)
    {
        if (reference == null) return null;
        PaintDecalPrefab highest = null;
        foreach (PaintDecalPrefab decal in PaintDecalPrefabs)
        {
            if (decal == null || decal.CwHitNearby == null || decal.movement == null || decal.movement.IsExhausted ||
                Mathf.Abs(decal.transform.position.x - reference.transform.position.x) > ColumnTolerance) continue;
            if (highest == null || decal.CwHitNearby.Priority >= highest.CwHitNearby.Priority) highest = decal;
        }
        return highest;
    }

    private void RefreshColumn(float x)
    {
        PaintDecalPrefab highest = null;
        foreach (PaintDecalPrefab decal in PaintDecalPrefabs)
        {
            if (decal == null || Mathf.Abs(decal.transform.position.x - x) > ColumnTolerance ||
                decal.movement.IsExhausted) continue;
            if (highest == null || decal.CwHitNearby.Priority >= highest.CwHitNearby.Priority) highest = decal;
        }
        foreach (PaintDecalPrefab decal in PaintDecalPrefabs)
        {
            if (decal == null || Mathf.Abs(decal.transform.position.x - x) > ColumnTolerance) continue;
            bool active = decal == highest;
            decal.CwHitNearby.enabled = active;
            decal.gameObject.SetActive(active);
        }
    }

    public void RemovePaintDecalList(PaintDecalPrefab decal)
    {
        if (decal == null || !PaintDecalPrefabs.Remove(decal)) return;
        decal.movement.DisablePaint();
        decal.gameObject.SetActive(false);
        RefreshColumn(decal.transform.position.x);
    }
}

public enum ColorType { Red, Pink, Orange, White, Blue, Green, Yellow }
