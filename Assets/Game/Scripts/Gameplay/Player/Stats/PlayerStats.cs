using System;
using Game.Gameplay.Combat;
using UnityEngine;

namespace Game.Gameplay.Player
{
    public class PlayerStats : MonoBehaviour, IDamageable, IHealthView
    {
        public const string STATE_DEATH_RESPAWN_SCENE = "player.death.respawn.scene";
        public const string STATE_DEATH_RESPAWN_SPAWN = "player.death.respawn.spawn";

        [Header("HP")]
        [SerializeField] private int maxHp = 20;
        [SerializeField] private int hp = 20;

        [Header("Money")]
        [SerializeField] private int money = 0;

        [Header("Damage")]
        [SerializeField] private bool damageEnabled = true;

        [Header("Death Respawn Defaults")]
        [SerializeField] private string defaultDeathRespawnScene = "Room_Lab_Reviving";
        [SerializeField] private string defaultDeathRespawnSpawnId = "Left";

        public int MaxHp => maxHp;
        public int Hp => hp;
        public float Current => hp;
        public float Max => maxHp;
        public int Money => money;
        public bool IsDead => hp <= 0;
        public bool IsFullHp => hp >= maxHp;
        public bool DamageEnabled => damageEnabled;

        public event Action OnStatsChanged;
        public event Action<DamageInfo> OnDamaged;
        public Action OnHpChanged;

        private void Awake()
        {
            hp = Mathf.Clamp(hp, 0, maxHp);
        }

        public void TakeDamage(DamageInfo info)
        {
            if (!damageEnabled) return;
            if (IsDead) return;
            if (info.amount <= 0f) return;

            int amount = Mathf.RoundToInt(info.amount);

            hp -= amount;
            hp = Mathf.Clamp(hp, 0, maxHp);

            var knock = GetComponent<KnockbackReceiver>();
            if (knock != null)
                knock.ApplyKnockback(info.direction, info.knockbackForce);

            OnDamaged?.Invoke(info);
            OnStatsChanged?.Invoke();
            OnHpChanged?.Invoke();
            GameRoot.I.Triggers.Raise(new DamagedEvent(gameObject, info));

            if (hp <= 0)
                Die(info);
        }

        private void Die(DamageInfo info)
        {
            Debug.Log($"Player died. killer={info.source?.name}");

            if (GameRoot.I == null)
            {
                Debug.LogError("GameRoot not found. Cannot transition on death.");
                return;
            }

            if (GameRoot.I.IsTransitioning) return;

            if (GameRoot.I.Dialogue != null && GameRoot.I.Dialogue.IsOpen)
                GameRoot.I.Dialogue.Close();

            string respawnScene = ResolveDeathRespawnScene(GameRoot.I);
            string respawnSpawnId = ResolveDeathRespawnSpawnId(GameRoot.I);

            GameRoot.I.TransitionTo(
                toScene: respawnScene,
                toSpawnId: respawnSpawnId
            );
        }

        private string ResolveDeathRespawnScene(GameRoot root)
        {
            string scene = root?.Global?.GetString(STATE_DEATH_RESPAWN_SCENE);
            if (!string.IsNullOrWhiteSpace(scene))
                return scene.Trim();
            return defaultDeathRespawnScene;
        }

        private string ResolveDeathRespawnSpawnId(GameRoot root)
        {
            string spawnId = root?.Global?.GetString(STATE_DEATH_RESPAWN_SPAWN);
            if (!string.IsNullOrWhiteSpace(spawnId))
                return spawnId.Trim();
            return defaultDeathRespawnSpawnId;
        }

        public void Heal(int amount)
        {
            if (IsDead) return;
            if (amount <= 0) return;

            hp += amount;
            hp = Mathf.Clamp(hp, 0, maxHp);

            OnStatsChanged?.Invoke();
            OnHpChanged?.Invoke();
        }

        public void ReviveToFull()
        {
            hp = maxHp;
            OnStatsChanged?.Invoke();
        }

        public void FullHeal()
        {
            if (hp == maxHp) return;

            hp = maxHp;
            OnStatsChanged?.Invoke();
            OnHpChanged?.Invoke();
        }

        public void AddMoney(int amount)
        {
            if (amount <= 0) return;

            money += amount;
            OnStatsChanged?.Invoke();
        }

        public void SetDamageEnabled(bool enabled)
        {
            damageEnabled = enabled;
        }

        public void ResetForNewGame(int startingMoney = 0)
        {
            hp = maxHp;
            money = Mathf.Max(0, startingMoney);
            OnStatsChanged?.Invoke();
            OnHpChanged?.Invoke();
        }

        public bool TrySpendMoney(int amount)
        {
            if (amount < 0) return false;
            if (money < amount) return false;

            money -= amount;
            OnStatsChanged?.Invoke();
            return true;
        }
    }
}
