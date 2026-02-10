using UnityEngine;

namespace Game.Gameplay.Combat
{
    public struct DamageInfo
    {
        public float amount;
        public GameObject source;
        public Vector2 hitPoint;
        public Vector2 direction;      // 攻击方向
        public float knockbackForce;  // 击退力度（Impulse）
        public KnockbackKind knockbackKind;

        public string kind;

    }
    public enum KnockbackKind
    {
        Hit,        // 受击击退（子弹直击）
        Explosion   // 爆炸击退（AOE径向）
    }

    public interface IDamageable
    {
        void TakeDamage(DamageInfo info);
    }
}
