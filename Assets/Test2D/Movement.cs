using System;
using UnityEngine;

public class Movement : MonoBehaviour
{
   public PaintDecalPrefab paintDecalPrefab;
   public bool canMove;

   [SerializeField] private float movementSpeed = 1.5f;
   
   [SerializeField] private float distanceMultiplier;
   [SerializeField] private GameObject debugGo;
   
   private Vector3 startPos;
   private Vector3 endPos;
   private bool endPosCalculated;

   public static Action<PaintDecalPrefab> OnPaintDecalPrefabOnDestroy;
   public float secondMultiplayer;
   public float secondMultiplayer2;
   
   private void Start()
   {
       startPos = transform.position;

       paintDecalPrefab.OpacityChange.Init(startPos, paintDecalPrefab.cwPaintDecal2D);
       paintDecalPrefab.ScaleChange.Init(startPos, paintDecalPrefab.cwPaintDecal2D);
       canMove = true;
   }

   public void CalculateMaxDistance(float lifeTime)
   {
       return;
      //  float calculatedDistance = lifeTime * distanceMultiplier;
       float calculatedDistance = lifeTime * distanceMultiplier + ( lifeTime * secondMultiplayer ) + secondMultiplayer2;
       
       //calculatedDistance = Mathf.Clamp(calculatedDistance, 0.2f, Mathf.Infinity);
      // // endPos = transform.position + new Vector3(0, -calculatedDistance, 0);
        if(lifeTime > 5 ) endPos = transform.position + new Vector3(0, -10, 0);
        else  endPos = transform.position + new Vector3(0, -calculatedDistance, 0);
        
       debugGo.transform.position = endPos;
       endPosCalculated = true;

       paintDecalPrefab.OpacityChange.SetEndPos(endPos);
       paintDecalPrefab.OpacityChange.SetDensity(lifeTime);
       // ScaleChange.SetEndPos(endPos);
   }
   
  private void Update()
  {
     return;
      if(!canMove) return;
      if (!endPosCalculated) return;

     // OpacityChange.UpdateOpacityLinear();
     paintDecalPrefab.OpacityChange.UpdateOpacityWithDistanceAndDensity();
     // ScaleChange.UpdateScaleX();
      
      if (!CheckingDistance()) return;

      OnPaintDecalPrefabOnDestroy?.Invoke(paintDecalPrefab);
      Destroy(gameObject);
   }
  
  private bool CheckingDistance()
  {
   
      float distanceToEnd = Vector3.Distance(transform.position, endPos);
      return distanceToEnd <= 0.1f;
  }

   public void DisablePaint()
   {
       canMove = false;
       transform.parent = null;
       paintDecalPrefab.cwPaintDecal2D.enabled = false;
       // Destroy(gameObject);
   }
}