using UnityEngine;
using Game.Systems.Items;

namespace Game.Gameplay.Player
{
    [RequireComponent(typeof(HeldItem))]
    public class HeldItemVisualController : MonoBehaviour
    {
        public enum RotationMode
        {
            RotateWithAim,   // 自身随方向旋转
            FixedUp          // 固定朝上
        }

        [Header("Refs")]
        [SerializeField] private HeldItem heldItem;               // 你的 HeldItem 组件
        [SerializeField] private PlayerInputReader input;         // 读 Point
        [SerializeField] private SpriteRenderer spriteRenderer;   // 显示用
        [SerializeField] private Transform firePoint;
        [SerializeField] private Vector2 defaultFirePointLocal = new Vector2(0.6f, 0f); // 没绑时的兜底
        private ItemDefinition lastItem;




        [Tooltip("把相机的 z 平面投到 0，2D 正交一般这样就稳")]
        [SerializeField] private bool useMainCamera = true;

        private Camera cam;

        private void Awake()
        {
            if (firePoint == null && spriteRenderer != null)
            {
                // 尝试自动找子物体 FirePoint（你按我说建了就能自动找到）
                var t = spriteRenderer.transform.Find("FirePoint");
                if (t != null) firePoint = t;
            }
            if (firePoint != null && firePoint.localPosition == Vector3.zero)
            {
                firePoint.localPosition = defaultFirePointLocal;
            }

            if (heldItem == null) heldItem = GetComponent<HeldItem>();
            cam = Camera.main;
        }

        private void Update()
        {
            // 延迟拿 input（兼容你的 GameRoot 初始化顺序）
            if (input == null)
            {
                if (GameRoot.I != null) input = GameRoot.I.playerInput;
                if (input == null) return;
            }

            if (spriteRenderer == null) return;
            if (heldItem == null) return;

            var item = heldItem.held;

            // 没手持：隐藏
            if (item != null)
            {
                // 有手持：显示并设置 sprite
                spriteRenderer.enabled = true;

                // 这里用你 ItemDefinition 的 WorldSprite（按上面建议加的字段）
                var s = item.Visual.worldSprite;
                if (spriteRenderer.sprite != s) spriteRenderer.sprite = s;

                if (item != lastItem)
                {
                    lastItem = item;
                    ApplyItemFirePoint(item);
                }

                // 计算鼠标世界坐标
                if (useMainCamera)
                {
                    Vector3 wp3 = cam.ScreenToWorldPoint(
                        new Vector3(input.PointerPos.x, input.PointerPos.y, -cam.transform.position.z)
                    );
                    Vector2 mouseWorld = (Vector2)wp3;

                    Vector2 origin = transform.position;
                    Vector2 toMouse = mouseWorld - origin;

                    // ✅ 归一化方向：holdDistance 才是真正的“距离=1”
                    Vector2 dir;
                    if (toMouse.sqrMagnitude < 0.0001f)
                        dir = Vector2.up;
                    else
                        dir = toMouse.normalized;

                    // ✅ 位置：玩家身边 holdDistance
                    float dist = item.Visual.holdDistance;
                    Vector3 pos = (Vector3)(origin + dir * dist);
                    pos.z = item.Visual.z;
                    spriteRenderer.transform.position = pos;

                    // ✅ 旋转：用 dir，而不是重复算 (mouseWorld - origin)
                    if (item.Visual.rotationMode == ItemVisualRotationMode.RotateWithAim)
                    {
                        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                        angle += item.Visual.defaultAngleOffset;
                        spriteRenderer.transform.rotation = Quaternion.Euler(0, 0, angle);
                    }
                    else
                    {
                        spriteRenderer.transform.rotation = Quaternion.Euler(0, 0, item.Visual.defaultAngleOffset);
                    }


                }
            }
            else
            {
                spriteRenderer.enabled = false;
                return;
            }
        }
        public Vector2 GetFirePointWorldPos()
        {
            if (firePoint != null) return firePoint.position;
            return (Vector2)spriteRenderer.transform.position; // 兜底：用武器中心
        }
        private void ApplyItemFirePoint(ItemDefinition item)
        {
            if (spriteRenderer == null) return;

            // ✅ 确保 firePoint 引用存在（没拖的话就按名字找）
            if (firePoint == null)
            {
                var t = spriteRenderer.transform.Find("FirePoint");
                if (t != null) firePoint = t;
            }

            if (firePoint == null)
            {
                Debug.LogWarning("HeldItemVisualController: 找不到 FirePoint（请在 HeldSprite 下建一个名为 FirePoint 的子物体，或手动拖引用）");
                return;
            }

            // ✅ 只有 WeaponDefinition 才有 firePointLocal
            if (item is WeaponDefinition weapon)
            {
                firePoint.localPosition = weapon.firePointLocal;
                Debug.Log($"[FirePoint] Applied {weapon.DisplayName} firePointLocal={weapon.firePointLocal}");
            }
        }

    }
}
