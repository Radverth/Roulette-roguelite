using UnityEngine;

namespace SinWheel
{
    public enum DebtOutcome
    {
        Owed,
        Reduced,
        Grown
    }

    /// <summary>
    /// Banking used to be free, so it was always correct the moment expected
    /// value turned negative. Now every run carries a quota drawn from the
    /// debt: meet it and the debt shrinks, miss it and the debt grows, the
    /// quota compounds, and the house splices another risk wedge into the ring.
    /// Leaving early is no longer the safe option, it is a different risk.
    /// </summary>
    public sealed class DebtSystem
    {
        private readonly GameContext _ctx;

        /// <summary>Coins handed over this run, by tithe or by banking out.</summary>
        public int PaidThisRun { get; private set; }

        public int Debt => _ctx.Save.Data.debt;
        /// <summary>Mark I raises what the house expects, without touching the stored figure.</summary>
        public int Quota => Mathf.RoundToInt(_ctx.Save.Data.quota * _ctx.Marks.QuotaMultiplier);
        public bool QuotaMet => PaidThisRun >= Quota;
        public float QuotaFill => Mathf.Clamp01(PaidThisRun / Mathf.Max(1f, Quota));

        public DebtSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public void EnsureSeeded()
        {
            var data = _ctx.Save.Data;
            var t = _ctx.Config.Tuning;
            if (data.debt < 0) data.debt = t.debtStart;
            if (data.quota < 0) data.quota = t.quotaBase;
        }

        public void ResetForRun()
        {
            EnsureSeeded();
            PaidThisRun = 0;
        }

        public void RecordPayment(int amount)
        {
            PaidThisRun += Mathf.Max(0, amount);
        }

        /// <summary>Settle the run against the quota. Returns what the ledger should show.</summary>
        public DebtOutcome Settle()
        {
            var data = _ctx.Save.Data;
            var t = _ctx.Config.Tuning;

            int quota = Quota;
            data.lastRunPaid = PaidThisRun;
            data.lastRunQuota = quota; // the ledger reports the quota that was in force

            if (PaidThisRun >= quota)
            {
                data.debt = Mathf.Max(0, data.debt - PaidThisRun);
                data.quota = Mathf.Max(t.quotaBase, Mathf.RoundToInt(data.quota * t.quotaRelief));
                data.penaltyRiskWedges = Mathf.Max(0, data.penaltyRiskWedges - 1);
                data.lastRunMetQuota = true;
                return DebtOutcome.Reduced;
            }

            int shortfall = quota - PaidThisRun;
            data.debt += shortfall;
            data.quota = Mathf.RoundToInt(data.quota * t.quotaGrowth);
            data.penaltyRiskWedges = Mathf.Min(t.maxPenaltyRiskWedges, data.penaltyRiskWedges + 1);
            data.lastRunMetQuota = false;
            return DebtOutcome.Grown;
        }

        public static string SealSprite(DebtOutcome outcome)
        {
            switch (outcome)
            {
                case DebtOutcome.Reduced: return "Loop/debt_seal_reduced";
                case DebtOutcome.Grown: return "Loop/debt_seal_grown";
                default: return "Loop/debt_seal_owed";
            }
        }
    }
}
