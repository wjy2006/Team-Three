using UnityEngine;
using Assets.Game.Scripts.Systems.Items;

namespace Game.Gameplay.Player
{
    public class HeldItem : MonoBehaviour
    {
        public ItemDefinition held; // 手上拿的东西（可为空）
    }
}
