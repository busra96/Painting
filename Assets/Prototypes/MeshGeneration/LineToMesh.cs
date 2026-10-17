using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineToMesh : MonoBehaviour
{
    public GameObject spherePrefab; // Küre prefabı
    public Material lineMaterial;   // Çizgiye uygulanacak materyal
    public float sphereSpacing = 0.5f; // Küreler arasındaki mesafe
    public float lineRadius = 0.2f;    // Çizginin/tüpün yarıçapı
    public float zPosition = 10f;      // Z eksenindeki sabit değer

    private List<Vector3> positions = new List<Vector3>(); // Kürelerin pozisyonları
    private MeshFilter meshFilter; // Mesh için kullanılacak filter

   void Start()
    {
        // MeshFilter ekle
        GameObject meshObject = new GameObject("LineMesh");
        meshFilter = meshObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = meshObject.AddComponent<MeshRenderer>();
        renderer.material = lineMaterial;
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

                // Eğer araya bir küre koymamışsak, küre oluştur
                if (positions.Count == 0 || Vector3.Distance(positions[positions.Count - 1], position) > sphereSpacing)
                {
                    positions.Add(position);
                    Instantiate(spherePrefab, position, Quaternion.identity);

                    // Mesh'i güncelle
                    UpdateMesh();
                }
            }
        }
    }

    void UpdateMesh()
    {
        if (positions.Count < 2) return; // En az 2 nokta gerek

        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        int segments = 12; // Tüpün detay seviyesi (çemberin kaç segmentten oluşacağı)

        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 current = positions[i];
            Vector3 forward = Vector3.forward;

            // Eğer bir sonraki nokta varsa, forward vektörünü hesapla
            if (i < positions.Count - 1)
                forward = (positions[i + 1] - current).normalized;

            // Eğer bir önceki nokta varsa, öncekiyle birleştir
            if (i > 0)
                forward = ((positions[i] - positions[i - 1]).normalized + forward).normalized;

            // Sağlam bir düzlem oluşturmak için forward vektörüne dik bir vektör al
            Vector3 right = Vector3.Cross(forward, Vector3.forward).normalized;

            // Çember boyunca vertex’leri hesapla
            for (int j = 0; j <= segments; j++)
            {
                float angle = (j / (float)segments) * Mathf.PI * 2;
                Vector3 offset = right * Mathf.Cos(angle) * lineRadius + Vector3.up * Mathf.Sin(angle) * lineRadius;
                vertices.Add(current + offset);
                uvs.Add(new Vector2(j / (float)segments, i / (float)positions.Count));
            }
        }

        // Üçgenleri oluştur
        for (int i = 0; i < positions.Count - 1; i++)
        {
            for (int j = 0; j < segments; j++)
            {
                int start = i * (segments + 1) + j;
                int next = start + segments + 1;

                triangles.Add(start);
                triangles.Add(next + 1);
                triangles.Add(next);

                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(next + 1);
            }
        }

        // Mesh'e verileri ata
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals(); // Normalleri hesapla (smooth görünüm için)

        // Mesh'i ata
        meshFilter.mesh = mesh;
    }
}
