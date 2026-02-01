using UnityEngine;

namespace Game.Gameplay.Player
{
    public class PlayerStats : MonoBehaviour
    {
        [Min(1)] public int maxHp = 20;
        [Min(0)] public int hp = 20;
        [Min(0)] public int money = 0;
    }
}
