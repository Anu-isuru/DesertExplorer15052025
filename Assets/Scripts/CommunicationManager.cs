using UnityEngine;
using TMPro;

public class CommunicationManager : MonoBehaviour
{
    public static CommunicationManager Instance { get; private set; }
    [Tooltip("World-space prefab with a TextMeshProUGUI for speech bubbles")]
    public GameObject speechBubblePrefab;
    public float bubbleDuration = 2f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Shows a bubble above the speaker, then delivers message to the listener.
    /// </summary>
    public void SendMessage(
        Transform speaker,
        ICommunicator listener,
        string message

    )
    {
        Debug.Log($"[CommMgr] Sending “{message}” from {speaker.name} to {listener}");

        // 1) Visual
        var bubble = Instantiate(
            speechBubblePrefab,
            speaker.position + Vector3.up * 1.5f,
            Quaternion.identity,
            speaker
        );
        bubble.GetComponentInChildren<TextMeshProUGUI>().text = message;
        Destroy(bubble, bubbleDuration);

        // 2) Logical
        listener.ReceiveMessage(message, speaker);
    }
}
