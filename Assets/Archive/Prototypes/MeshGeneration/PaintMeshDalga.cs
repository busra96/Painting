using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PaintMeshDalga : MonoBehaviour
{
     public Material paintMaterial;  // Boya için kullanılacak materyal
    public float paintThickness = 0.5f; // Boyanın genişliği
    public float paintHeight = 0.2f;    // Boyanın yüksekliği (kabarıklık)
    public float zPosition = 10f;       // Z eksenindeki sabit değer
    public float waveIntensity = 0.05f; // Dalgalanma yoğunluğu
    public float waveFrequency = 5f;    // Dalgalanma sıklığı (daha yüksek = daha sık dalgalar)

    private LineRenderer lineRenderer;  // Çizim için kullanılan LineRenderer
    private MeshFilter meshFilter;      // Boya mesh'i için MeshFilter

    void Start()
    {
        // LineRenderer bileşenini al
        lineRenderer = GetComponent<LineRenderer>();

        // Boya mesh'i için yeni bir GameObject oluştur
        GameObject paintMeshObject = new GameObject("RaisedPaintMesh");
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
        Mesh paintMesh = GenerateRaisedPaintMesh(positions);
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

    Mesh GenerateRaisedPaintMesh(Vector3[] positions)
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

            // Dalgalı kabarıklık hesaplaması
            float wave = Mathf.PerlinNoise(i * waveFrequency, 0) * waveIntensity;

            // Üst yüzey noktalarını (su damlası etkisi için) yarım küre eğrisiyle hesapla
            float domeCurve = Mathf.Sin(i / (float)positions.Length * Mathf.PI);
            Vector3 domeOffset = Vector3.up * (paintHeight * domeCurve + wave);

            // Üst ve alt yüzey noktaları
            Vector3 leftPointTop = positions[i] - right + domeOffset;
            Vector3 rightPointTop = positions[i] + right + domeOffset;
            Vector3 leftPointBottom = positions[i] - right;
            Vector3 rightPointBottom = positions[i] + right;

            // Vertex'leri ekle
            vertices.Add(leftPointTop);     // Üst sol
            vertices.Add(rightPointTop);    // Üst sağ
            vertices.Add(leftPointBottom);  // Alt sol
            vertices.Add(rightPointBottom); // Alt sağ

            // UV'leri ekle
            uvs.Add(new Vector2(0, i / (float)positions.Length)); // Üst sol UV
            uvs.Add(new Vector2(1, i / (float)positions.Length)); // Üst sağ UV
            uvs.Add(new Vector2(0, i / (float)positions.Length)); // Alt sol UV
            uvs.Add(new Vector2(1, i / (float)positions.Length)); // Alt sağ UV

            // Üçgenleri oluştur
            if (i < positions.Length - 1)
            {
                int startIndex = i * 4;

                // Üst yüzey üçgenleri
                triangles.Add(startIndex);
                triangles.Add(startIndex + 4);
                triangles.Add(startIndex + 1);

                triangles.Add(startIndex + 1);
                triangles.Add(startIndex + 4);
                triangles.Add(startIndex + 5);

                // Alt yüzey üçgenleri
                triangles.Add(startIndex + 2);
                triangles.Add(startIndex + 6);
                triangles.Add(startIndex + 3);

                triangles.Add(startIndex + 3);
                triangles.Add(startIndex + 6);
                triangles.Add(startIndex + 7);

                // Yan yüzey üçgenleri
                triangles.Add(startIndex);
                triangles.Add(startIndex + 2);
                triangles.Add(startIndex + 4);

                triangles.Add(startIndex + 4);
                triangles.Add(startIndex + 2);
                triangles.Add(startIndex + 6);

                triangles.Add(startIndex + 1);
                triangles.Add(startIndex + 5);
                triangles.Add(startIndex + 3);

                triangles.Add(startIndex + 3);
                triangles.Add(startIndex + 5);
                triangles.Add(startIndex + 7);
            }
        }

        // Mesh verilerini ata
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }
}
