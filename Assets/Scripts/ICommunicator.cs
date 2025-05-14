using UnityEngine;

/// <summary>
/// Any agent that can receive messages must implement this.
/// </summary>
public interface ICommunicator
{
    /// <param name="message">The text sent by the speaker.</param>
    /// <param name="from">The Transform of the agent who spoke.</param>
    void ReceiveMessage(string message, Transform from);
}