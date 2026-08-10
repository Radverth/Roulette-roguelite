using UnityEngine;

namespace SinWheel
{
    /// <summary>What the Croupier does once he sits down. One per run, rotating.</summary>
    public enum CroupierSeat
    {
        CallsTheWedge,  // he names the wedge first; if he is right you get nothing
        BankTax,        // every bank is taxed
        NoBreaks,       // break conditions are disabled
        BlindSpin       // the result is withheld until you commit to another
    }

    /// <summary>
    /// The descent. A run is no longer a flat sequence of spins: cross a coin
    /// threshold and the house invites you deeper, where the stakes and the
    /// danger both multiply. Declining is not playing safe — it is cashing out,
    /// and the run ends. Advancement is tied to winnings, not spin count,
    /// because that is what a house does.
    /// </summary>
    public sealed class TableSystem
    {
        private readonly GameContext _ctx;

        public int CurrentTable { get; private set; } = 1;
        public int CoinsEarnedThisRun { get; private set; }
        public bool InvitePending { get; private set; }
        public CroupierSeat Seat { get; private set; }

        public TableSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public int TableCount => _ctx.Config.Tables.tables.Count;

        public TableConfig Config(int index)
        {
            foreach (var t in _ctx.Config.Tables.tables)
                if (t.index == index) return t;
            return _ctx.Config.Tables.tables.Count > 0 ? _ctx.Config.Tables.tables[0] : new TableConfig();
        }

        public TableConfig Current => Config(CurrentTable);
        public bool AtLastTable => CurrentTable >= TableCount;

        /// <summary>Coins this run must reach for the next invite. Compounding.</summary>
        public int NextThreshold
        {
            get
            {
                var cfg = _ctx.Config.Tables;
                return Mathf.RoundToInt(cfg.thresholdBase * Mathf.Pow(cfg.thresholdGrowth, CurrentTable - 1));
            }
        }

        public float ThresholdProgress =>
            AtLastTable ? 1f : Mathf.Clamp01(CoinsEarnedThisRun / (float)Mathf.Max(1, NextThreshold));

        public void ResetForRun()
        {
            CurrentTable = 1;
            CoinsEarnedThisRun = 0;
            InvitePending = false;

            // He takes one modifier per run, rotating, so a player who reaches
            // his table repeatedly does not meet the same trick twice.
            Seat = (CroupierSeat)(_ctx.Save.Data.runsCompleted % 4);
        }

        /// <summary>Every coin won counts toward the invite, banked or not.</summary>
        public void RecordCoinsEarned(int coins)
        {
            if (coins <= 0) return;
            CoinsEarnedThisRun += coins;
        }

        /// <summary>Called after a spin resolves: has the house seen enough to invite you deeper?</summary>
        public bool ShouldInvite()
        {
            if (InvitePending || AtLastTable) return false;
            return CoinsEarnedThisRun >= NextThreshold;
        }

        public void RaiseInvite()
        {
            InvitePending = true;
            _ctx.Analytics.Track("table_invite", "from_table", CurrentTable, "coins", CoinsEarnedThisRun);
        }

        /// <summary>Accept the invite: deeper, richer, worse.</summary>
        public void Accept()
        {
            InvitePending = false;
            if (AtLastTable) return;

            CurrentTable++;
            CoinsEarnedThisRun = 0;
            _ctx.Run.TableCooldownBonus = 0f; // a Vigil win lasts one table

            var table = Current;

            // Mending stops partway down.
            if (table.restoresResilience)
                _ctx.Health.Heal(_ctx.Health.MaxHp);

            // From the Offering, the house adds something of its own each table.
            if (table.cursedWedgePerTable)
            {
                _ctx.Run.ExtraWedges.Add("damage_large");
                _ctx.Hud?.Toast("THE HOUSE ADDS A WEDGE", Palette.Blood);
            }

            _ctx.Ring.Rebuild();
            _ctx.Analytics.Track("table_accepted", "table", CurrentTable, "hp", Mathf.RoundToInt(_ctx.Health.CurrentHp));

            if (IsCroupierSeated)
            {
                _ctx.Hud?.ShowSpeech("croupier", SeatLine);
                _ctx.Analytics.Track("croupier_seated", "table", CurrentTable, "seat", Seat.ToString());
            }
        }

        public void Decline()
        {
            InvitePending = false;
            _ctx.Analytics.Track("table_declined", "table", CurrentTable, "coins", CoinsEarnedThisRun);
            _ctx.Game.BankAndEndRun();
        }

        // --- Effects of the current table ---

        public float RewardMultiplier => Current.rewardMultiplier;
        public float NoticeRateMultiplier => Current.noticeRateMultiplier;
        public int MaxActiveSins => Mathf.Max(1, Current.maxActiveSins);
        public int ExtraRiskWedges => Current.extraRiskWedges;

        /// <summary>
        /// A Mark drags him up the tables; Croupier's Favour drags him further
        /// still. Whichever wants him earliest wins.
        /// </summary>
        public bool IsCroupierSeated => CurrentTable >=
            Mathf.Min(_ctx.Marks.CroupierFromTable, _ctx.Pledges.CroupierFromTable);

        public string SeatLine
        {
            get
            {
                switch (Seat)
                {
                    case CroupierSeat.CallsTheWedge: return "I WILL CALL IT FIRST.";
                    case CroupierSeat.BankTax: return "MY CUT IS A FIFTH NOW.";
                    case CroupierSeat.NoBreaks: return "THEY STAY UNTIL I SAY.";
                    default: return "YOU WILL NOT SEE IT YET.";
                }
            }
        }

        public string SeatLabel
        {
            get
            {
                switch (Seat)
                {
                    case CroupierSeat.CallsTheWedge: return "HE CALLS IT";
                    case CroupierSeat.BankTax: return "HE TAKES A FIFTH";
                    case CroupierSeat.NoBreaks: return "NO BREAKS";
                    default: return "BLIND SPIN";
                }
            }
        }

        /// <summary>Table VII: he taxes every bank.</summary>
        public float BankTaxPercent =>
            IsCroupierSeated && Seat == CroupierSeat.BankTax ? 20f : 0f;

        /// <summary>Table VII: sins cannot be broken, they simply run their course.</summary>
        public bool BreaksDisabled =>
            IsCroupierSeated && Seat == CroupierSeat.NoBreaks;

        public bool CallsTheWedge =>
            IsCroupierSeated && Seat == CroupierSeat.CallsTheWedge;

        public bool BlindSpin =>
            IsCroupierSeated && Seat == CroupierSeat.BlindSpin;

        public string PlaqueSprite => $"Escalation/table_plaque_{Mathf.Clamp(CurrentTable, 1, 7)}";
    }
}
