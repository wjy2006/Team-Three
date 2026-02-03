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
                    if (cam == null) cam = Camera.main;
                    if (cam == null) return;

                    Vector3 wp3 = cam.ScreenToWorldPoint(new Vector3(input.PointerPos.x, input.PointerPos.y, -cam.transform.position.z));
                    Vector2 mouseWorld = (Vector2)wp3;

                    // 计算方向
                    Vector2 dir = mouseWorld - (Vector2)transform.position;

                    if ((mouseWorld - (Vector2)transform.position).sqrMagnitude < 0.0001f)
                        dir = Vector2.up;
                    else
                        (mouseWorld - (Vector2)transform.position).Normalize();

                    // 位置：玩家身边 distance=1
                    float dist = item.Visual.holdDistance;
                    Vector3 pos = transform.position + (Vector3)((mouseWorld - (Vector2)transform.position) * dist);
                    pos.z = -5f; // ⭐ 固定Z层
                    spriteRenderer.transform.position = pos;


                    if (item.Visual.rotationMode == ItemVisualRotationMode.RotateWithAim)
                    {
                        float angle = Mathf.Atan2((mouseWorld - (Vector2)transform.position).y, (mouseWorld - (Vector2)transform.position).x) * Mathf.Rad2Deg;
                        angle += item.Visual.defaultAngleOffset;
                        spriteRenderer.transform.rotation = Quaternion.Euler(0, 0, angle);
                    }
                    else // FixedUp
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
        private void ApplyItemFirePoint1(ItemDefinition item)
        {
            if (firePoint == null) return;

            // 没拿武器：可以恢复默认（可选）
            if (item == null)
                return;

            // 只有武器才有 firePointLocal
            if (item is WeaponDefinition weapon)
            {
                firePoint.localPosition = weapon.firePointLocal;
            }
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
            if (item is Game.Systems.Items.WeaponDefinition weapon)
            {
                firePoint.localPosition = weapon.firePointLocal;
                Debug.Log($"[FirePoint] Applied {weapon.DisplayName} firePointLocal={weapon.firePointLocal}");
            }
        }

    }
}
