using System.Linq;
using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// Permanent upgrades bought with meta coins. Tiers persist in SaveData.
    /// Covers the meta tree (cooldown, HP cap, banking bonus) and per-sin
    /// 3-tier resistance trees.
    /// </summary>
    public sealed class UpgradeSystem
    {
        private readonly GameContext _ctx;

        public UpgradeSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public UpgradeConfig GetConfig(string id) =>
            _ctx.Config.Upgrades.upgrades.FirstOrDefault(u => u.id == id);

        public int GetTier(string id) => _ctx.Save.Data.GetUpgradeTier(id);

        public int GetCost(UpgradeConfig cfg, int currentTier) =>
            Mathf.RoundToInt(cfg.baseCost * Mathf.Pow(cfg.costGrowth, currentTier));

        public bool TryPurchase(string id)
        {
            UpgradeConfig cfg = GetConfig(id);
            if (cfg == null) return false;

            int tier = GetTier(id);
            if (tier >= cfg.maxTier) return false;

            int cost = GetCost(cfg, tier);
            if (!_ctx.Wallet.TrySpendMetaCoins(cost)) return false;

            _ctx.Save.Data.SetUpgradeTier(id, tier + 1);
            _ctx.Save.Persist();
            _ctx.Analytics.Track("upgrade_purchased", "id", id, "tier", tier + 1, "cost", cost);
            return true;
        }

        // --- Effect queries used by gameplay systems ---

        public float EffectiveSpinCooldown()
        {
            var t = _ctx.Config.Tuning;
            var cfg = GetConfig("spin_cooldown");
            float reduction = cfg != null ? GetTier(cfg.id) * cfg.effectPerTier : 0f;
            return Mathf.Max(t.minSpinCooldown, t.baseSpinCooldown - reduction);
        }

        public int EffectiveMaxHp()
        {
            var cfg = GetConfig("hp_cap");
            float bonus = cfg != null ? GetTier(cfg.id) * cfg.effectPerTier : 0f;
            return _ctx.Config.Tuning.baseMaxHp + Mathf.RoundToInt(bonus);
        }

        public float BankingBonusMultiplier()
        {
            var cfg = GetConfig("banking_bonus");
            float bonus = cfg != null ? GetTier(cfg.id) * cfg.effectPerTier : 0f;
            return _ctx.Config.Tuning.bankingBonusBase + bonus;
        }

        /// <summary>Accumulated mitigation for one sin's resistance tree.</summary>
        public float SinResistValue(string sinId)
        {
            var cfg = _ctx.Config.Upgrades.upgrades
                .FirstOrDefault(u => u.category == "sin_resist" && u.sinId == sinId);
            if (cfg == null) return 0f;
            return GetTier(cfg.id) * cfg.effectPerTier;
        }
    }
}
