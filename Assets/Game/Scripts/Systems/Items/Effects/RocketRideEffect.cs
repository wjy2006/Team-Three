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

        [Tooltip("Explosion only damages IDamageable targets in this layer mask. Player self-damage still uses damageToPlayer.")]
        public LayerMask explosionLayer;

        [Header("VFX")]
        public GameObject explosionVfxPrefab;
        public float explosionVfxLife = 1.5f;

        [Header("Audio")]
        public AudioClip flyingLoopSfx;
        public AudioClip explosionSfx;

        [Header("Impact Damage (delta-v)")]
        public float dvThreshold = 6f;
        public float damagePerDv = 8f;
        public float maxImpactDamage = 120f;

        public override bool Apply(ItemUseContext ctx) => false;
    }
}
