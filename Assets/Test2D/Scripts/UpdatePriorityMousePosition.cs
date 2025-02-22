using PaintIn2D;
using UnityEngine;

public class UpdatePriorityMousePosition : MonoBehaviour
{
    public Camera mainCamera;
    public CwHitScreen2D CwHitScreen2D;
    public float positionStepY;
    private float _minPriority;

    private void Update()
    {
        Vector3 pos = GetWorldPositionFromMouse();
        int priority = -Mathf.CeilToInt(pos.y / positionStepY);
        priority = Mathf.Max(priority, -2147483640);
        CwHitScreen2D.Priority = priority;
    }

    private Vector3 GetWorldPositionFromMouse()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);
        worldPosition.z = 0;
        return worldPosition;
    }
}
