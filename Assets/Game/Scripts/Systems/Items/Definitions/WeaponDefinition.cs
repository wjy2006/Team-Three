using UnityEngine;

namespace Game.Systems.Items
{
    public enum WeaponFireMode
    {
        SemiAuto,   // 点一下打一发
        Auto        // 按住连发
    }
    [CreateAssetMenu(menuName = "Game/Items/Weapon Definition", fileName = "NewWeapon")]
    public class WeaponDefinition : ItemDefinition
    {
        [Header("Weapon Stats")]
        public float fireRate = 6f;          // 每秒几发
        public float bulletSpeed = 12f;
        public float spreadDegrees = 0f;     // 散射角（0表示无）
        public int pellets = 1;              // 霰弹枪：>1
                                             // WeaponDefinition.cs
        [Header("Recoil Settings")]
        [Tooltip("视觉后坐力距离：枪管向后缩进的最大距离")]
        public float visualRecoilStrength = 0.15f;
        [Tooltip("视觉恢复速度：数值越大回位越快")]
        public float visualRecoilReturnSpeed = 20f;
        [Tooltip("物理后坐力：开火时给玩家施加的反向冲力")]
        public float physicalRecoilForce = 0f;


        public WeaponFireMode fireMode = WeaponFireMode.Auto;
        [Header("Bullet Params")]
        public float damage = 1f;
        public float bulletLifeTime = 2f;

        [Header("Prefabs")]
        public GameObject bulletPrefab;

        [Header("FirePoint")]
        public Vector2 firePointLocal = new Vector2(0.6f, 0f); // 默认枪口在武器本地坐标的位置
        public float fireAngleOffset = 0f; // 只影响子弹发射方向的额外修正（可选）
    }
}
