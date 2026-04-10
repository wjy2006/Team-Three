using UnityEngine;
using Game.Gameplay.Player;
using Game.Gameplay.Combat;

namespace Game.Systems.Items.Effects
{
    [CreateAssetMenu(menuName = "Game/Items/Effects/Poison", fileName = "PoisonEffect")]
    public class PoisonEffect : ItemEffect
    {
        [Min(1)] public int amount = 5;

        public override bool Apply(ItemUseContext ctx)
        {
            var user = ctx.user;
            if (user == null)
            {
                Debug.LogWarning("[ItemEffect] PoisonEffect.Apply called with null user");
                return false;
            }

            if (!user.TryGetComponent<PlayerStats>(out var stats))
            {
                Debug.LogWarning($"[ItemEffect] {user.name} has no PlayerStats, cannot take poison damage");
                return false;
            }

            var info = new DamageInfo
            {
                amount = amount,
                source = user,
                hitPoint = user.transform.position,
                direction = Vector2.zero,
                knockbackForce = 0f,
                knockbackKind = KnockbackKind.None,
                kind = "poison"
            };

            stats.TakeDamage(info);
            Debug.Log($"[ItemEffect] {user.name} took poison damage {amount} HP");
            return true;
        }
    }
}
