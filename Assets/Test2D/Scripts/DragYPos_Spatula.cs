using System;
using UnityEngine;

public class DragYPos_Spatula : MonoBehaviour
{
    public Camera mainCamera;
    public Spatula Spatula;
    public LayerMask DragLayer;
    private bool isDragging = false;
    
    public float smoothSpeed = 10f; // Hareketin hızını kontrol eden değer
    private Transform selectedObject;
    private float lastMouseY;

    private bool IsStop;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, DragLayer))
            {
                if (hit.collider.CompareTag("SpatulaDrag"))
                {
                    isDragging = true;
                    selectedObject = Spatula.transform;
                    //selectedObject = hit.transform;
                    lastMouseY = Input.mousePosition.y;
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            selectedObject = null;
        }

        if (isDragging && selectedObject != null && !IsStop)
        {
            DragObject();
        }
    }

    void DragObject()
    {
        float mouseDeltaY = Input.mousePosition.y - lastMouseY; // Mouse'un ne kadar aşağı indiği
        lastMouseY = Input.mousePosition.y; // Yeni pozisyonu kaydet

        if (mouseDeltaY < 0) // Sadece aşağı hareket etsin
        {
            Vector3 targetPosition = selectedObject.position + new Vector3(0, mouseDeltaY * 0.02f, 0);
            selectedObject.position = Vector3.Lerp(selectedObject.position, targetPosition, Time.deltaTime * smoothSpeed);

            if (selectedObject.position.y <= -6)
            {
                IsStop = true;
            }
        }
    }
}
