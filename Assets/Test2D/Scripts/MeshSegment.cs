using UnityEngine;

public class MeshSegment : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Spatula")
        {
            Destroy(gameObject);
        }
    }
}
