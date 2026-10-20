using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshDrawer_MatClone : MonoBehaviour
{
    public ColorType ColorType;
    private List<Vector3> points = new List<Vector3>();
    private bool isDrawing = false;

    private Mesh tempMesh;
    private MeshFilter tempMeshFilter;
    private MeshRenderer tempMeshRenderer;

    public float meshWidth = 0.2f;
    public float meshDepth = 0.1f;
    public int segmentCount = 10;

    public Color drawColor = Color.green;

    public Material baseMaterial; // Public base material

    private void Start()
    {
        CreateTemporaryMesh();
    }

    public void SetColor(Color color)
    {
        drawColor = color;

        if (tempMeshRenderer != null)
        {
            tempMeshRenderer.material.color = drawColor;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDrawing = true;
            points.Clear();

            if (tempMeshRenderer != null)
            {
                tempMeshRenderer.material.color = drawColor;
            }
        }

        if (Input.GetMouseButton(0) && isDrawing)
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 10f;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

            if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], worldPos) > 0.1f)
            {
                points.Add(worldPos);
            }

            UpdateTemporaryMesh();
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDrawing = false;

            CreateSegments();
            ResetTemporaryMesh();
            points.Clear();
        }
    }

    private void CreateTemporaryMesh()
    {
        GameObject tempMeshObject = new GameObject("TemporaryMesh");
        tempMeshFilter = tempMeshObject.AddComponent<MeshFilter>();
        tempMeshRenderer = tempMeshObject.AddComponent<MeshRenderer>();

        // Material klonlanıyor ve özelleştiriliyor
        Material tempMaterial = Instantiate(baseMaterial);
        tempMaterial.color = drawColor;
        tempMeshRenderer.material = tempMaterial;

        tempMesh = new Mesh();
        tempMeshFilter.mesh = tempMesh;
    }

    private void UpdateTemporaryMesh()
    {
        if (points.Count < 2) return;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 forward = (points[i + 1] - points[i]).normalized;
            Vector3 right = Vector3.Cross(forward, Vector3.back).normalized;

            Vector3 leftFront = points[i] - right * meshWidth / 2f;
            Vector3 rightFront = points[i] + right * meshWidth / 2f;
            Vector3 leftBack = leftFront - Vector3.forward * meshDepth;
            Vector3 rightBack = rightFront - Vector3.forward * meshDepth;

            vertices.Add(leftFront);
            vertices.Add(rightFront);
            vertices.Add(leftBack);
            vertices.Add(rightBack);

            if (i < points.Count - 2)
            {
                int startIndex = i * 4;

                // Ön yüz
                triangles.Add(startIndex);
                triangles.Add(startIndex + 1);
                triangles.Add(startIndex + 5);
                triangles.Add(startIndex);
                triangles.Add(startIndex + 5);
                triangles.Add(startIndex + 4);

                // Arka yüz
                triangles.Add(startIndex + 2);
                triangles.Add(startIndex + 7);
                triangles.Add(startIndex + 3);
                triangles.Add(startIndex + 2);
                triangles.Add(startIndex + 6);
                triangles.Add(startIndex + 7);

                // Yan yüzler
                triangles.Add(startIndex);
                triangles.Add(startIndex + 4);
                triangles.Add(startIndex + 6);
                triangles.Add(startIndex);
                triangles.Add(startIndex + 6);
                triangles.Add(startIndex + 2);

                triangles.Add(startIndex + 1);
                triangles.Add(startIndex + 3);
                triangles.Add(startIndex + 7);
                triangles.Add(startIndex + 1);
                triangles.Add(startIndex + 7);
                triangles.Add(startIndex + 5);
            }
        }

        tempMesh.Clear();
        tempMesh.SetVertices(vertices);
        tempMesh.SetTriangles(triangles, 0);
        tempMesh.RecalculateNormals();
    }

    private void ResetTemporaryMesh()
    {
        tempMesh.Clear();
    }

    private void CreateSegments()
    {
        if (points.Count < 2 || segmentCount <= 0) return;

        int pointsPerSegment = Mathf.CeilToInt(points.Count / (float)segmentCount);

        for (int i = 0; i < segmentCount; i++)
        {
            int startIndex = i * pointsPerSegment;
            int endIndex = Mathf.Min(startIndex + pointsPerSegment, points.Count - 1);

            if (endIndex <= startIndex) break;

            List<Vector3> segmentPoints = points.GetRange(startIndex, endIndex - startIndex + 1);

            if (endIndex < points.Count - 1)
            {
                segmentPoints.Add(points[endIndex + 1]);
            }

            CreateSegmentMesh(segmentPoints);
        }
    }

   /* private void CreateSegmentMesh(List<Vector3> segmentPoints)
    {
        if (segmentPoints.Count < 2) return;

        GameObject segmentObject = new GameObject("MeshSegment");
        MeshFilter segmentFilter = segmentObject.AddComponent<MeshFilter>();
        MeshRenderer segmentRenderer = segmentObject.AddComponent<MeshRenderer>();

        // Material klonlanıyor ve özelleştiriliyor
        Material segmentMaterial = Instantiate(baseMaterial);
        segmentMaterial.color = drawColor;
        segmentRenderer.material = segmentMaterial;

        Mesh segmentMesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < segmentPoints.Count - 1; i++)
        {
            Vector3 forward = (segmentPoints[i + 1] - segmentPoints[i]).normalized;
            Vector3 right = Vector3.Cross(forward, Vector3.back).normalized;

            Vector3 leftFront = segmentPoints[i] - right * meshWidth / 2f;
            Vector3 rightFront = segmentPoints[i] + right * meshWidth / 2f;
            Vector3 leftBack = leftFront - Vector3.forward * meshDepth;
            Vector3 rightBack = rightFront - Vector3.forward * meshDepth;

            vertices.Add(leftFront);
            vertices.Add(rightFront);
            vertices.Add(leftBack);
            vertices.Add(rightBack);

            if (i < segmentPoints.Count - 2)
            {
                int startIndex = i * 4;

                // Ön yüz
                triangles.Add(startIndex);
                triangles.Add(startIndex + 1);
                triangles.Add(startIndex + 5);
                triangles.Add(startIndex);
                triangles.Add(startIndex + 5);
                triangles.Add(startIndex + 4);

                // Arka yüz
                triangles.Add(startIndex + 2);
                triangles.Add(startIndex + 7);
                triangles.Add(startIndex + 3);
                triangles.Add(startIndex + 2);
                triangles.Add(startIndex + 6);
                triangles.Add(startIndex + 7);

                // Yan yüzler
                triangles.Add(startIndex);
                triangles.Add(startIndex + 4);
                triangles.Add(startIndex + 6);
                triangles.Add(startIndex);
                triangles.Add(startIndex + 6);
                triangles.Add(startIndex + 2);

                triangles.Add(startIndex + 1);
                triangles.Add(startIndex + 3);
                triangles.Add(startIndex + 7);
                triangles.Add(startIndex + 1);
                triangles.Add(startIndex + 7);
                triangles.Add(startIndex + 5);
            }
        }

        segmentMesh.SetVertices(vertices);
        segmentMesh.SetTriangles(triangles, 0);
        segmentMesh.RecalculateNormals();
        segmentFilter.mesh = segmentMesh;

        MeshCollider collider = segmentObject.AddComponent<MeshCollider>();
        segmentObject.AddComponent<MeshSegment>();
        collider.sharedMesh = segmentMesh;
        collider.convex = true;
    }*/
   
   // Daha fazla segment eklemek için bir yoğunluk faktörü
   public int subdivisions = 10; // Yuvarlaklık için segment başına ek vertex sayısı
   
   private void CreateSegmentMesh(List<Vector3> segmentPoints)
  {
    if (segmentPoints.Count < 2) return;

    GameObject segmentObject = new GameObject("MeshSegment");
    MeshFilter segmentFilter = segmentObject.AddComponent<MeshFilter>();
    MeshRenderer segmentRenderer = segmentObject.AddComponent<MeshRenderer>();

    // Material klonlanıyor ve özelleştiriliyor
    Material segmentMaterial = Instantiate(baseMaterial);
    segmentMaterial.color = drawColor;
    segmentRenderer.material = segmentMaterial;

    Mesh segmentMesh = new Mesh();
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();

    

    for (int i = 0; i < segmentPoints.Count - 1; i++)
    {
        // İki nokta arasını "subdivisions" kadar böler
        Vector3 start = segmentPoints[i];
        Vector3 end = segmentPoints[i + 1];

        for (int j = 0; j < subdivisions; j++)
        {
            float t = j / (float)(subdivisions - 1); // Lerp oranı
            Vector3 interpolatedPoint = Vector3.Lerp(start, end, t);

            // Yuvarlaklık ve genişlik için ek vertex hesaplamaları
            Vector3 forward = (end - start).normalized;
            Vector3 right = Vector3.Cross(forward, Vector3.back).normalized;

            Vector3 leftFront = interpolatedPoint - right * meshWidth / 2f;
            Vector3 rightFront = interpolatedPoint + right * meshWidth / 2f;
            Vector3 leftBack = leftFront - Vector3.forward * meshDepth;
            Vector3 rightBack = rightFront - Vector3.forward * meshDepth;

            vertices.Add(leftFront);
            vertices.Add(rightFront);
            vertices.Add(leftBack);
            vertices.Add(rightBack);

            // Üçgenler eklenir
            if (j < subdivisions - 1)
            {
                int startIndex = i * subdivisions * 4 + j * 4;

                // Ön yüz
                triangles.Add(startIndex);
                triangles.Add(startIndex + 1);
                triangles.Add(startIndex + 5);
                triangles.Add(startIndex);
                triangles.Add(startIndex + 5);
                triangles.Add(startIndex + 4);

                // Arka yüz
                triangles.Add(startIndex + 2);
                triangles.Add(startIndex + 7);
                triangles.Add(startIndex + 3);
                triangles.Add(startIndex + 2);
                triangles.Add(startIndex + 6);
                triangles.Add(startIndex + 7);

                // Yan yüzler
                triangles.Add(startIndex);
                triangles.Add(startIndex + 4);
                triangles.Add(startIndex + 6);
                triangles.Add(startIndex);
                triangles.Add(startIndex + 6);
                triangles.Add(startIndex + 2);

                triangles.Add(startIndex + 1);
                triangles.Add(startIndex + 3);
                triangles.Add(startIndex + 7);
                triangles.Add(startIndex + 1);
                triangles.Add(startIndex + 7);
                triangles.Add(startIndex + 5);
            }
        }
    }

    // Mesh'i segment objesine uygula
    segmentMesh.SetVertices(vertices);
    segmentMesh.SetTriangles(triangles, 0);

    // Normalleri yeniden hesapla
    segmentMesh.RecalculateNormals(); // Smooth normals sağlar
    segmentMesh.RecalculateBounds();  // Mesh'in sınırlarını yeniden hesaplar
    segmentFilter.mesh = segmentMesh;

    // Collider ekle
    MeshCollider collider = segmentObject.AddComponent<MeshCollider>();
    segmentObject.AddComponent<MeshSegment>();
    collider.sharedMesh = segmentMesh;
    collider.convex = true;
}
}
