using System;
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

    public GameObject PaintDecalParent;
    
    public static Action OnSpawnPaintDecal;

    private void OnEnable()
    {
        PaintAmount.OnFinishPaint += FinishPaintAmount;
    }

    private void OnDisable()
    {
        PaintAmount.OnFinishPaint -= FinishPaintAmount;
    }

    private void FinishPaintAmount()
    {
        PaintDecalParent.SetActive(false);
    }

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
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = GetWorldPositionFromMouse();
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider != null && hit.collider.CompareTag("PaintableBook"))
            {
                if (paintPositions.Contains(mousePosition)) return;

                HandleSpawn(mousePosition);
                isHolding = true;
            }
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
        OnSpawnPaintDecal?.Invoke();
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