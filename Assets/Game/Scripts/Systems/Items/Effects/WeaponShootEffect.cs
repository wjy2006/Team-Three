using UnityEngine;

namespace Game.Systems.Items
{
    [CreateAssetMenu(menuName = "Game/Items/Effects/Weapon Shoot Effect", fileName = "WeaponShootEffect")]
    public class WeaponShootEffect : ItemEffect
    {
        public override bool Apply(ItemUseContext ctx)
        {
            if (ctx.item is not WeaponDefinition weapon)
            {
                Debug.LogError("WeaponShootEffect: ctx.item 不是 WeaponDefinition");
                return false;
            }

            if (weapon.bulletPrefab == null)
            {
                Debug.LogError($"WeaponShootEffect: 武器 {weapon.DisplayName} 没有 bulletPrefab");
                return false;
            }

            // 1) 决定生成点：优先用现有 FirePoint（你已经做了 HeldItemVisualController）
            Vector2 spawnPos = ctx.user.transform.position;
            if (ctx.user.TryGetComponent<Gameplay.Player.HeldItemVisualController>(out var visual))
                spawnPos = visual.GetFirePointWorldPos();


            int pellets = Mathf.Max(1, weapon.pellets);
            float spread = weapon.spreadDegrees;

            for (int i = 0; i < pellets; i++)
            {
                float t = pellets == 1 ? 0f : (i / (float)(pellets - 1) - 0.5f); // [-0.5, 0.5]
                float angle = (t * spread) + weapon.fireAngleOffset;
                Vector2 baseDir = ctx.aimDir;

                float randomOffset = Random.Range(-weapon.spreadDegrees, weapon.spreadDegrees);
                Vector2 dir = Rotate(baseDir, randomOffset);


                var go = Instantiate(weapon.bulletPrefab, spawnPos, Quaternion.identity);

                if (go.TryGetComponent<Gameplay.Combat.Bullet2D>(out var bullet))
                {
                    bullet.Init(ctx.user, dir, weapon.bulletSpeed, weapon.damage, weapon.bulletLifeTime);

                }
                else
                {
                    // 兜底：没有 Bullet2D 组件就直接给速度
                    if (go.TryGetComponent<Rigidbody2D>(out var rb)) rb.linearVelocity = dir * weapon.bulletSpeed;
                }

            }

            return false; // 枪不消耗
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cs = Mathf.Cos(rad);
            float sn = Mathf.Sin(rad);
            return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
        }
    }
}
