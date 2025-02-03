using System.Collections.Generic;
using UnityEngine;

public class SnakeMeshDrawerWithColor : MonoBehaviour
{
    
    // Çizimle ilgili veriler
    private List<Vector3> points = new List<Vector3>();
    private bool isDrawing = false;

    // Mesh için bileşenler
    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    // Mesh'in genişliği ve derinliği
    public float meshWidth = 0.2f; // Genişlik
    public float meshDepth = 0.1f; // Derinlik
    public float zPos = -.1f;

    // Çizim rengi
    public Color drawColor = Color.green; // Başlangıçta yeşil renk

    private void Start()
    {
        // Yeni bir mesh başlat
        CreateNewMesh();
    }

    private void Update()
    {
        // Fare sol tık durumlarını kontrol et
        if (Input.GetMouseButtonDown(0)) // Tıklama başladığında
        {
            isDrawing = true;
            points.Clear(); // Önceki noktaları temizle
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

            // Mesh'i sürekli güncelle
            UpdateMesh();
        }

        if (Input.GetMouseButtonUp(0)) // Fareyi bıraktığınızda
        {
            isDrawing = false;

            // Çizimi sahneye kaydet ve yeni bir mesh başlat
            SaveCurrentMeshToScene();
            CreateNewMesh();
        }
    }

    private void CreateNewMesh()
    {
        // Mesh bileşenlerini yeni bir GameObject'te başlat
        GameObject meshObject = new GameObject("DrawnMesh");
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshRenderer = meshObject.AddComponent<MeshRenderer>();

        // Malzeme ve renk ayarı
        meshRenderer.material = new Material(Shader.Find("Standard"));
        meshRenderer.material.color = drawColor;

        // Yeni bir mesh oluştur
        mesh = new Mesh();
        meshFilter.mesh = mesh;
    }

    private void SaveCurrentMeshToScene()
    {
        // Çizimi sahnede bırakmak için mevcut mesh'i bağlı olduğu GameObject'ten ayırıyoruz
        meshFilter.mesh = mesh;

        // Mevcut mesh'in referanslarını temizle, yeni bir mesh başlatılacak
        mesh = null;
        meshFilter = null;
        meshRenderer = null;
    }
    
    
   private void UpdateMesh()
{
    if (points.Count < 2) return;

    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();

    // Mesh'in genişliğini ve yönünü hesaplayarak noktalar oluştur
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

            // Ön yüz (Z = 0)
            triangles.Add(startIndex);
            triangles.Add(startIndex + 1);
            triangles.Add(startIndex + 5);

            triangles.Add(startIndex);
            triangles.Add(startIndex + 5);
            triangles.Add(startIndex + 4);

            // Arka yüz (Z = -depth)
            triangles.Add(startIndex + 2);
            triangles.Add(startIndex + 7);
            triangles.Add(startIndex + 3);

            triangles.Add(startIndex + 2);
            triangles.Add(startIndex + 6);
            triangles.Add(startIndex + 7);

            // Yan yüzler
            // Sol yan
            triangles.Add(startIndex);
            triangles.Add(startIndex + 4);
            triangles.Add(startIndex + 6);

            triangles.Add(startIndex);
            triangles.Add(startIndex + 6);
            triangles.Add(startIndex + 2);

            // Sağ yan
            triangles.Add(startIndex + 1);
            triangles.Add(startIndex + 3);
            triangles.Add(startIndex + 7);

            triangles.Add(startIndex + 1);
            triangles.Add(startIndex + 7);
            triangles.Add(startIndex + 5);
        }
    }

    // Başlangıç ve bitiş noktalarına yarım ay şeklinde kavis ekle
    if (points.Count > 2)
    {
        AddRoundedCaps(vertices, triangles);
    }

    mesh.Clear();
    mesh.SetVertices(vertices);
    mesh.SetTriangles(triangles, 0);
    mesh.RecalculateNormals();
}

