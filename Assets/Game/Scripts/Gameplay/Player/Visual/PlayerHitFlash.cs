using System.Collections;
using UnityEngine;

namespace Game.Gameplay.Player
{
    public class PlayerHitFlash : MonoBehaviour
    {
        [SerializeField] private PlayerStats stats;
        [SerializeField] private SpriteRenderer targetRenderer;

        [Header("Flash")]
        [SerializeField] private float flashDuration = 0.12f;

        private Coroutine flashCo;
        private Color originalColor;

        private void Awake()
        {
            if (stats == null) stats = GetComponentInParent<PlayerStats>();
            if (targetRenderer == null) targetRenderer = GetComponentInChildren<SpriteRenderer>();

            if (targetRenderer != null)
                originalColor = targetRenderer.color;
        }

        private void OnEnable()
        {
            if (stats != null)
                stats.OnDamaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (stats != null)
                stats.OnDamaged -= OnDamaged;
        }

        private void OnDamaged(Combat.DamageInfo info)
        {
            if (flashCo != null) StopCoroutine(flashCo);
            flashCo = StartCoroutine(FlashRed());
        }


        private IEnumerator FlashRed()
        {
            targetRenderer.color = Color.red;
            yield return new WaitForSeconds(flashDuration);
            targetRenderer.color = originalColor;
            flashCo = null;
        }
    }
}
