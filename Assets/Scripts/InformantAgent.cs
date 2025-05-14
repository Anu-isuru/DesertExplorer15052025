using UnityEngine;

public class InformantAgent : MonoBehaviour
{
    [Tooltip("Drag your Water GameObject here")]
    public Transform waterSource;

    bool hasSpoken = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        // 1) Did the explorer arrive?
        var listener = other.GetComponent<ICommunicator>();
        if (hasSpoken || listener == null) return;

        Debug.Log($"[Informant] Collided with {other.name}");
        // 2) Build a correct “Water at x,y” string
        var pos = waterSource.position;
        string msg = $"Water at {pos.x:F1},{pos.y:F1}";
        // 3) Send it!
        CommunicationManager.Instance.SendMessage(transform, listener, msg);
        hasSpoken = true;
    }
}
