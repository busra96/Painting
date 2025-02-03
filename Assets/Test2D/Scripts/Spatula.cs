using System.Collections.Generic;
using UnityEngine;

public class Spatula : MonoBehaviour
{
   public List<PaintDecalPrefab> PaintDecalPrefabs;
   
   private void OnEnable()
   {
      Movement.OnPaintDecalPrefabOnDestroy += OnPaintDecalPrefabOnDestroy;
   }

   private void OnDisable()
   {
      Movement.OnPaintDecalPrefabOnDestroy -= OnPaintDecalPrefabOnDestroy;
   }

   private void OnPaintDecalPrefabOnDestroy(PaintDecalPrefab paintDecalPrefab)
   {
      RemovePaintDecalList(paintDecalPrefab);
   }

   private float maxDistance;
   public void AddPaintDecalList(PaintDecalPrefab paintDecalPrefab)
   {
      if (paintDecalPrefab == null || paintDecalPrefab.movement == null)
         return;

      if (PaintDecalPrefabs.Contains(paintDecalPrefab))
         return;

       PaintDecalPrefabs.Add(paintDecalPrefab);
       
       for (int i = 0; i < PaintDecalPrefabs.Count; i++)
       {
          PaintDecalPrefab pd = PaintDecalPrefabs[i];
          if(pd.gameObject == paintDecalPrefab.gameObject) continue;

          if (Mathf.Approximately(pd.transform.position.x, paintDecalPrefab.transform.position.x))
          {
             if (pd.ColorType == paintDecalPrefab.ColorType )
             {
                PaintDecalPrefabs.Remove(pd);
                Destroy(pd.gameObject);
             }
          }
       }

       for (int i = 0; i < PaintDecalPrefabs.Count; i++) //lifetime acılınca lifetime biten objeyi listeden cıkarıp bu foru döndürmek lazım
       {
          PaintDecalPrefab pd = PaintDecalPrefabs[i];
          
          if (Mathf.Approximately(pd.transform.position.x, paintDecalPrefab.transform.position.x))
          {
             if (pd.CwHitNearby.Priority <  GetHighestPriorityDecal(pd).CwHitNearby.Priority )
             {
                pd.gameObject.SetActive(false);
             }
             GetHighestPriorityDecal(pd).gameObject.SetActive(true);
          }
       }
   }
   
   public PaintDecalPrefab GetHighestPriorityDecal(PaintDecalPrefab paintDecalPrefab)
   {
      if (PaintDecalPrefabs == null || PaintDecalPrefabs.Count == 0 || paintDecalPrefab == null)
         return null;

      PaintDecalPrefab highestPriorityDecal = null;
      int highestPriority = int.MinValue;
      float targetX = paintDecalPrefab.transform.position.x;

      foreach (var decal in PaintDecalPrefabs)
      {
         if (decal == null || decal.CwHitNearby == null) 
            continue;

         // Eğer x koordinatları aynıysa ve priority daha yüksekse
         if (Mathf.Approximately(decal.transform.position.x, targetX) && decal.CwHitNearby.Priority > highestPriority)
         {
            highestPriority = decal.CwHitNearby.Priority;
            highestPriorityDecal = decal;
         }
      }

      return highestPriorityDecal;
   }

   public void RemovePaintDecalList(PaintDecalPrefab paintDecalPrefab)
   {
      if (paintDecalPrefab == null || paintDecalPrefab.movement == null)
         return;


      if (!PaintDecalPrefabs.Contains(paintDecalPrefab))
         return;

      PaintDecalPrefabs.Remove(paintDecalPrefab);
   }
}

public enum ColorType { Red, Pink, Orange, White, Blue , Green, Yellow}