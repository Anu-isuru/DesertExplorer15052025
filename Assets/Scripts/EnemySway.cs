using UnityEngine;

public class EnemySway : MonoBehaviour
{
    [Tooltip("Half the total left/right distance")]
    public float amplitude = 1.5f;
    [Tooltip("Oscillation speed")]
    public float frequency = 1f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // Compute a sine‐wave offset on X
        float xOffset = Mathf.Sin(Time.time * frequency) * amplitude;
        // Apply it to the original position
        transform.position = startPos + Vector3.right * xOffset;
    }
}
