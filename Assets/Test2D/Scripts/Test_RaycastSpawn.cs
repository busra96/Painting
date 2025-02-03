using System.Collections.Generic;
using PaintIn2D;
using UnityEngine;
using UnityEngine.EventSystems;

public class Test_RaycastSpawn : MonoBehaviour
{
    public GameObject prefabToSpawn;
    public Camera mainCamera;
    public ColorSelection ColorSelection;

    public float minimumDistance = 1f;

    private Vector2 lastSpawnPosition;
    private bool isHolding = false;

    public List<Movement> paintMovements = new List<Movement>();
    public float MultiplayLifeTime;

    private int paintIndex = 0;

    private void CalculateLifeTime()
    {
        float lifetime = paintMovements.Count;
        foreach (var paintMovement in paintMovements)
        {
            paintMovement.CalculateMaxDistance(lifetime);
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            
            if (!HandleSpawn(mousePosition)) return;

            isHolding = true;
        }
        
        if (Input.GetMouseButtonUp(0))
        {
            isHolding = false;
            
            Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastToPaper(mousePosition);

            
            CalculateLifeTime();
            
            foreach (var paint in paintMovements)
            {
                paint.paintDecalPrefab.ID = paintIndex;
            }

            paintIndex++;
            paintMovements.Clear();
        }

        if (isHolding)
        {
            Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);

            if (Vector2.Distance(mousePosition, lastSpawnPosition) >= minimumDistance)
            {
                HandleSpawn(mousePosition);
            }
        }
    }

    bool HandleSpawn(Vector2 position)
    {
        if (prefabToSpawn == null) return false;
        
        RaycastHit2D hit = Physics2D.Raycast(position, Vector2.zero);

        if (!hit.collider) return false;
        if (!hit.collider.CompareTag("PaintableBook")) return false;
        
        RaycastToPaper(position);
        return true;
    }

    private void RaycastToPaper(Vector2 position)
    {
        GameObject obj = Instantiate(prefabToSpawn, position, Quaternion.identity); // Prefab oluştur
        obj.GetComponent<CwPaintDecal2D>().Color = ColorSelection.GetOriginalColor(); // Renk ata
        lastSpawnPosition = position; // Son spawn pozisyonunu güncelle

        Movement paintMovement = obj.GetComponent<Movement>();
        paintMovements.Add(paintMovement);

        paintMovement.gameObject.name = "Paint_" + paintIndex;
    }
}
