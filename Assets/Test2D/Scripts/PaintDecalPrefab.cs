using System;
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
    
    public int ID;
    private bool onSpatula;

    public List<PaintDecalPrefab> TriggerPaintDecalPrefabs;

    public float distance = 1f;
    private bool distanceCalculate;
    private Vector2 startPos;
    
    public void OnTriggerEnter2D(Collider2D other)
    {
       /* if (other.TryGetComponent(out Movement paintMovement))
        {
            if(onSpatula) return;
            if (paintMovement.paintDecalPrefab.ID == ID) return;
            
            if(TriggerPaintDecalPrefabs.Contains(paintMovement.paintDecalPrefab)) return;
        
            if (paintMovement.transform.position.y > transform.position.y)
            {
                TriggerPaintDecalPrefabs.Add(paintMovement.paintDecalPrefab);
                transform.position = new Vector3( paintMovement.transform.position.x, transform.position.y, transform.position.z);
                
               // int priority = paintMovement.paintDecalPrefab.CwHitNearby.Priority;
               // CwHitNearby.Priority = priority + 1;
               // CalculateNewRadiusAndPriority();
                //paintMovement.paintDecalPrefab.CalculateNewRadiusAndPriority();
        
            }
        }*/
        
        if (other.TryGetComponent(out Spatula spatula))
        {
            transform.parent = spatula.transform;
            spatula.AddPaintDecalList(this);
          
            transform.localPosition = new Vector3(transform.localPosition.x, 0.032f, transform.localPosition.z);
            //transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y - distance, transform.localPosition.z);
          //  startPos = transform.position;
         
            //transform.localPosition =  SnapToGrid(transform.localPosition);
            Destroy(Collider);
            Destroy(Rigidbody2D);
            movement.canMove = true;
            distance = cwPaintDecal2D.Radius * .25f;
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y - distance, transform.localPosition.z);
            CwHitNearby.enabled = true;
            onSpatula = true;
            distanceCalculate = true;
        }
    }

   /*  private void Update()
    {
        if (!distanceCalculate) return;

        float distY = Mathf.Abs(startPos.y - transform.position.y); // Y eksenindeki mesafeyi hesapla
        if (distY >= distance)
        {
            distanceCalculate = false;
            CwHitNearby.enabled = true;
        }
    }*/

    private Vector3 SnapToGrid(Vector3 position)
    {
        float snappedX = Mathf.Round(position.x / .25f) * .25f;
        return new Vector3(snappedX, position.y, position.z);
    }
    
    public void CalculateNewRadiusAndPriority()
    {
        cwPaintDecal2D.Radius = .5f + (CwHitNearby.Priority * .1f);
    }
}