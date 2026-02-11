using UnityEngine;
using Game.Gameplay.Combat;
using Game.Gameplay.Player;

namespace Game.UI
{
    public class PixelHeadHpBarAdvanced : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private Transform barRoot;
        [SerializeField] private SpriteRenderer fill; // 红条：即时
        [SerializeField] private SpriteRenderer lag;  // 白条：延迟
        [SerializeField] private SpriteRenderer bg;   // 背景/框（可空）

        [Header("Placement")]
        [SerializeField] private Vector3 offset = new Vector3(0, 0, 0);
        [SerializeField] private float z = -5f;

        [Header("Show/Hide")]
        [SerializeField] private float showTime = 5;      // 受击后保持多久再开始淡出
        [SerializeField] private float fadeDuration = 3; // 淡出时长

        [Header("Lag (gray bar)")]
        [Tooltip("白条开始追红条前的延迟")]
        [SerializeField] private float lagDelay = 0.2f;

        [Tooltip("白条追上红条需要的时间（秒）。掉的越多追得越快")]
        [SerializeField] private float lagCatchupDuration = 0.6f;

        private IHealthView health;
        private float hideAt;
        private float alpha = 0f;
        private bool fading = false;

        // 用于 lag 逻辑
        private float lagValue01 = 1f;          // 白条当前显示比例
        private float lagDelayUntil = 0f;       // 白条延迟到这个时间才开始追

        // 事件源（兼容两种）
        private PlayerStats playerStats;
        private Health2D health2D;

        private void Awake()
        {
            health = GetComponent<IHealthView>();
            if (health == null)
            {
                Debug.LogError($"{name}: 缺少 IHealthView（PlayerStats/Health2D 需要实现）");
                enabled = false;
                return;
            }

            if (barRoot == null)
            {
                var t = transform.Find("HpBarRoot");
                if (t != null) barRoot = t;
            }

            if (barRoot == null)
            {
                Debug.LogError($"{name}: 找不到 barRoot（请在角色下建 HpBarRoot 并拖给脚本）");
                enabled = false;
                return;
            }

            if (fill == null)
            {
                var t = barRoot.Find("Fill");
                if (t != null) fill = t.GetComponent<SpriteRenderer>();
            }
            if (lag == null)
            {
                var t = barRoot.Find("Lag");
                if (t != null) lag = t.GetComponent<SpriteRenderer>();
            }
            if (bg == null)
            {
                var t = barRoot.Find("Bg");
                if (t != null) bg = t.GetComponent<SpriteRenderer>();
            }

            if (fill == null || lag == null)
            {
                Debug.LogError($"{name}: Fill 或 Lag 没绑定（请确认 HpBarRoot 下有 Fill/Lag SpriteRenderer）");
                enabled = false;
                return;
            }

            playerStats = GetComponent<PlayerStats>();
            health2D = GetComponent<Health2D>();

            barRoot.gameObject.SetActive(false);
            SetAlpha(0f);

            // 初始同步
            float t01 = GetHp01();
            lagValue01 = t01;
            ApplyScale(fill, t01);
            ApplyScale(lag, t01);
        }

        private void OnEnable()
        {
            if (playerStats != null) playerStats.OnDamaged += OnDamaged;
            if (health2D != null) health2D.OnDamaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (playerStats != null) playerStats.OnDamaged -= OnDamaged;
            if (health2D != null) health2D.OnDamaged -= OnDamaged;
        }

        private void LateUpdate()
        {
            // 跟随头顶
            Vector3 pos = transform.position + offset;
            pos.z = z;
            barRoot.position = pos;

            float hp01 = GetHp01();

            // 红条：立即跟随
            ApplyScale(fill, hp01);

            // 白条：延迟后按“固定时长”追随
            if (Time.time >= lagDelayUntil)
            {
                // 回血：白条直接跟上（可改成也用时长追，但一般这样更干净）
                if (hp01 > lagValue01)
                {
                    lagValue01 = hp01;
                }
                else
                {
                    // 掉血：在 lagCatchupDuration 秒内追到 hp01
                    float duration = Mathf.Max(0.0001f, lagCatchupDuration);
                    float diff = Mathf.Abs(lagValue01 - hp01);

                    // 关键：每秒速度 = diff / duration（差值越大，速度越快）
                    float step = (diff / duration) * Time.deltaTime;

                    lagValue01 = Mathf.MoveTowards(lagValue01, hp01, step);
                }
            }

            ApplyScale(lag, lagValue01);

            // 淡出逻辑
            if (barRoot.gameObject.activeSelf)
            {
                if (!fading && Time.time >= hideAt)
                {
                    fading = true;
                }

                if (fading)
                {
                    alpha -= Time.deltaTime / Mathf.Max(0.0001f, fadeDuration);
                    SetAlpha(alpha);

                    if (alpha <= 0f)
                    {
                        barRoot.gameObject.SetActive(false);
                        fading = false;
                    }
                }
            }
        }

        private void OnDamaged(DamageInfo info)
        {
            // 显示并重置计时
            barRoot.gameObject.SetActive(true);
            hideAt = Time.time + showTime;

            // 立刻不透明
            fading = false;
            alpha = 1f;
            SetAlpha(1f);

            // 掉血时，让白条延迟再追
            lagDelayUntil = Time.time + lagDelay;

            // 防止白条小于红条（例如第一次显示或某些突变）
            float hp01 = GetHp01();
            if (lagValue01 < hp01) lagValue01 = hp01;
        }

        private float GetHp01()
        {
            float max = Mathf.Max(0.0001f, health.Max);
            return Mathf.Clamp01(health.Current / max);
        }

        private static void ApplyScale(SpriteRenderer r, float x01)
        {
            if (r == null) return;
            var s = r.transform.localScale;
            s.x = x01;
            r.transform.localScale = s;
        }

        private void SetAlpha(float a)
        {
            a = Mathf.Clamp01(a);

            if (bg != null)
            {
                var c = bg.color; c.a = a; bg.color = c;
            }
            if (lag != null)
            {
                var c = lag.color; c.a = a; lag.color = c;
            }
            if (fill != null)
            {
                var c = fill.color; c.a = a; fill.color = c;
            }
        }
    }
}
