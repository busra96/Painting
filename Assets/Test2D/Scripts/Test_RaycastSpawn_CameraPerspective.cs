/*using System.Collections.Generic;
using PaintIn2D;
using UnityEngine;
using UnityEngine.EventSystems;
public class Test_RaycastSpawn_CameraPerspective : MonoBehaviour
{
     public GameObject prefabToSpawn;
    public Camera mainCamera;
    public ColorSelection ColorSelection;

    public float minimumDistance = 1f;

    private Vector3 lastSpawnPosition; // 2D'den 3D'ye geçtik
    private bool isHolding = false;

    public List<Movement> paintMovements = new List<Movement>();
    public float MultiplayLifeTime;

    private int paintIndex = 0;
    public LayerMask PaintableBookLayer;

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
            Vector3 mousePosition = GetWorldPositionFromMouse();
            if (!HandleSpawn(mousePosition)) return;
            isHolding = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isHolding = false;

            Vector3 mousePosition = GetWorldPositionFromMouse();
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
            Vector2 mousePosition = GetWorldPositionFromMouse();
            float distance = Vector2.Distance(mousePosition, lastSpawnPosition);
            if (distance >= minimumDistance)
            {
                HandleSpawn(mousePosition);
            }
        }
    }

    bool HandleSpawn(Vector2 position)
    {
        if (prefabToSpawn == null) return false;

        RaycastHit2D hit = Physics2D.Raycast(position, Vector2.zero, PaintableBookLayer);

        if (hit.collider != null)
        {
            if (!hit.collider.CompareTag("PaintableBook"))
            {
                return false;
            }

            RaycastToPaper(hit.point); // Çarpma noktasında spawn işlemini gerçekleştir
            return true;
        }

        return false;
    }

    private void RaycastToPaper(Vector3 position)
    {
        GameObject obj = Instantiate(prefabToSpawn, position, Quaternion.identity); // Prefab oluştur
        obj.GetComponent<CwPaintDecal2D>().Color = ColorSelection.GetOriginalColor(); // Renk ata
        lastSpawnPosition = position; // Son spawn pozisyonunu güncelle

        Movement paintMovement = obj.GetComponent<Movement>();
        paintMovements.Add(paintMovement);

        paintMovement.gameObject.name = "Paint_" + paintIndex;
    }

    private Vector3 GetWorldPositionFromMouse()
    {
        // Fare pozisyonunu ekran koordinatlarından dünya koordinatlarına çevir
        Vector3 mousePosition = Input.mousePosition;
        
        mousePosition.z = Mathf.Abs(mainCamera.transform.position.z); // Kameranın Z pozisyonunu al
        mousePosition.z = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition); // Kamera perspektifini hesaba katar
        worldPosition.z = 0;
        Vector2 rayOrigin = new Vector2(worldPosition.x, worldPosition.y); // X ve Y'yi al, Z gereksiz (2D için)

        // 2D raycast gönder
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.zero, Mathf.Infinity,PaintableBookLayer); // Tam olarak o noktaya bakar
        if (hit.collider != null)
        {
            return hit.point; // Çarpma noktasını döndür
        }

        return rayOrigin; // Eğer bir collider'a çarpmadıysa, fare konumunu döndür
    }
}
*/

using System.Collections.Generic;
using PaintCore;
using PaintIn2D;
using UnityEngine;
using UnityEngine.EventSystems;

public class Test_RaycastSpawn_CameraPerspective : MonoBehaviour
{
    public CurrentImageGridProcessor gridProcessor;
    public GameObject prefabToSpawn;
    public Camera mainCamera;
    public ColorSelection ColorSelection;

    public float positionStepX = 0.1f;
    public float positionStepY = 0.1f;
    [SerializeField]private float initialRadius = 1f;

    private bool isHolding = false;

    public List<Movement> paintMovements = new List<Movement>();

    private int paintIndex = 0;
    private List<Vector3> paintPositions = new List<Vector3>();

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
            Vector3 mousePosition = GetWorldPositionFromMouse();
            if (paintPositions.Contains(mousePosition)) return;
            HandleSpawn(mousePosition);
            isHolding = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isHolding = false;
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
            Vector2 mousePosition = GetWorldPositionFromMouse();
            if (!paintPositions.Contains(mousePosition))
            {
                HandleSpawn(mousePosition);
            }
        }
    }

    private void HandleSpawn(Vector3 position)
    {
        GameObject obj = Instantiate(prefabToSpawn, position, Quaternion.identity);
        obj.GetComponent<CwPaintDecal2D>().Color = ColorSelection.GetOriginalColor();
        PaintDecalPrefab paintDecalPrefab = obj.GetComponent<PaintDecalPrefab>();
        paintDecalPrefab.ColorType = ColorSelection.ColorType;
        paintDecalPrefab.Init(gridProcessor);
        paintPositions.Add(position);

        int priority = -Mathf.CeilToInt(position.y / positionStepY);
        obj.GetComponent<CwHitNearby>().Priority = priority;
        obj.GetComponent<CwPaintDecal2D>().Radius = initialRadius + priority * 0.005f;

        paintDecalPrefab.PaintGrid.SetBlushRadius();
        
        Movement paintMovement = obj.GetComponent<Movement>();
        paintMovements.Add(paintMovement);
        paintMovement.gameObject.name = "Paint" + paintIndex;
    }

    private Vector3 GetWorldPositionFromMouse()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);
        worldPosition.z = 0;
        return SnapToGrid(worldPosition);
    }

    private Vector3 SnapToGrid(Vector3 position)
    {
        float snappedX = Mathf.Round(position.x / positionStepX) * positionStepX;
        float snappedY = Mathf.Round(position.y / positionStepY) * positionStepY;
        return new Vector3(snappedX, snappedY, position.z);
    }
}