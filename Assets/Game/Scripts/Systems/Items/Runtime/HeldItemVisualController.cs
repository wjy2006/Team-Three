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


        [Tooltip("把相机的 z 平面投到 0，2D 正交一般这样就稳")]
        [SerializeField] private bool useMainCamera = true;

        private Camera cam;

        private void Awake()
        {
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
            if (item == null)
            {
                spriteRenderer.enabled = false;
                return;
            }

            // 有手持：显示并设置 sprite
            spriteRenderer.enabled = true;

            // 这里用你 ItemDefinition 的 WorldSprite（按上面建议加的字段）
            var s = item.WorldSprite;
            if (spriteRenderer.sprite != s) spriteRenderer.sprite = s;

            // 计算鼠标世界坐标
            if (useMainCamera)
            {
                if (cam == null) cam = Camera.main;
                if (cam == null) return;

                Vector2 screen = input.PointerPos;
                Vector3 wp3 = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
                Vector2 mouseWorld = (Vector2)wp3;

                // 计算方向
                Vector3 origin = transform.position;
                Vector2 dir = mouseWorld - (Vector2)origin;

                if (dir.sqrMagnitude < 0.0001f)
                    dir = Vector2.up;
                else
                    dir.Normalize();

                // 位置：玩家身边 distance=1
                float dist = item.HoldDistance;
                Vector3 pos = origin + (Vector3)(dir * dist);
                pos.z = -5f; // ⭐ 固定Z层
                spriteRenderer.transform.position = pos;


                if (item.RotationMode == ItemVisualRotationMode.RotateWithAim)
                {
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    angle += item.DefaultAngleOffset;
                    spriteRenderer.transform.rotation = Quaternion.Euler(0, 0, angle);
                }
                else // FixedUp
                {
                    spriteRenderer.transform.rotation = Quaternion.Euler(0, 0, item.DefaultAngleOffset);
                }

            }
        }
    }
}
