using UnityEngine;
using System;
using Game.Gameplay.Combat;

namespace Game.Gameplay.Player
{
    public class PlayerStats : MonoBehaviour, IDamageable, IHealthView
    {
        [Header("HP")]
        [SerializeField] private int maxHp = 20;
        [SerializeField] private int hp = 20;

        [Header("Money")]
        [SerializeField] private int money = 0;

        public int MaxHp => maxHp;
        public int Hp => hp;
        public float Current => hp;
        public float Max => maxHp;

        public int Money => money;

        public bool IsDead => hp <= 0;
        public bool IsFullHp => hp >= maxHp;

        public event Action OnStatsChanged;
        public event Action<DamageInfo> OnDamaged;

        private void Awake()
        {
            hp = Mathf.Clamp(hp, 0, maxHp);
        }

        // ===============================
        // 统一伤害入口（只保留这个）
        // ===============================
        public void TakeDamage(DamageInfo info)
        {
            if (IsDead) return;
            if (info.amount <= 0f) return;

            int amount = Mathf.RoundToInt(info.amount);

            hp -= amount;
            hp = Mathf.Clamp(hp, 0, maxHp);
            var knock = GetComponent<KnockbackReceiver>();
            if (knock != null)
            {
                knock.ApplyKnockback(info.direction, info.knockbackForce);
            }

            OnDamaged?.Invoke(info);     // ⭐ 把完整 info 传出去
            OnStatsChanged?.Invoke();
            GameRoot.I.Triggers.Raise(new DamagedEvent(gameObject, info));

            if (hp <= 0)
            {
                Die(info);
            }
        }

        private void Die(DamageInfo info)
        {
            Debug.Log($"Player died. killer={info.source.name}");

            // 以后可以加：
            // 禁用控制
            // 播放死亡动画
            // 切场景
        }

        // ===============================
        // 治疗
        // ===============================
        public void Heal(int amount)
        {
            if (IsDead) return;
            if (amount <= 0) return;

            hp += amount;
            hp = Mathf.Clamp(hp, 0, maxHp);

            OnStatsChanged?.Invoke();
        }

        public void FullHeal()
        {
            if (hp == maxHp) return;

            hp = maxHp;
            OnStatsChanged?.Invoke();
        }

        // ===============================
        // 金钱
        // ===============================
        public void AddMoney(int amount)
        {
            if (amount <= 0) return;

            money += amount;
            OnStatsChanged?.Invoke();
        }

        public bool TrySpendMoney(int amount)
        {
            if (amount <= 0) return false;
            if (money < amount) return false;

            money -= amount;
            OnStatsChanged?.Invoke();
            return true;
        }
    }
}
