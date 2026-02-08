using UnityEngine;

namespace Game.Systems.Items.Effects
{
    [CreateAssetMenu(menuName = "Game/Items/Effects/None", fileName = "NoneEffect")]
    public class NoneEffect : ItemEffect
    {
        public override bool Apply(ItemUseContext ctx)
        {
            var user=ctx.user;
            if (user == null)
            {
                Debug.LogWarning("[ItemEffect] NoneEffect.Apply called with null user");
                return false;
            }
            return false;
        }
    }
}
