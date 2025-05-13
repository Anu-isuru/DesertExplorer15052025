using UnityEngine;

[RequireComponent(typeof(CompositeCollider2D))]
public class MapBoundsProvider : MonoBehaviour
{
    [HideInInspector] public float minX, maxX, minY, maxY;

    void Awake()
    {
        // Get the CompositeCollider2D attached to this GameObject
        var comp = GetComponent<CompositeCollider2D>();
        // Read its world‐space bounds
        var b = comp.bounds;

        // b.min and b.max are Vector3s with the corners of the box
        minX = b.min.x;
        minY = b.min.y;
        maxX = b.max.x;
        maxY = b.max.y;

        Debug.Log($"Map bounds: X[{minX:F2}, {maxX:F2}]  Y[{minY:F2}, {maxY:F2}]");
    }
}
