using UnityEngine;

public class EnemyBobbing : MonoBehaviour
{
    [Tooltip("How far up/down from start position")]
    public float amplitude = 1f;
    [Tooltip("How fast to bob")]
    public float frequency = 1f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // sine‐wave offset
        float xOffset = Mathf.Sin(Time.time * frequency) * amplitude;
        transform.position = startPos + Vector3.up * xOffset;
    }
}
