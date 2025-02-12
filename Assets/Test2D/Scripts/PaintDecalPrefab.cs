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
    private bool onSpatula;

    public List<PaintDecalPrefab> TriggerPaintDecalPrefabs;

    public float distance = 1f;
    private bool distanceCalculate;
    private Vector2 startPos;

    public void Init(CurrentImageGridProcessor gridProcessor)
    {
        PaintGrid.gridProcessor = gridProcessor;
    }
    
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out Spatula spatula))
        {
            transform.parent = spatula.transform;
            spatula.AddPaintDecalList(this);
          
            transform.localPosition = new Vector3(transform.localPosition.x, 0.032f, transform.localPosition.z);
            //transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y - distance, transform.localPosition.z);
            //startPos = transform.position;
         
            //transform.localPosition =  SnapToGrid(transform.localPosition);
            Destroy(Collider);
            Destroy(Rigidbody2D);
            movement.canMove = true;
            distance = cwPaintDecal2D.Radius * .25f;
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y - distance, transform.localPosition.z);
            CwHitNearby.enabled = true;
            onSpatula = true;
            distanceCalculate = true;
            PaintGrid.Activate();
        }
    }
}