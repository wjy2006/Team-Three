using UnityEngine;

namespace Game.Gameplay.Combat
{
    public class Health2D : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        public float maxHp = 10f;
        public float hp = 10f;

        [Header("Options")]
        public bool destroyOnDeath = true;

        private void Awake()
        {
            hp = Mathf.Clamp(hp, 0, maxHp);
            if (hp <= 0) hp = maxHp; // 第一次放进场景默认满血
        }

        public void TakeDamage(DamageInfo info)
        {
            if (info.amount <= 0) return;

            hp -= info.amount;
            Debug.Log($"{name} took {info.amount} damage from {(info.source ? info.source.name : "unknown")}  hp={hp}/{maxHp}");

            if (hp <= 0)
            {
                Die(info);
            }
        }

        private void Die(DamageInfo info)
        {
            Debug.Log($"{name} died. killer={(info.source ? info.source.name : "unknown")}");

            // 这里以后可以播动画、掉落、禁用控制等
            if (destroyOnDeath)
                Destroy(gameObject);
        }
    }
}
