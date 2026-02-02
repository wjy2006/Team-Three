using UnityEngine;
using Game.Gameplay.Player;

namespace Game.Systems.Items.Effects
{
    [CreateAssetMenu(menuName = "Game/Items/Effects/Heal", fileName = "HealEffect")]
    public class HealEffect : ItemEffect
    {
        [Min(1)] public int amount = 10;

        public override bool Apply(ItemUseContext ctx)
        {
            var user=ctx.user;
            if (user == null)
            {
                Debug.LogWarning("[ItemEffect] HealEffect.Apply called with null user");
                return false;
            }

            if (!user.TryGetComponent<PlayerStats>(out var stats))
            {
                Debug.LogWarning($"[ItemEffect] {user.name} has no PlayerStats, cannot heal");
                return false;
            }
            stats.Heal(amount);

            Debug.Log($"[ItemEffect] {user.name} healed +{amount} HP");
            return false;
        }
    }
}
