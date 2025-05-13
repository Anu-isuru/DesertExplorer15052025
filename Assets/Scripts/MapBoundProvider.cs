using UnityEngine;

[RequireComponent(typeof(CompositeCollider2D))]
public class MapBoundsProvider : MonoBehaviour
{
    [HideInInspector] public float minX, maxX, minY, maxY;

    void Awake()
    {
        var comp = GetComponent<CompositeCollider2D>();
        var b = comp.bounds;

        minX = b.min.x;
        minY = b.min.y;
        maxX = b.max.x;
        maxY = b.max.y;

        Debug.Log($"Map bounds: X[{minX:F2}, {maxX:F2}]  Y[{minY:F2}, {maxY:F2}]");
    }
}
