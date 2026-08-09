using System;
using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// Persistent XP and level. Levels gate sin-boss unlocks and future wheel
    /// configurations (13th segment, rare-odds boosts).
    /// </summary>
    public sealed class XpSystem
    {
        private readonly GameContext _ctx;

        public event Action<int> OnLevelUp;

        public int Level => _ctx.Save.Data.level;
        public int Xp => _ctx.Save.Data.xp;

        public XpSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public int XpToNextLevel()
        {
            var t = _ctx.Config.Tuning;
            return Mathf.RoundToInt(t.xpBase * Mathf.Pow(t.xpGrowth, Level - 1));
        }

        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            _ctx.Save.Data.xp += amount;

            while (_ctx.Save.Data.xp >= XpToNextLevel())
            {
                _ctx.Save.Data.xp -= XpToNextLevel();
                _ctx.Save.Data.level++;
                OnLevelUp?.Invoke(_ctx.Save.Data.level);
            }
        }
    }
}