private void AddRoundedCaps(List<Vector3> vertices, List<int> triangles)
{
    // Başlangıç noktasında yarım ay ekle
    Vector3 startPoint = points[0];
    Vector3 direction = (points[1] - startPoint).normalized;
    Vector3 perpendicular = Vector3.Cross(direction, Vector3.back).normalized;

    int startIndex = vertices.Count;

    // Yarım ay oluştur (dışarıya doğru bombeli bir şekil)
    int segments = 16; // Yarım ayın detay seviyesi
    for (int i = 0; i <= segments; i++)
    {
        float angle = Mathf.PI * i / segments; // 180 derece
        Vector3 offset = Mathf.Cos(angle) * perpendicular * (meshWidth / 2f) + Mathf.Sin(angle) * -direction * (meshWidth / 2f);
        vertices.Add(startPoint + offset);
    }

    // Yarım ay üçgenleri oluştur
    for (int i = 1; i < segments; i++)
    {
        triangles.Add(startIndex);
        triangles.Add(startIndex + i);
        triangles.Add(startIndex + i + 1);
    }

    // Bitiş noktasında yarım ay ekle
    Vector3 endPoint = points[points.Count - 1];
    direction = (endPoint - points[points.Count - 2]).normalized;
    perpendicular = Vector3.Cross(direction, Vector3.back).normalized;

    int endIndex = vertices.Count;

    for (int i = 0; i <= segments; i++)
    {
        float angle = Mathf.PI * i / segments; // 180 derece
        Vector3 offset = Mathf.Cos(angle) * perpendicular * (meshWidth / 2f) + Mathf.Sin(angle) * direction * (meshWidth / 2f);
        vertices.Add(endPoint + offset);
    }

    // Yarım ay üçgenleri oluştur
    for (int i = 1; i < segments; i++)
    {
        triangles.Add(endIndex);
        triangles.Add(endIndex + i + 1);
        triangles.Add(endIndex + i);
    }
}


    
    
  

    /*private void UpdateMesh()
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

            // Ön yüz (Z = 0) noktaları
            Vector3 leftFront = points[i] - right * meshWidth / 2f;
            Vector3 rightFront = points[i] + right * meshWidth / 2f;

            // Arka yüz (Z = -depth) noktaları
            Vector3 leftBack = leftFront - Vector3.forward * meshDepth;
            Vector3 rightBack = rightFront - Vector3.forward * meshDepth;

            // Ön ve arka yüz için verteksleri ekle
            vertices.Add(leftFront); // Ön sol
            vertices.Add(rightFront); // Ön sağ
            vertices.Add(leftBack); // Arka sol
            vertices.Add(rightBack); // Arka sağ

            // Triangles ekle (ön ve arka yüzler)
            if (i < points.Count - 2) // Son noktada triangle ekleme
            {
                int startIndex = i * 4;

                // Ön yüz (Z = 0)
                triangles.Add(startIndex);
                triangles.Add(startIndex + 1);
                triangles.Add(startIndex + 5);

                triangles.Add(startIndex);
                triangles.Add(startIndex + 5);
                triangles.Add(startIndex + 4);

                // Arka yüz (Z = -depth)
                triangles.Add(startIndex + 2);
                triangles.Add(startIndex + 7);
                triangles.Add(startIndex + 3);

                triangles.Add(startIndex + 2);
                triangles.Add(startIndex + 6);
                triangles.Add(startIndex + 7);

                // Yan yüzler
                // Sol yan
                triangles.Add(startIndex);
                triangles.Add(startIndex + 4);
                triangles.Add(startIndex + 6);

                triangles.Add(startIndex);
                triangles.Add(startIndex + 6);
                triangles.Add(startIndex + 2);

                // Sağ yan
                triangles.Add(startIndex + 1);
                triangles.Add(startIndex + 3);
                triangles.Add(startIndex + 7);

                triangles.Add(startIndex + 1);
                triangles.Add(startIndex + 7);
                triangles.Add(startIndex + 5);
            }
        }

        // Mesh'i güncelle
        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
    }*/
}
