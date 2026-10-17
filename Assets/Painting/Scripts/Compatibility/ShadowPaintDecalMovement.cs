using UnityEngine;

public class ShadowPaintDecalMovement : MonoBehaviour
{
    public float yOffset = -0.2f; // Sadece Y ekseninde offset
    public float smoothSpeed = 10f; // Hareketin yumuşaklığını ayarlar
    void Update()
    {
        if (Input.GetMouseButton(0)) // Sadece sol tık basılıyken takip et
        {
           
            FollowMouseWithYOffset();
        }
    }

    void FollowMouseWithYOffset()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f; // Kameradan mesafeyi belirle (Z değeri sahneye göre ayarlanabilir)

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.y += yOffset; // Sadece Y ekseninde offset uygula

        transform.position = Vector3.Lerp(transform.position, worldPos, smoothSpeed * Time.deltaTime);
    }
}
