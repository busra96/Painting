using UnityEngine;
using UnityEngine.EventSystems;

public static class Helper
{
    public static float Remap(float value, float min1, float max1, float min2, float max2)
    {
        return Mathf.Approximately(min1, max1) ? min2 : (value - min1) / (max1 - min1) * (max2 - min2) + min2;
    }

    public static bool PointerPressed(out int id, out Vector2 position)
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase != TouchPhase.Began) continue;
            id = touch.fingerId;
            position = touch.position;
            return true;
        }
        id = -1;
        position = Input.mousePosition;
        return Input.touchCount == 0 && Input.GetMouseButtonDown(0);
    }

    public static bool ReadPointer(int id, out Vector2 position, out bool released)
    {
        position = Input.mousePosition;
        if (id < 0)
        {
            released = !Input.GetMouseButton(0);
            return true;
        }
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.fingerId != id) continue;
            position = touch.position;
            released = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            return true;
        }
        released = true;
        return false;
    }

    public static bool IsOverUI(int id)
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(id);
    }

    public static bool ScreenToPaper(Camera camera, Vector2 screen, float z, out Vector3 world)
    {
        world = default;
        if (camera == null) return false;
        Ray ray = camera.ScreenPointToRay(screen);
        if (!new Plane(Vector3.forward, new Vector3(0, 0, z)).Raycast(ray, out float distance)) return false;
        world = ray.GetPoint(distance);
        return true;
    }

    public static Color SampleSprite(SpriteRenderer renderer, Texture2D texture, Vector2 normalized)
    {
        if (renderer.flipX) normalized.x = 1f - normalized.x;
        if (renderer.flipY) normalized.y = 1f - normalized.y;
        Rect rect = renderer.sprite.rect;
        Texture2D original = renderer.sprite.texture;
        // GetPixelBilinear locates texel zero at UV zero; account for the GPU's half-texel offset.
        // Clamp within the sprite so Repeat wrapping/adjacent atlas sprites cannot bleed into edges.
        float u = (rect.x + Mathf.Clamp(normalized.x * rect.width - 0.5f, 0, rect.width - 1)) / original.width;
        float v = (rect.y + Mathf.Clamp(normalized.y * rect.height - 0.5f, 0, rect.height - 1)) / original.height;
        Color color = texture.GetPixelBilinear(u, v) * renderer.color;
        return Color.Lerp(Color.white, new Color(color.r, color.g, color.b, 1f), color.a);
    }
}
