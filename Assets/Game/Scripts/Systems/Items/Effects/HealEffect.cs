using UnityEngine;

namespace Assets.Game.Scripts.Systems.Items.Effects
{
    [CreateAssetMenu(menuName = "Game/Items/Effects/Heal", fileName = "HealEffect")]
    public class HealEffect : ItemEffect
    {
        public int amount = 10;

        public override bool Apply(GameObject user)
        {
            Debug.Log($"[ItemEffect] {user.name} healed +{amount} HP");
            return true; // Undertale 的消耗品：用完消失
        }
    }
}
