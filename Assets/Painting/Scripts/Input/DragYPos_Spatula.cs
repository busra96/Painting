using System;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class DragYPos_Spatula : MonoBehaviour
{
    public Camera mainCamera;
    public Spatula Spatula;
    public LayerMask DragLayer;
    public float smoothSpeed = 10f;
    public GridDataComparer gridDataComparer;
    [SerializeField] private float finishY = -6f;
    [SerializeField, Min(0.1f)] private float maximumSpeed = 7f;
    public event Action OnCompleted;
    public float Progress => Mathf.InverseLerp(startY, finishY, Spatula.transform.position.y);
    public bool IsComplete { get; private set; }

    private bool isDragging;
    private int pointerId;
    private float previousPointerY;
    private float targetY;
    private float startY;

    private void OnEnable()
    {
        if (Spatula == null) return;
        startY = targetY = Spatula.transform.position.y;
        IsComplete = false;
        isDragging = false;
    }

    private void Update()
    {
        if (IsComplete || Spatula == null || mainCamera == null) return;
        if (!isDragging && Helper.PointerPressed(out int id, out Vector2 screen) && !Helper.IsOverUI(id))
        {
            Ray ray = mainCamera.ScreenPointToRay(screen);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, DragLayer) &&
                hit.collider.CompareTag("SpatulaDrag") &&
                Helper.ScreenToPaper(mainCamera, screen, Spatula.transform.position.z, out Vector3 point))
            {
                pointerId = id;
                previousPointerY = point.y;
                targetY = Spatula.transform.position.y;
                isDragging = true;
            }
        }
        if (isDragging)
        {
            if (!Helper.ReadPointer(pointerId, out Vector2 current, out bool released)) StopDragging();
            else
            {
                if (Helper.ScreenToPaper(mainCamera, current, Spatula.transform.position.z, out Vector3 world))
                {
                    float delta = world.y - previousPointerY;
                    previousPointerY = world.y;
                    if (delta < 0) targetY = Mathf.Max(finishY, targetY + delta);
                }
                if (released) StopDragging();
            }
        }
        if (ImpastoPaintSurface.Active != null) Advance(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (ImpastoPaintSurface.Active == null) Advance(Time.fixedDeltaTime);
    }

    private void Advance(float deltaTime)
    {
        if (IsComplete || Spatula == null) return;
        Vector3 position = Spatula.transform.position;
        float eased = Mathf.Lerp(position.y, targetY, 1f - Mathf.Exp(-Mathf.Max(1, smoothSpeed) * deltaTime));
        float load = ImpastoPaintSurface.Active != null ? ImpastoPaintSurface.Active.BladeLoad : 0;
        float resistance = Mathf.Lerp(1f, 0.78f, Mathf.Clamp01(load / 0.5f));
        position.y = Mathf.MoveTowards(position.y, eased, maximumSpeed * resistance * deltaTime);
        if (targetY <= finishY && position.y - finishY < 0.015f) position.y = finishY;
        Spatula.transform.position = position;
        if (position.y > finishY) return;
        IsComplete = true;
        isDragging = false;
        // The UI owns result presentation; standalone test scenes still get a score.
        if (OnCompleted != null) OnCompleted.Invoke();
        else if (gridDataComparer != null) gridDataComparer.Test();
    }

    private void StopDragging()
    {
        isDragging = false;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused)
        {
            StopDragging();
            if (Spatula != null) targetY = Spatula.transform.position.y;
        }
    }
}
