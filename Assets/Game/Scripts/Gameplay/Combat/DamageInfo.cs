using UnityEngine;

namespace Game.Gameplay.Combat
{
    public struct DamageInfo
    {
        public float amount;
        public GameObject source;     // 伤害来源（开枪者/陷阱等）
        public Vector2 hitPoint;      // 命中点（可选）
        public Vector2 direction;     // 伤害方向（可选，用于击退）
        public string kind;           // 可选： "bullet" "fire" 等
    }

    public interface IDamageable
    {
        void TakeDamage(DamageInfo info);
    }
}
