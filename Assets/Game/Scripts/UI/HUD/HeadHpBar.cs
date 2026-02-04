using UnityEngine;
using Game.Gameplay.Combat;
using Game.Gameplay.Player;

namespace Game.UI
{
    public class HeadHpBar : MonoBehaviour
    {
        [SerializeField] private Transform barRoot;
        [SerializeField] private SpriteRenderer fillRenderer;

        [SerializeField] private Vector3 offset = new Vector3(0, 0, 0);
        [SerializeField] private float showTime = 5f;

        private IHealthView health;
        private float hideAt;

        private void Awake()
        {
            health = GetComponent<IHealthView>();

            if (barRoot == null)
                barRoot = transform.Find("HpBarRoot");

            if (fillRenderer == null)
                fillRenderer = barRoot.Find("Fill").GetComponent<SpriteRenderer>();

            barRoot.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (TryGetComponent<IDamageable>(out var d))
            {
                if (d is PlayerStats ps)
                    ps.OnDamaged += OnDamaged;
                if (d is Health2D h2)
                    h2.OnDamaged += OnDamaged;
            }
        }

        private void OnDisable()
        {
            if (TryGetComponent<IDamageable>(out var d))
            {
                if (d is PlayerStats ps)
                    ps.OnDamaged -= OnDamaged;
                if (d is Health2D h2)
                    h2.OnDamaged -= OnDamaged;
            }
        }

        private void LateUpdate()
        {
            if (barRoot == null || health == null) return;

            barRoot.position = transform.position + offset;

            float t = health.Current / health.Max;
            t = Mathf.Clamp01(t);

            // ⭐ 关键：修改 X scale
            fillRenderer.transform.localScale =
                new Vector3(t, 1f, 1f);

            if (barRoot.gameObject.activeSelf && Time.time > hideAt)
                barRoot.gameObject.SetActive(false);
        }

        private void OnDamaged(DamageInfo info)
        {
            barRoot.gameObject.SetActive(true);
            hideAt = Time.time + showTime;
        }
    }
}
