using UnityEngine;
using System;
using Game.Gameplay.Combat;

namespace Game.Gameplay.Player
{
    public class PlayerStats : MonoBehaviour, IDamageable
    {
        [Header("HP")]
        [SerializeField] private int maxHp = 20;   // 固定20
        [SerializeField] private int hp = 20;

        [Header("Money")]
        [SerializeField] private int money = 0;

        // 对外只读
        public int MaxHp => maxHp;
        public int Hp => hp;
        public int Money => money;

        public bool IsDead => hp <= 0;
        public bool IsFullHp => hp >= maxHp;

        // 当数值变化时通知外界（UI等）
        public event Action OnStatsChanged;

        void Awake()
        {
            hp = Mathf.Clamp(hp, 0, maxHp);
        }

        // ===== HP =====

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || IsDead) return;

            hp -= amount;
            hp = Mathf.Clamp(hp, 0, maxHp);

            OnStatsChanged?.Invoke();
        }

        public void Heal(int amount)
        {
            if (IsDead) return;

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

        // ===== Money =====

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
        public void TakeDamage(DamageInfo info)
        {
            // 这里决定怎么把 float 转成 int
            int amount = Mathf.RoundToInt(info.amount);

            TakeDamage(amount);

            // 可选：击退 / 受击反馈
            // Debug.Log($"Player hit by {info.source?.name}");
        }

    }
}
