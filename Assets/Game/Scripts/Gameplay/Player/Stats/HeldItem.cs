using UnityEngine;
using Game.Systems.Items;
using Game.Systems.Items.Runtime;

namespace Game.Gameplay.Player
{
    public class HeldItem : MonoBehaviour
    {
        // 旧系统可能会直接改这个
        public ItemDefinition held;

        // 新系统：运行时实例
        public ItemInstance heldInstance;

        public void SetHeld(ItemInstance inst)
        {
            heldInstance = inst;
            held = inst != null ? inst.Definition : null;
        }

        /// <summary>
        /// 兼容旧代码：如果外部直接给 held 赋值，
        /// 这里会补一个实例，保证 RocketRideController 能拿到 InstanceId。
        /// </summary>
        private void LateUpdate()
        {
            if (held == null)
            {
                if (heldInstance != null) heldInstance = null;
                return;
            }

            // held 有值但实例为空 -> 自动补实例
            if (heldInstance == null || heldInstance.Definition != held)
            {
                heldInstance = new ItemInstance(held);
            }
        }
    }
}
