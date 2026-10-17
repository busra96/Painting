using System.Collections.Generic;
using UnityEngine;

public class MeshDrawerTest : MonoBehaviour
{
    private List<Vector3> points = new List<Vector3>();
    private bool isDrawing = false;

    // Geçici mesh bileşenleri
    private Mesh tempMesh;
    private MeshFilter tempMeshFilter;
    private MeshRenderer tempMeshRenderer;

    // Mesh'in genişliği ve derinliği
    public float meshWidth = 0.2f; // Çizgi genişliği
    public float meshDepth = 0.1f; // Çizgi derinliği

    // Parçalara bölme
    public int segmentCount = 10; // Mesh'in kaç parçaya bölüneceği

    // Çizim rengi
    public Color drawColor = Color.green;

    private void Start()
    {
        CreateTemporaryMesh();
    }

    public void SetColor(Color color)
    {
        drawColor = color;
        
        // Geçici mesh'in rengini hemen güncelle
        if (tempMeshRenderer != null)
        {
            tempMeshRenderer.material.color = drawColor;
        }
    }
    
    private void Update()
    {
        // Fare sol tık durumlarını kontrol et
        if (Input.GetMouseButtonDown(0)) // Tıklama başladığında
        {
            isDrawing = true;
            points.Clear(); // Önceki noktaları temizle
            
            if (tempMeshRenderer != null)
            {
                tempMeshRenderer.material.color = drawColor;
            }
        }

        if (Input.GetMouseButton(0) && isDrawing) // Fareye basılı tutulurken
        {
            // Farenin dünya uzayındaki pozisyonunu hesapla
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 10f; // Kameradan uzaklık
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

            // Noktayı listeye ekle (aynı noktayı tekrar eklememek için kontrol)
            if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], worldPos) > 0.1f)
            {
                points.Add(worldPos);
            }

            // Geçici mesh'i sürekli güncelle
            UpdateTemporaryMesh();
        }

        if (Input.GetMouseButtonUp(0)) // Fareyi bıraktığınızda
        {
            isDrawing = false;

            // Çizimi parçalara böl ve sahneye kaydet
            CreateSegments();

            // Geçici mesh'i sıfırla
            ResetTemporaryMesh();

            // Noktaları temizle
            points.Clear();
        }
    }

    private void CreateTemporaryMesh()
    {
        // Geçici mesh için bir GameObject oluştur
        GameObject tempMeshObject = new GameObject("TemporaryMesh");
        tempMeshFilter = tempMeshObject.AddComponent<MeshFilter>();
        tempMeshRenderer = tempMeshObject.AddComponent<MeshRenderer>();

        // Malzeme ve renk ayarı
        Material tempMaterial = new Material(Shader.Find("Standard"));
        tempMaterial.color = drawColor;
        
        // Smoothness ve Metallic ayarları
        tempMaterial.SetFloat("_Smoothness", 0f); // Pürüzsüzlük 0
        tempMaterial.SetFloat("_Metallic", 0f);   // Metalliği 0

        // Emission Color ayarları
        tempMaterial.EnableKeyword("_EMISSION");
        tempMaterial.SetColor("_EmissionColor", new Color(50f / 255f, 50f / 255f, 50f / 255f));
        
        tempMeshRenderer.material = tempMaterial;

        // Yeni bir geçici mesh oluştur
        tempMesh = new Mesh();
        tempMeshFilter.mesh = tempMesh;
    }

    private void UpdateTemporaryMesh()
    {
        // Eğer yeterli nokta yoksa mesh oluşturma
        if (points.Count < 2) return;

        // Vertex ve triangle listelerini oluştur
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Mesh'in genişliğini hesaplayarak iki tarafı çiz
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

        // Geçici mesh'i güncelle
        tempMesh.Clear();
        tempMesh.SetVertices(vertices);
        tempMesh.SetTriangles(triangles, 0);
        tempMesh.RecalculateNormals();
    }

    private void ResetTemporaryMesh()
    {
        // Geçici mesh'i sıfırla
        tempMesh.Clear();
    }

    private void CreateSegments()
    {
        if (points.Count < 2 || segmentCount <= 0) return;

        // Her bir segmentin uzunluğunu belirle
        int pointsPerSegment = Mathf.CeilToInt(points.Count / (float)segmentCount);

        for (int i = 0; i < segmentCount; i++)
        {
            int startIndex = i * pointsPerSegment;
            int endIndex = Mathf.Min(startIndex + pointsPerSegment, points.Count - 1);

            if (endIndex <= startIndex) break;

            // Segmentin noktalarını al ve birleştirme sağlamak için bir sonraki segmentin ilk noktasını ekle
            List<Vector3> segmentPoints = points.GetRange(startIndex, endIndex - startIndex + 1);

            // Sonraki segmentle bağlantı sağlamak için bir sonraki nokta eklenir
            if (endIndex < points.Count - 1)
            {
                segmentPoints.Add(points[endIndex + 1]);
            }

            // Yeni bir segment oluştur
            CreateSegmentMesh(segmentPoints);
        }
    }

    private void CreateSegmentMesh(List<Vector3> segmentPoints)
    {
        // Eğer segment geçerli bir mesh oluşturamıyorsa, işlemi durdur
        if (segmentPoints.Count < 2) return;

        // Segment için GameObject oluştur
        GameObject segmentObject = new GameObject("MeshSegment");
        MeshFilter segmentFilter = segmentObject.AddComponent<MeshFilter>();
        MeshRenderer segmentRenderer = segmentObject.AddComponent<MeshRenderer>();
        
        Material segmentMaterial = new Material(Shader.Find("Standard"));
        segmentMaterial.color = drawColor;
        
        // Smoothness ve Metallic ayarları
        segmentMaterial.SetFloat("_Smoothness", 0f); // Pürüzsüzlük 0
        segmentMaterial.SetFloat("_Metallic", 0f);   // Metalliği 0

        // Emission Color ayarları
        segmentMaterial.EnableKeyword("_EMISSION");
        segmentMaterial.SetColor("_EmissionColor", new Color(50 / 255f, 50 / 255f, 50 / 255f));
        
        segmentRenderer.material = segmentMaterial;


        // Mesh verilerini oluştur
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

        // Mesh'i segment objesine uygula
        segmentMesh.SetVertices(vertices);
        segmentMesh.SetTriangles(triangles, 0);
        segmentMesh.RecalculateNormals();
        segmentFilter.mesh = segmentMesh;

        // Collider ekle
        MeshCollider collider = segmentObject.AddComponent<MeshCollider>();
        segmentObject.AddComponent<MeshSegment>();
        collider.sharedMesh = segmentMesh;
        collider.convex = true; // Collider'ı convex yaparak çarpışma tespitini kolaylaştır
    }
}
