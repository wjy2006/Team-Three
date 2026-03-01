using UnityEngine;

namespace Game.Gameplay.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class KnockbackReceiver : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private float massScale = 1f;

        private PlayerInputReader input;
        private bool isPlayer;

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            
            // ✅ 检查自己是不是玩家
            isPlayer = CompareTag("Player");
            
            // 只有玩家才需要获取输入
            if (isPlayer)
                input = GetComponent<PlayerInputReader>();
        }

        public void ApplyKnockback(Vector2 direction, float force)
        {
            // ✅ 核心修复：只有【是玩家】且【按住取消键】时才无视击退
            if (isPlayer && input != null && input.CancelHeld)
            {
                // 可以加个小粒子或者音效反馈，代表“防御成功”
                return; 
            }

            if (force <= 0) return;

            direction = direction.sqrMagnitude < 0.0001f
                ? Vector2.right
                : direction.normalized;

            rb.AddForce(direction * force / massScale, ForceMode2D.Impulse);
        }
    }
}