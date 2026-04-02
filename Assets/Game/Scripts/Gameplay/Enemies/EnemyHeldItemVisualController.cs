using UnityEngine;
using Game.Gameplay.Player;
using Game.Systems.Items;

namespace Game.Gameplay.Combat.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HeldItem))]
    public class EnemyHeldItemVisualController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private HeldItem heldItem;
        [SerializeField] private Transform visualOrigin;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Vector2 defaultFirePointLocal = new Vector2(0.6f, 0f);

        [Header("Aim")]
        [SerializeField] private Vector2 defaultAimDirection = Vector2.right;

        private ItemDefinition lastItem;
        private Vector2 aimDirection = Vector2.right;

        private void Reset()
        {
            heldItem = GetComponent<HeldItem>();
            if (visualOrigin == null) visualOrigin = transform;
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (defaultAimDirection.sqrMagnitude > 0.0001f)
                aimDirection = defaultAimDirection.normalized;
        }

        private void Awake()
        {
            if (heldItem == null) heldItem = GetComponent<HeldItem>();
            if (visualOrigin == null) visualOrigin = transform;
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (defaultAimDirection.sqrMagnitude > 0.0001f)
                aimDirection = defaultAimDirection.normalized;

            if (firePoint == null && spriteRenderer != null)
            {
                var t = spriteRenderer.transform.Find("FirePoint");
                if (t != null) firePoint = t;
            }
            if (firePoint != null && firePoint.localPosition == Vector3.zero)
                firePoint.localPosition = defaultFirePointLocal;
        }

        private void Update()
        {
            if (GameRoot.I != null && GameRoot.I.Pause != null && GameRoot.I.Pause.IsPaused)
                return;

            ApplyVisual();
        }

        public void SetAimDirection(Vector2 dir)
        {
            if (dir.sqrMagnitude > 0.0001f)
                aimDirection = dir.normalized;
        }

        public void RefreshNow()
        {
            ApplyVisual();
        }

        public Vector2 GetFirePointWorldPos()
        {
            if (firePoint != null) return firePoint.position;
            if (spriteRenderer != null) return spriteRenderer.transform.position;
            return visualOrigin != null ? (Vector2)visualOrigin.position : (Vector2)transform.position;
        }

        public Vector2 GetFirePointForwardDir()
        {
            if (firePoint != null)
            {
                Vector2 d = firePoint.right;
                if (d.sqrMagnitude > 0.0001f) return d.normalized;
            }

            if (spriteRenderer != null)
            {
                Vector2 d = spriteRenderer.transform.right;
                if (d.sqrMagnitude > 0.0001f) return d.normalized;
            }

            if (aimDirection.sqrMagnitude > 0.0001f) return aimDirection.normalized;
            return Vector2.right;
        }

        private void ApplyVisual()
        {
            if (spriteRenderer == null || heldItem == null) return;

            ItemDefinition item = heldItem.held;
            if (item == null)
            {
                spriteRenderer.enabled = false;
                spriteRenderer.sprite = null;
                lastItem = null;
                return;
            }

            spriteRenderer.enabled = true;
            spriteRenderer.sprite = item.Visual.worldSprite;

            if (item != lastItem)
            {
                lastItem = item;
                ApplyItemFirePoint(item);
            }

            Transform origin = visualOrigin != null ? visualOrigin : transform;
            Vector2 dir = aimDirection.sqrMagnitude < 0.0001f ? Vector2.right : aimDirection.normalized;
            float holdDist = item.Visual.holdDistance;

            Vector3 pos = (Vector3)((Vector2)origin.position + dir * holdDist);
            pos.z = item.Visual.z;
            spriteRenderer.transform.position = pos;

            if (item.Visual.rotationMode == ItemVisualRotationMode.RotateWithAim)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                angle += item.Visual.defaultAngleOffset;
                spriteRenderer.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
            else
            {
                spriteRenderer.transform.rotation = Quaternion.Euler(0f, 0f, item.Visual.defaultAngleOffset);
            }
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
                firePoint.localPosition = weapon.firePointLocal;
            else
                firePoint.localPosition = defaultFirePointLocal;
        }
    }
}
