using UnityEngine;

namespace Game.Systems.Items
{
    [CreateAssetMenu(menuName = "Game/Items/Effects/Rocket Ride", fileName = "RocketRideEffect")]
    public class RocketRideEffect : ItemEffect
    {
        [Header("Rocket Prefab (must have RocketMountEntity + Rigidbody2D + Collider2D + Health2D)")]
        public GameObject rocketPrefab;

        [Header("Rocket HP")]
        public int rocketHp = 200;

        [Header("Move")]
        public float accel = 35f;
        public float maxSpeed = 28f;

        [Header("Turn")]
        public float maxTurnDegPerSec = 360f;

        [Header("Explosion")]
        public float explosionRadius = 3.0f;
        public float damageToOthers = 50f;
        public float damageToPlayer = 19f;

        [Tooltip("爆炸只会伤害这个 LayerMask 里的 IDamageable（不包含玩家，玩家固定扣 19）")]
        public LayerMask explosionLayer;

        [Header("VFX")]
        public GameObject explosionVfxPrefab;
        public float explosionVfxLife = 1.5f;

        [Header("Impact Damage (delta-v)")]
        public float dvThreshold = 6f;        // 低于这个 dv 不扣血
        public float damagePerDv = 8f;        // 每 1 点 dv 造成多少伤害（线性）
        public float maxImpactDamage = 120f;  // 单次撞击最多扣多少（防止极端情况）

        // 现在不靠 click 使用触发，所以 Apply 不做事（不消耗物品）
        public override bool Apply(ItemUseContext ctx) => false;
    }
}
