using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// Three currencies:
    ///  - Run coins: at-risk, reset every run, banked at the player's discretion.
    ///  - Meta coins: persistent, spent on permanent upgrades.
    ///  - Gems: rare cosmetic currency, persistent, never gates power.
    /// </summary>
    public sealed class CurrencySystem
    {
        private readonly GameContext _ctx;

        public int RunCoins { get; private set; }

        public int MetaCoins => _ctx.Save.Data.metaCoins;
        public int Gems => _ctx.Save.Data.gems;

        public CurrencySystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public void ResetRun()
        {
            RunCoins = 0;
        }

        public void AddRunCoins(int amount)
        {
            RunCoins = Mathf.Max(0, RunCoins + amount);
        }

        /// <returns>Coins actually lost.</returns>
        public int LoseRunCoinsPercent(float percent)
        {
            int lost = Mathf.RoundToInt(RunCoins * Mathf.Clamp01(percent / 100f));
            RunCoins -= lost;
            return lost;
        }

        /// <summary>Gluttony hook: spins cost banked (meta) currency.</summary>
        public int ChargeMetaCoinsPercent(float percent)
        {
            int cost = Mathf.RoundToInt(_ctx.Save.Data.metaCoins * Mathf.Clamp01(percent / 100f));
            _ctx.Save.Data.metaCoins = Mathf.Max(0, _ctx.Save.Data.metaCoins - cost);
            return cost;
        }

        public void AddMetaCoins(int amount)
        {
            _ctx.Save.Data.metaCoins = Mathf.Max(0, _ctx.Save.Data.metaCoins + amount);
        }

        public bool TrySpendMetaCoins(int amount)
        {
            if (_ctx.Save.Data.metaCoins < amount) return false;
            _ctx.Save.Data.metaCoins -= amount;
            return true;
        }

        public void AddGems(int amount)
        {
            _ctx.Save.Data.gems = Mathf.Max(0, _ctx.Save.Data.gems + amount);
        }

        /// <summary>Moves run coins into the persistent bank. Returns amount banked (with bonus).</summary>
        public int BankRunCoins(float bankingBonusMultiplier)
        {
            int banked = Mathf.RoundToInt(RunCoins * Mathf.Max(1f, bankingBonusMultiplier));
            _ctx.Save.Data.metaCoins += banked;
            _ctx.Save.Data.totalBanked += banked;
            RunCoins = 0;
            return banked;
        }
    }
}
