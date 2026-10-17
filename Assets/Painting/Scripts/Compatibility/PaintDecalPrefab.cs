using System.Collections.Generic;
using PaintCore;
using PaintIn2D;
using UnityEngine;

public class PaintDecalPrefab : MonoBehaviour
{
    public ColorType ColorType;
    public ScaleChange ScaleChange;
    public OpacityChange OpacityChange;
    public Movement movement;
    public CwHitNearby CwHitNearby;
    public CwPaintDecal2D cwPaintDecal2D;
    public Collider2D Collider;
    public Rigidbody2D Rigidbody2D;
    public PaintGrid PaintGrid;
    public int ID;
    public List<PaintDecalPrefab> TriggerPaintDecalPrefabs;
    public float distance = 1f;
    public bool OnSpatula { get; private set; }

    public void Init(CurrentImageGridProcessor gridProcessor)
    {
        if (PaintGrid != null) PaintGrid.gridProcessor = gridProcessor;
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (!OnSpatula && other.TryGetComponent(out Spatula spatula)) AttachTo(spatula);
    }

    public void AttachTo(Spatula spatula)
    {
        if (OnSpatula || spatula == null) return;
        OnSpatula = true;
        transform.SetParent(spatula.transform, true);
        distance = cwPaintDecal2D.Radius * 0.25f;
        transform.localPosition = new Vector3(transform.localPosition.x, 0.032f - distance, transform.localPosition.z);
        // Disable immediately: delayed Destroy alone allows duplicate trigger callbacks this frame.
        if (Collider != null) { Collider.enabled = false; Destroy(Collider); }
        if (Rigidbody2D != null) { Rigidbody2D.simulated = false; Destroy(Rigidbody2D); }
        movement.canMove = true;
        // Scoring reads the final painted texture instead of estimating every brush footprint.
        if (PaintGrid != null) PaintGrid.enabled = false;
        spatula.AddPaintDecalList(this);
    }
}
