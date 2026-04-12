using System;
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
        [SerializeField] private SpriteRenderer playerBodyRenderer;
        [SerializeField] private Material chaosHeldMaterial;
        [SerializeField] private string chaosItemId = "chaos";

        [Header("Settings")]
        [SerializeField] private bool useMainCamera = true;

        private ItemDefinition lastItem;
        private Camera cam;
        private float currentRecoilOffset;
        private Material defaultPlayerBodyMaterial;
        private bool hasDefaultPlayerBodyMaterial;
        private bool isChaosMaterialApplied;

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

            TryResolvePlayerBodyRenderer();
            CacheDefaultPlayerBodyMaterial();
        }

        private void OnDisable()
        {
            RestoreDefaultPlayerMaterial();
        }

        private void OnDestroy()
        {
            RestoreDefaultPlayerMaterial();
        }

        public void ApplyVisualRecoil(float strength)
        {
            currentRecoilOffset = Mathf.Clamp(currentRecoilOffset + strength, 0f, strength * 2.5f);
        }

        private void Update()
        {
            if (input == null)
            {
                if (GameRoot.I != null) input = GameRoot.I.playerInput;
                if (input == null) return;
            }

            var item = heldItem != null ? heldItem.held : null;
            UpdatePlayerChaosMaterial(item);

            if (currentRecoilOffset > 0f)
            {
                float returnSpeed = 20f;
                if (item is WeaponDefinition weapon) returnSpeed = weapon.visualRecoilReturnSpeed;
                currentRecoilOffset = Mathf.MoveTowards(currentRecoilOffset, 0f, returnSpeed * Time.deltaTime);
            }

            if (item == null)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.enabled = false;
                    spriteRenderer.sprite = null;
                }
                lastItem = null;
                currentRecoilOffset = 0f;
                return;
            }

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

            if (!useMainCamera) return;

            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            Vector3 wp3 = cam.ScreenToWorldPoint(new Vector3(input.PointerPos.x, input.PointerPos.y, -cam.transform.position.z));
            Vector2 mouseWorld = (Vector2)wp3;
            Vector2 origin = transform.position;
            Vector2 toMouse = mouseWorld - origin;
            Vector2 dir = toMouse.sqrMagnitude < 0.0001f ? Vector2.up : toMouse.normalized;

            float finalDist = item.Visual.holdDistance - currentRecoilOffset;
            Vector3 pos = (Vector3)(origin + dir * finalDist);
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

        public void RefreshNow()
        {
            if (spriteRenderer == null || heldItem == null) return;

            var item = heldItem.held;
            UpdatePlayerChaosMaterial(item);

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
            return spriteRenderer != null ? (Vector2)spriteRenderer.transform.position : (Vector2)transform.position;
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
        }

        private void UpdatePlayerChaosMaterial(ItemDefinition item)
        {
            TryResolvePlayerBodyRenderer();
            if (playerBodyRenderer == null) return;

            bool shouldApplyChaos = chaosHeldMaterial != null
                && item != null
                && !string.IsNullOrWhiteSpace(chaosItemId)
                && string.Equals(item.ItemId, chaosItemId, StringComparison.Ordinal);

            if (shouldApplyChaos == isChaosMaterialApplied) return;

            if (shouldApplyChaos)
            {
                CacheDefaultPlayerBodyMaterial();
                playerBodyRenderer.sharedMaterial = chaosHeldMaterial;
                isChaosMaterialApplied = true;
                return;
            }

            RestoreDefaultPlayerMaterial();
        }

        private void TryResolvePlayerBodyRenderer()
        {
            if (playerBodyRenderer != null) return;

            var renderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var candidate = renderers[i];
                if (candidate == null) continue;
                if (candidate == spriteRenderer) continue;
                playerBodyRenderer = candidate;
                break;
            }
        }

        private void CacheDefaultPlayerBodyMaterial()
        {
            if (playerBodyRenderer == null || hasDefaultPlayerBodyMaterial) return;
            defaultPlayerBodyMaterial = playerBodyRenderer.sharedMaterial;
            hasDefaultPlayerBodyMaterial = true;
        }

        private void RestoreDefaultPlayerMaterial()
        {
            if (!isChaosMaterialApplied) return;
            if (playerBodyRenderer == null) return;
            if (hasDefaultPlayerBodyMaterial) playerBodyRenderer.sharedMaterial = defaultPlayerBodyMaterial;
            isChaosMaterialApplied = false;
        }
    }
}
