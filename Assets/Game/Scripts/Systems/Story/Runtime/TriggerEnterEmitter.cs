using UnityEngine;

public class EnterTriggerEmitter : MonoBehaviour
{
    public string triggerId;

    private void OnTriggerEnter2D(Collider2D other)
    {
        GameRoot.I.Triggers.Raise(
            new EnterTriggerEvent(other.gameObject, triggerId)
        );
    }
}
