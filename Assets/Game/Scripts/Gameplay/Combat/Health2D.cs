using UnityEngine;

namespace Game.Gameplay.Combat
{
    public class Health2D : MonoBehaviour, IDamageable, IHealthView
    {
        public float maxHp = 10;
        public float hp = 10;

        public float Current => hp;
        public float Max => maxHp;

        public System.Action<DamageInfo> OnDamaged;

        private void Awake()
        {
            hp = Mathf.Clamp(hp, 0, maxHp);
            if (hp <= 0) hp = maxHp;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (info.amount <= 0) return;
            hp = Mathf.Clamp(hp - info.amount, 0, maxHp);
            OnDamaged?.Invoke(info);

            if (hp <= 0) Destroy(gameObject);
        }
    }
}
