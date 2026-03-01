using UnityEngine;
using Game.Systems.Items;

namespace Game.Gameplay.Player
{
    [RequireComponent(typeof(HeldItem))]
    public class HeldItemVisualController : MonoBehaviour
    {
        public enum RotationMode { RotateWithAim, FixedUp }

        [Header("Refs")]
        [SerializeField] private HeldItem heldItem;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Vector2 defaultFirePointLocal = new Vector2(0.6f, 0f);
        
        [Header("Settings")]
        [SerializeField] private bool useMainCamera = true;

        private ItemDefinition lastItem;
        private Camera cam;
        
        // ✨ 新增：后坐力偏移变量
        private float currentRecoilOffset;

        private void Awake()
        {
            if (firePoint == null && spriteRenderer != null)
            {
                var t = spriteRenderer.transform.Find("FirePoint");
                if (t != null) firePoint = t;
            }
            if (firePoint != null && firePoint.localPosition == Vector3.zero)
                firePoint.localPosition = defaultFirePointLocal;

            if (heldItem == null) heldItem = GetComponent<HeldItem>();
            cam = Camera.main;
        }

        // ✨ 新增：供外部调用的开火后坐力接口
        public void ApplyVisualRecoil(float strength)
        {
            // 累加偏移，但限制最大值（例如最大不超过强度的2倍），防止连发太快枪飞了
            currentRecoilOffset = Mathf.Clamp(currentRecoilOffset + strength, 0, strength * 2.5f);
        }

        private void Update()
        {
            if (input == null)
            {
                if (GameRoot.I != null) input = GameRoot.I.playerInput;
                if (input == null) return;
            }

            var item = heldItem.held;

            // --- 1. 处理后坐力回位逻辑 ---
            if (currentRecoilOffset > 0)
            {
                float returnSpeed = 20f; // 兜底回弹速度
                if (item is WeaponDefinition weapon) returnSpeed = weapon.visualRecoilReturnSpeed;
                
                // 每一帧线性减少偏移量
                currentRecoilOffset = Mathf.MoveTowards(currentRecoilOffset, 0, returnSpeed * Time.deltaTime);
            }

            if (item == null)
            {
                spriteRenderer.enabled = false;
                lastItem = null;
                currentRecoilOffset = 0;
                return;
            }

            // ✅ 世界暂停：停止逻辑更新
            if (GameRoot.I != null && GameRoot.I.Pause != null && GameRoot.I.Pause.IsPaused)
                return;

            if (spriteRenderer == null) return;

            spriteRenderer.enabled = true;
            spriteRenderer.sprite = item.Visual.worldSprite;

            if (item != lastItem)
            {
                lastItem = item;
                ApplyItemFirePoint(item);
            }

            if (useMainCamera)
            {
                Vector3 wp3 = cam.ScreenToWorldPoint(new Vector3(input.PointerPos.x, input.PointerPos.y, -cam.transform.position.z));
                Vector2 mouseWorld = (Vector2)wp3;
                Vector2 origin = transform.position;
                Vector2 toMouse = mouseWorld - origin;

                Vector2 dir = toMouse.sqrMagnitude < 0.0001f ? Vector2.up : toMouse.normalized;

                // --- 2. 核心修改：将后坐力应用到距离计算中 ---
                // 最终距离 = 基础持握距离 - 当前后坐力缩进量
                float finalDist = item.Visual.holdDistance - currentRecoilOffset;
                
                Vector3 pos = (Vector3)(origin + dir * finalDist);
                pos.z = item.Visual.z;
                spriteRenderer.transform.position = pos;

                // 旋转逻辑保持不变
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

        public void RefreshNow()
        {
            if (spriteRenderer == null || heldItem == null) return;
            var item = heldItem.held;
            if (item == null)
            {
                spriteRenderer.enabled = false;
                spriteRenderer.sprite = null;
                return;
            }
            spriteRenderer.enabled = true;
            spriteRenderer.sprite = item.Visual.worldSprite;
        }

        public Vector2 GetFirePointWorldPos()
        {
            if (firePoint != null) return firePoint.position;
            return (Vector2)spriteRenderer.transform.position;
        }

        private void ApplyItemFirePoint(ItemDefinition item)
        {
            if (spriteRenderer == null) return;
            if (firePoint == null)
            {
                var t = spriteRenderer.transform.Find("FirePoint");
                if (t != null) firePoint = t;
            }
            if (firePoint == null) return;

            if (item is WeaponDefinition weapon)
            {
                firePoint.localPosition = weapon.firePointLocal;
            }
        }
    }
}