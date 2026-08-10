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

        /// <summary>
        /// Partial bank: convert part of the purse and keep spinning. Safety
        /// now, bought with attention later — the caller raises Notice.
        /// </summary>
        public int TitheRunCoins(float percent, float bankingBonusMultiplier)
        {
            int taken = Mathf.RoundToInt(RunCoins * Mathf.Clamp01(percent / 100f));
            if (taken <= 0) return 0;

            RunCoins -= taken;
            int banked = ApplyHouseCut(Mathf.RoundToInt(taken * Mathf.Max(1f, bankingBonusMultiplier)));
            _ctx.Save.Data.metaCoins += banked;
            _ctx.Save.Data.totalBanked += banked;
            return banked;
        }

        /// <summary>At his table the Croupier takes a fifth of everything you bank.</summary>
        private int ApplyHouseCut(int amount)
        {
            float percent = _ctx.Tables?.BankTaxPercent ?? 0f;
            if (percent <= 0f) return amount;
            int cut = Mathf.RoundToInt(amount * percent / 100f);
            if (cut > 0) _ctx.Hud?.Toast($"THE HOUSE TAKES {cut}", Palette.Blood);
            return Mathf.Max(0, amount - cut);
        }

        /// <summary>Moves run coins into the persistent bank. Returns amount banked (with bonus).</summary>
        public int BankRunCoins(float bankingBonusMultiplier)
        {
            int banked = ApplyHouseCut(Mathf.RoundToInt(RunCoins * Mathf.Max(1f, bankingBonusMultiplier)));
            _ctx.Save.Data.metaCoins += banked;
            _ctx.Save.Data.totalBanked += banked;
            RunCoins = 0;
            return banked;
        }
    }
}
