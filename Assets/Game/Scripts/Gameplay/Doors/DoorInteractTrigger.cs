using UnityEngine;

public class DoorInteractTrigger : MonoBehaviour
{
    public KeyDoor2D door;
    public DialogueAsset cantOpen;

    private PlayerInputReader input;
    private bool inRange;

    private void Awake()
    {
        if (door == null)
            door = GetComponentInParent<KeyDoor2D>();
    }

    private void Update()
    {
        if (!inRange) return;

        if (input == null)
        {
            input = GameRoot.I != null ? GameRoot.I.playerInput : null;
            if (input == null) return;
        }

        if (GameRoot.I != null && (GameRoot.I.InputLocked || (GameRoot.I.Dialogue != null && GameRoot.I.Dialogue.IsOpen)))
            return;

        if (input.ConsumeInteractDown())
        {
            if (door == null) return;

            bool ok = door.TryOpen();

            if (!ok) GameRoot.I.Dialogue.Open("Door",cantOpen);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        inRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        inRange = false;
    }
}
