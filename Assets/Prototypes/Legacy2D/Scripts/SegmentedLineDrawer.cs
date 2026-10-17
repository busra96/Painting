using System.Collections.Generic;
using UnityEngine;

public class SegmentedLineDrawer : MonoBehaviour
{
    [SerializeField] private GameObject linePrefab;
    [SerializeField] private float lengthThreshold = 0.05f;
    [SerializeField] private float lineWidth = 0.3f;
    [SerializeField] private float shadowWidth = 0.4f;

    private Vector3 lastSegmentEndPoint;
    private bool isDrawing = false;
    private Camera mainCamera;

    private GameObject currentLineObject;
    private LineRenderer currentMainLineRenderer;
    private LineRenderer currentShadowLineRenderer;
    private List<Vector3> currentPoints = new List<Vector3>();

    [SerializeField] private Color mainColor = Color.white;
    [SerializeField] private Color shadowColor = new Color(0, 0, 0, 0.3f);

    private float zOffset = -0.02f;

    void Start()
    {
        mainCamera = Camera.main;
    }

    public void SetMainColor(Color mainColor)
    {
        this.mainColor = mainColor;
    }
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartDrawing();
        }

        if (isDrawing)
        {
            UpdateDrawing();
        }

        if (Input.GetMouseButtonUp(0))
        {
            StopDrawing();
        }
    }

    void StartDrawing()
    {
        isDrawing = true;
        currentPoints.Clear();
        lastSegmentEndPoint = GetMouseWorldPosition();
        CreateNewSegment(lastSegmentEndPoint);
    }

    void UpdateDrawing()
    {
        Vector3 mousePosition = GetMouseWorldPosition();

        if (Vector3.Distance(lastSegmentEndPoint, mousePosition) >= lengthThreshold)
        {
            CreateNewSegment(lastSegmentEndPoint);
            lastSegmentEndPoint = mousePosition;
        }

        if (currentMainLineRenderer == null || currentShadowLineRenderer == null)
        {
            return;
        }

        currentPoints.Add(mousePosition);
        currentMainLineRenderer.positionCount = currentPoints.Count;
        currentShadowLineRenderer.positionCount = currentPoints.Count;

        currentMainLineRenderer.SetPosition(currentPoints.Count - 1, mousePosition);
        Vector3 shadowOffset = new Vector3(0.05f, 0.05f, zOffset);
        currentShadowLineRenderer.SetPosition(currentPoints.Count - 1, mousePosition + shadowOffset);
    }

    void StopDrawing()
    {
        isDrawing = false;
    }

    void CreateNewSegment(Vector3 startPosition)
    {
        Vector3 endPosition = GetMouseWorldPosition();
        float segmentLength = Vector3.Distance(startPosition, endPosition);

        if (segmentLength < 0.001f)
        {
            return;
        }

        currentLineObject = Instantiate(linePrefab, Vector3.zero, Quaternion.identity, transform);

        if (currentLineObject == null)
        {
            return;
        }

        currentMainLineRenderer = currentLineObject.GetComponent<LineRenderer>();
        currentShadowLineRenderer = currentLineObject.transform.Find("ShadowLine")?.GetComponent<LineRenderer>();

        if (currentMainLineRenderer == null || currentShadowLineRenderer == null)
        {
            return;
        }

        SetupLineRenderer(currentMainLineRenderer, lineWidth, mainColor, 10);
        SetupLineRenderer(currentShadowLineRenderer, shadowWidth, shadowColor, 5, true);

        currentPoints.Clear();
        currentPoints.Add(startPosition);
        currentMainLineRenderer.positionCount = 2;
        currentShadowLineRenderer.positionCount = 2;
        currentMainLineRenderer.SetPosition(0, startPosition);
        currentMainLineRenderer.SetPosition(1, endPosition);
        currentShadowLineRenderer.SetPosition(0, startPosition + new Vector3(0.05f, 0.05f, zOffset));
        currentShadowLineRenderer.SetPosition(1, endPosition + new Vector3(0.05f, 0.05f, zOffset));

        AddColliderToLineSegment(currentLineObject, startPosition, endPosition);
    }

    void AddColliderToLineSegment(GameObject segment, Vector3 startPosition, Vector3 endPosition)
    {
        BoxCollider collider = segment.AddComponent<BoxCollider>();
        collider.isTrigger = true;

        Vector3 segmentCenter = (startPosition + endPosition) / 2;
        float segmentLength = Vector3.Distance(startPosition, endPosition);

        collider.center = segmentCenter;
        collider.size = new Vector3(segmentLength, 0.1f, 0.1f);
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        return mainCamera.ScreenToWorldPoint(mousePos);
    }

    void SetupLineRenderer(LineRenderer lr, float width, Color color, int sortingOrder, bool isShadow = false)
    {
        lr.startWidth = width;
        lr.endWidth = width;

        Material material = new Material(Shader.Find("Sprites/Default"));
        material.SetColor("_Color", color);

        if (isShadow)
        {
            material.renderQueue = 3000;
        }

        lr.material = material;
        lr.startColor = color;
        lr.endColor = color;
        lr.useWorldSpace = true;
        lr.sortingOrder = sortingOrder;
    }
}
