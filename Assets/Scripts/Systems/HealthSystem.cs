using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// The run's shared resilience meter. Not combat HP — it only drains from
    /// risk segments. Hitting zero ends the run and forfeits unbanked coins.
    /// </summary>
    public sealed class HealthSystem
    {
        public int MaxHp { get; private set; } = 1;
        public float CurrentHp { get; private set; } = 1f;
        public bool IsDead => CurrentHp <= 0f;

        public void ResetForRun(int maxHp)
        {
            MaxHp = Mathf.Max(1, maxHp);
            CurrentHp = MaxHp;
        }

        /// <returns>Damage actually applied after clamping.</returns>
        public float ApplyDamage(float amount)
        {
            float applied = Mathf.Min(CurrentHp, Mathf.Max(0f, amount));
            CurrentHp -= applied;
            return applied;
        }

        public void Heal(float amount)
        {
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + Mathf.Max(0f, amount));
        }
    }
}
