using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// Five slots of things the house is holding against your debt. Every
    /// Pledge rewrites a rule rather than nudging a number, so the queries here
    /// are read by the systems whose rules they change — the effects live at
    /// their call sites, not in a switch pretending to be data.
    ///
    /// Pledges persist across runs and are reclaimed when a Mark is taken:
    /// ascension costs you your build, which is what stops a runaway.
    /// </summary>
    public sealed class PledgeSystem
    {
        private readonly GameContext _ctx;

        public PledgeSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public PledgesConfig Config => _ctx.Config.Pledges;
        public int Slots => Config.slots;
        public List<string> Held => _ctx.Save.Data.pledges;
        public int EmptySlots => Mathf.Max(0, Slots - Held.Count);
        public bool HasRoom => Held.Count < Slots;

        public PledgeConfig Get(string id) => Config.pledges.FirstOrDefault(p => p.id == id);
        public bool Has(string id) => Held.Contains(id);

        public IEnumerable<PledgeConfig> HeldConfigs
        {
            get
            {
                foreach (string id in Held)
                {
                    var cfg = Get(id);
                    if (cfg != null) yield return cfg;
                }
            }
        }

        public void Take(string id)
        {
            if (!HasRoom || Has(id)) return;
            Held.Add(id);
            _ctx.Ring.Rebuild(); // Hollow Coin changes the ring itself
            _ctx.Analytics.Track("pledge_taken", "id", id, "held", Held.Count);
            _ctx.Save.Persist();
        }

        /// <summary>Selling refunds half in relics, so experimenting is not punished.</summary>
        public int SellValue(PledgeConfig cfg) =>
            cfg == null ? 0 : Mathf.RoundToInt(RarityValue(cfg.rarity) * Config.sellRefundPercent / 100f);

        public bool CanSell(PledgeConfig cfg) => cfg != null && !cfg.IsCursed;

        public bool TrySell(string id)
        {
            var cfg = Get(id);
            if (!CanSell(cfg) || !Has(id)) return false;

            Held.Remove(id);
            _ctx.Wallet.AddMetaCoins(SellValue(cfg));
            _ctx.Ring.Rebuild();
            _ctx.Analytics.Track("pledge_sold", "id", id);
            _ctx.Save.Persist();
            return true;
        }

        /// <summary>The house reclaims what was pledged as the debt clears.</summary>
        public void ReclaimOnMark()
        {
            if (!Config.lostOnMark || Held.Count == 0) return;
            _ctx.Analytics.Track("pledges_reclaimed", "count", Held.Count);
            Held.Clear();
            _ctx.Ring.Rebuild();
        }

        private static int RarityValue(string rarity)
        {
            switch (rarity)
            {
                case "uncommon": return 120;
                case "rare": return 200;
                case "cursed": return 0;
                default: return 70;
            }
        }

        private float ValueOf(string id) => Get(id)?.value ?? 0f;

        // --- Rules the Pledges rewrite, read by the systems they belong to ---

        /// <summary>Widow's Ring: risk wedges pay instead of wounding.</summary>
        public bool RiskWedgesPayCoin => Has("widows_ring");
        public float RiskCoinPercent => ValueOf("widows_ring");

        /// <summary>Long Coat / Blood Price change what a nudge costs and in what.</summary>
        public int NudgeNoticeDiscount => Has("long_coat") ? Mathf.RoundToInt(ValueOf("long_coat")) : 0;
        public bool NudgeCostsHealth => Has("blood_price");
        public float NudgeHealthCost => ValueOf("blood_price");

        /// <summary>The Thumb: two wedges for the price of one, but he steers sometimes.</summary>
        public bool NudgeTwoForOne => Has("the_thumb");
        public float ThumbHijackPercent => ValueOf("the_thumb");

        /// <summary>Sexton's Key turns a broken sin into fuel for more nudges.</summary>
        public float NoticeRefundOnBreak => Has("sextons_key") ? ValueOf("sextons_key") : 0f;

        /// <summary>The Tally: every Nth spin cannot be a risk wedge.</summary>
        public int GuaranteedRewardEvery => Has("the_tally") ? Mathf.RoundToInt(ValueOf("the_tally")) : 0;

        /// <summary>Cracked Mirror keeps Pride away and lets the rest linger.</summary>
        public bool PrideBanished => Has("cracked_mirror");
        public int SinDurationBonus => Has("cracked_mirror") ? Mathf.RoundToInt(ValueOf("cracked_mirror")) : 0;

        /// <summary>Iron Tithe: free of Notice, but it will only take so much.</summary>
        public bool TitheIsFree => Has("iron_tithe");
        public float TitheCapPercent => ValueOf("iron_tithe");

        /// <summary>Hollow Coin doubles the jackpot in the ring.</summary>
        public bool DoubleJackpot => Has("hollow_coin");

        /// <summary>Seventh Hour: at the last table nothing may come for you.</summary>
        public bool SinsDisabledAtLastTable => Has("seventh_hour");

        /// <summary>Croupier's Favour drags him up the tables.</summary>
        public int CroupierFromTable => Has("croupiers_favour") ? 3 : int.MaxValue;

        /// <summary>Open Ledger shows the next wedge and doubles the watching.</summary>
        public bool ShowsNextWedge => Has("open_ledger");
        public float NoticeRateMultiplier => Has("open_ledger") ? ValueOf("open_ledger") : 1f;

        /// <summary>Blind Wager hides the wheel while it turns.</summary>
        public bool HidesWheel => Has("blind_wager");

        /// <summary>Widow's Debt: busting costs double.</summary>
        public bool DoublesDebtOnBust => Has("widows_debt");

        /// <summary>Understudy keeps the last break reward you earned.</summary>
        public bool KeepsLastBreakReward => Has("understudy");

        /// <summary>
        /// Every Pledge that speaks in multiplier terms, folded into the one
        /// figure the player watches assemble.
        /// </summary>
        public void ContributeMultTerms(ScoreBreakdown score)
        {
            if (Has("paupers_luck") && EmptySlots > 0)
                score.Add($"PAUPERS X{EmptySlots}", ValueOf("paupers_luck") * EmptySlots);

            if (Has("debtors_crown") && _ctx.Marks.EarnedCount > 0)
                score.Add("CROWN", ValueOf("debtors_crown") * _ctx.Marks.EarnedCount);

            if (Has("gravediggers_cut") && _ctx.Save.Data.wedgesStruck > 0)
                score.Add("GRAVE CUT", ValueOf("gravediggers_cut") * _ctx.Save.Data.wedgesStruck);

            if (Has("the_long_game") && _ctx.Game.SpinsThisRun > 0)
                score.Add("LONG GAME", ValueOf("the_long_game") * _ctx.Game.SpinsThisRun);

            if (Has("croupiers_favour"))
                score.Add("CROUPIER", ValueOf("croupiers_favour"));

            if (Has("ash_ledger") && _ctx.Save.Data.ashLedgerCharged)
                score.Add("ASH LEDGER", ValueOf("ash_ledger"));

            if (Has("blind_wager"))
                score.Multiply("BLIND WAGER", ValueOf("blind_wager"));

            if (Has("widows_debt"))
                score.Multiply("WIDOWS DEBT", ValueOf("widows_debt"));
        }
    }
}
