using System.Collections.Generic;
using PaintIn2D;
using UnityEngine;

public class RaycastAndSpawn : MonoBehaviour
{
    public GameObject prefabToSpawn; 
    public Camera mainCamera;
    public ColorSelection ColorSelection;
    
    public float minimumDistance = 1f; // Obje oluşturma için minimum mesafe

    private Vector2 lastSpawnPosition; // Son oluşturulan objenin pozisyonu
    private bool isHolding = false;   // Mouse'un basılı olup olmadığını izlemek için

    public List<PaintMovement> paintMovements;
    public float MultiplayLifeTime;

    private int paintIndex = 0;

    public void CalculateLifeTime()
    {
        if(paintMovements == null)return;

        float lifetime = paintMovements.Count * MultiplayLifeTime;
        foreach (var paintMovements in paintMovements)
            paintMovements.LifeTime = lifetime;
    }
    
   /* void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
         
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider != null) 
            {
                Debug.Log("Raycast temas etti: " + hit.collider.gameObject.name);

                SpawnObjectAtPosition(hit.point);
            }
            else
            {
                Debug.Log("Raycast herhangi bir nesneye temas etmedi.");
            }
        }
    }
    // Spawn işlemini gerçekleştiren metod
    void SpawnObjectAtPosition(Vector2 position)
    {
        if (prefabToSpawn != null)
        {
            GameObject obj = Instantiate(prefabToSpawn, position, Quaternion.identity);
            obj.GetComponent<CwPaintDecal2D>().Color = ColorSelection.GetOriginalColor();
        }
    }
    
    */
   
   void Update()
   {
       if (Input.GetMouseButtonDown(0)) // Mouse sol tık başladığında
       {
           isHolding = true;
           Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
           HandleSpawn(mousePosition); // İlk tıklamada obje oluştur
       }
       else if (Input.GetMouseButton(0) && isHolding) // Mouse sol tık basılı tutulduğunda
       {
           Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);

           // Eğer mouse'un pozisyonu, son oluşturulan pozisyondan minimum mesafeyi aşarsa
           if (Vector2.Distance(mousePosition, lastSpawnPosition) >= minimumDistance)
           {
               HandleSpawn(mousePosition); // Yeni obje oluştur
           }
       }
       else if (Input.GetMouseButtonUp(0)) // Mouse sol tık bırakıldığında
       {
           isHolding = false; // Basılı durumu sıfırla
        
           CalculateLifeTime();

           foreach (var paint in paintMovements)
           {
               paint.currentIndex = paintIndex;
           }

           paintIndex++;
           paintMovements.Clear();
       }
   }
   
   void HandleSpawn(Vector2 position)
   {
       if (prefabToSpawn != null)
       {
           // Raycast yap ve hit kontrolü
           RaycastHit2D hit = Physics2D.Raycast(position, Vector2.zero);

           if (hit.collider != null)
           {
               // Çarpışılan objenin tag'i "PaintableBook" ise spawn işlemini yap
               if (hit.collider.CompareTag("PaintableBook"))
               {
                   GameObject obj = Instantiate(prefabToSpawn, position, Quaternion.identity); // Prefab oluştur
                   obj.GetComponent<CwPaintDecal2D>().Color = ColorSelection.GetOriginalColor(); // Renk ata
                   lastSpawnPosition = position; // Son spawn pozisyonunu güncelle
                   
                   PaintMovement paintMovement = obj.GetComponent<PaintMovement>();
                   paintMovements.Add(paintMovement);

                   paintMovement.gameObject.name = "Paint_" + paintIndex.ToString();
               }
               else
               {
                   Debug.Log("Raycast 'PaintableBook' tag'li bir obje üzerinde değil.");
               }
           }
           else
           {
               Debug.Log("Raycast herhangi bir nesneye temas etmedi.");
           }
       }
       else
       {
           Debug.LogWarning("Prefab atanmadı! Obje oluşturulamadı.");
       }
       
       
       /*if (prefabToSpawn != null)
       {
           GameObject obj = Instantiate(prefabToSpawn, position, Quaternion.identity); // Prefab oluştur
           obj.GetComponent<CwPaintDecal2D>().Color = ColorSelection.GetOriginalColor(); // Renk ata
           lastSpawnPosition = position; // Son spawn pozisyonunu güncelle
       }
       else
       {
           Debug.LogWarning("Prefab atanmadı! Obje oluşturulamadı.");
       }*/
   }

    
}
