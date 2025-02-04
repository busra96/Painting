using System;
using System.Collections;
using System.Collections.Generic;
using PaintCore;
using UnityEngine;

public class ShadowOffset : MonoBehaviour
{
    public CwPointerMouse PointerMouse;
    public float distanceThreshold = 10f; // Minimum hareket mesafesi

    private Vector3 lastMousePosition;

    private void Update()
    {
        Vector3 currentMousePosition = Input.mousePosition; // Şu anki mouse pozisyonu
        Vector3 delta = currentMousePosition - lastMousePosition; // Hareket farkı

        if (PointerMouse != null)
        {
            float newOffsetY = PointerMouse.offset.y;

            if (Mathf.Abs(delta.x) > distanceThreshold) // Sağa veya sola hareket varsa
            {
                newOffsetY = 20; // Her durumda 20 olarak kalacak
            }

            if (delta.y < -distanceThreshold) // Sadece yukarıdan aşağı hareket ettiğinde
            {
                newOffsetY = 0; // Aşağı hareket ediyorsa 0 yap
            }

            PointerMouse.offset = new Vector3(
                0,
                newOffsetY,
               0
            );
        }

        lastMousePosition = currentMousePosition; // Güncel pozisyonu sakla
    }
}
