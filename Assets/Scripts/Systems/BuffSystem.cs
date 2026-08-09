using System.Collections.Generic;
using System.Linq;

namespace SinWheel
{
    /// <summary>
    /// Temporary run effects measured in spins. Buffs multiply rewards; debuffs
    /// amplify damage taken. Envy will use BestBuff to mirror the player's
    /// strongest blessing onto the risk side.
    /// </summary>
    public sealed class BuffSystem
    {
        public sealed class ActiveEffect
        {
            public string Id;
            public bool IsDebuff;
            public float Multiplier;
            public int SpinsRemaining;
        }

        private readonly List<ActiveEffect> _effects = new List<ActiveEffect>();

        public IReadOnlyList<ActiveEffect> Effects => _effects;

        public float RewardMultiplier
        {
            get
            {
                float m = 1f;
                foreach (var e in _effects)
                    if (!e.IsDebuff) m *= e.Multiplier;
                return m;
            }
        }

        public float DamageMultiplier
        {
            get
            {
                float m = 1f;
                foreach (var e in _effects)
                    if (e.IsDebuff) m *= e.Multiplier;
                return m;
            }
        }

        public ActiveEffect BestBuff =>
            _effects.Where(e => !e.IsDebuff).OrderByDescending(e => e.Multiplier).FirstOrDefault();

        public void AddBuff(string id, float multiplier, int spins)
        {
            _effects.Add(new ActiveEffect { Id = id, IsDebuff = false, Multiplier = multiplier, SpinsRemaining = spins });
        }

        public void AddDebuff(string id, float multiplier, int spins)
        {
            _effects.Add(new ActiveEffect { Id = id, IsDebuff = true, Multiplier = multiplier, SpinsRemaining = spins });
        }

        /// <summary>Called once per resolved spin.</summary>
        public void TickSpin()
        {
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                _effects[i].SpinsRemaining--;
                if (_effects[i].SpinsRemaining <= 0)
                    _effects.RemoveAt(i);
            }
        }

        public void Clear()
        {
            _effects.Clear();
        }
    }
}
