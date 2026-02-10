using UnityEngine;

namespace Game.Gameplay.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class KnockbackReceiver : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private float massScale = 1f; // 可做抗性

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
        }

        public void ApplyKnockback(Vector2 direction, float force)
        {
            if (force <= 0) return;

            direction = direction.sqrMagnitude < 0.0001f
                ? Vector2.right
                : direction.normalized;

            rb.AddForce(direction * force / massScale, ForceMode2D.Impulse);
        }
    }
}
