using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SmoothPaintMesh : MonoBehaviour
{
    public Material paintMaterial;  // Boya için kullanılacak materyal
    public float paintThickness = 0.5f; // Boyanın genişliği
    public float paintHeight = 0.2f;    // Boyanın yüksekliği (kabarıklık)
    public float zPosition = 10f;       // Z eksenindeki sabit değer
    public int smoothness = 10;         // Kenarların ne kadar yumuşak olacağını belirler (daha yüksek = daha smooth)
    public float waveIntensity = 0.05f; // Dalgalanma yoğunluğu
    public float waveFrequency = 5f;    // Dalgalanma sıklığı (daha yüksek = daha sık dalgalar)

    private LineRenderer lineRenderer;  // Çizim için kullanılan LineRenderer
    private MeshFilter meshFilter;      // Boya mesh'i için MeshFilter

    void Start()
    {
        // LineRenderer bileşenini al
        lineRenderer = GetComponent<LineRenderer>();

        // Boya mesh'i için yeni bir GameObject oluştur
        GameObject paintMeshObject = new GameObject("SmoothPaintMesh");
        paintMeshObject.transform.SetParent(transform);
        paintMeshObject.transform.localPosition = Vector3.zero;

        meshFilter = paintMeshObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = paintMeshObject.AddComponent<MeshRenderer>();
        renderer.material = paintMaterial;
    }

    void Update()
    {
        if (Input.GetMouseButton(0)) // Sol tık basılıysa
        {
            // Mouse pozisyonunu al ve dünyaya çevir
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.forward, new Vector3(0, 0, zPosition)); // Z düzlemine ışın gönder
            if (plane.Raycast(ray, out float distance))
            {
                Vector3 position = ray.GetPoint(distance);
                position.z = zPosition; // Z sabit

                // Yeni bir nokta ekle (çizgiye)
                if (lineRenderer.positionCount == 0 || Vector3.Distance(lineRenderer.GetPosition(lineRenderer.positionCount - 1), position) > 0.1f)
                {
                    AddPointToLine(position);
                }
            }
        }

        // LineRenderer noktalarını al
        int pointCount = lineRenderer.positionCount;
        if (pointCount < 2) return; // En az iki nokta gerek

        Vector3[] positions = new Vector3[pointCount];
        lineRenderer.GetPositions(positions);

        // Boya mesh'ini oluştur
        Mesh paintMesh = GenerateSmoothPaintMesh(positions);
        meshFilter.mesh = paintMesh;
    }

    void AddPointToLine(Vector3 newPoint)
    {
        // Mevcut noktaları al
        int currentCount = lineRenderer.positionCount;
        lineRenderer.positionCount = currentCount + 1;

        // Yeni noktayı ekle
        lineRenderer.SetPosition(currentCount, newPoint);
    }

    Mesh GenerateSmoothPaintMesh(Vector3[] positions)
    {
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int i = 0; i < positions.Length; i++)
        {
            // Z eksenini sabit tut
            positions[i].z = zPosition;

            // Boyanın genişliği için bir offset vektörü hesapla (x-y düzleminde)
            Vector3 forward = Vector3.forward; // Z eksenine paralel yön
            Vector3 right = Vector3.Cross(forward, Vector3.up).normalized * paintThickness;

            // Kenarları smooth yapmak için segmentlere böl
            for (int j = 0; j <= smoothness; j++)
            {
                float t = j / (float)smoothness; // Segment oranı (0 ile 1 arasında)
                float angle = t * Mathf.PI; // Yarım çember oluşturmak için açı (0 ile π arasında)

                // Smooth kenar için offset hesapla
                float smoothOffset = Mathf.Sin(angle) * paintHeight;
                Vector3 domeOffset = Vector3.up * smoothOffset;

                // Üst ve alt noktaları hesapla
                Vector3 leftPoint = positions[i] - right + domeOffset;
                Vector3 rightPoint = positions[i] + right + domeOffset;

                vertices.Add(leftPoint);
                vertices.Add(rightPoint);

                // UV koordinatlarını ekle
                uvs.Add(new Vector2(0, i / (float)positions.Length));
                uvs.Add(new Vector2(1, i / (float)positions.Length));

                // Üçgenleri oluştur
                if (i < positions.Length - 1 && j < smoothness)
                {
                    int startIndex = (i * (smoothness + 1) + j) * 2;

                    // Üçgenleri ekle (sol - sağ - aşağı üçgenler)
                    triangles.Add(startIndex);
                    triangles.Add(startIndex + 2);
                    triangles.Add(startIndex + 1);

                    triangles.Add(startIndex + 1);
                    triangles.Add(startIndex + 2);
                    triangles.Add(startIndex + 3);
                }
            }
        }

        // Mesh verilerini ata
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals(); // Normalleri tekrar hesapla (gölgeler için)

        return mesh;
    }
}
