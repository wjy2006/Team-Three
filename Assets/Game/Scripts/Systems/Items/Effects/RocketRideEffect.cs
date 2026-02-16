using UnityEngine;

namespace Game.Systems.Items
{
    [CreateAssetMenu(menuName = "Game/Items/Effects/Rocket Ride", fileName = "RocketRideEffect")]
    public class RocketRideEffect : ItemEffect
    {
        [Header("Rocket Prefab (must have RocketMountEntity + Rigidbody2D + Collider2D)")]
        public GameObject rocketPrefab;

        [Header("Rocket HP")]
        public int rocketHp = 200;

        [Header("Move")]
        public float accel = 35f;
        public float maxSpeed = 28f;

        [Header("Turn")]
        public float maxTurnDegPerSec = 360f;

        [Header("Explode")]
        public float explodeSpeedThreshold = 16f;
        public float explosionRadius = 3.0f;
        public float damageToOthers = 50f;
        public float damageToPlayer = 19f;

        [Tooltip("爆炸只会伤害这个 LayerMask 里的 IDamageable（不包含玩家，玩家用固定扣 19）")]
        public LayerMask explosionLayer;

        [Header("VFX")]
        public GameObject explosionVfxPrefab;
        public float explosionVfxLife = 1.5f;

        // ✅ 注意：现在不靠 click 使用触发，所以 Apply 可以空实现（不消耗）
        public override bool Apply(ItemUseContext ctx) => false;
    }
}
