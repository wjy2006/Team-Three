using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInputReader))]
public class TopDownMove2D : MonoBehaviour
{
    public float moveSpeed = 5f;
    public bool canMove = true;

    private Rigidbody2D rb;
    private PlayerInputReader input;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<PlayerInputReader>();
    }

    private void FixedUpdate()
    {
        if (!canMove) return;
        if (GameRoot.I != null && GameRoot.I.InputLocked) return;

        Vector2 move = input != null ? input.Move : Vector2.zero;
        if (move.sqrMagnitude < 0.0001f) return;

        rb.MovePosition(rb.position + move.normalized * moveSpeed * Time.fixedDeltaTime);
    }
}
