using System.Collections.Generic;
using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// Run lifecycle. A run lasts until resilience hits zero or the player
    /// walks away — but walking away is no longer free: every run carries a
    /// quota drawn from the debt, and the ledger settles against it.
    /// </summary>
    public sealed class GameManager
    {
        private readonly GameContext _ctx;

        public bool RunActive { get; private set; }
        public int SpinsThisRun;
        public DebtOutcome LastDebtOutcome { get; private set; } = DebtOutcome.Owed;
        public List<MarkConfig> LastMarksEarned { get; private set; } = new List<MarkConfig>();

        private bool _greedNoticedThisRun;
        private bool _sideTableOffered;

        public GameManager(GameContext ctx)
        {
            _ctx = ctx;
            _ctx.Xp.OnLevelUp += HandleLevelUp;
        }

        public void StartRun()
        {
            _ctx.Health.ResetForRun(_ctx.Upgrades.EffectiveMaxHp());
            _ctx.Wallet.ResetRun();
            _ctx.Buffs.Clear();
            _ctx.Bosses.ResetForRun();
            _ctx.Spin.ResetForRun();
            _ctx.Run.Reset();
            _ctx.Notice.ResetForRun();
            _ctx.Streak.ResetForRun();
            _ctx.Debt.ResetForRun();
            _ctx.Tables.ResetForRun();
            _ctx.Interludes.ResetForRun();
            _ctx.Ring.ClearShuffle(); // also rebuilds the ring for the new run

            SpinsThisRun = 0;
            _greedNoticedThisRun = false;
            _sideTableOffered = false;
            RunActive = true;

            _ctx.Analytics.Track("run_start",
                "level", _ctx.Xp.Level, "quota", _ctx.Debt.Quota, "ring", _ctx.Ring.Count,
                "marks", _ctx.Marks.EarnedCount);
            _ctx.Hud?.OnRunStarted();

            // The Understudy keeps the break reward of the last sin you broke.
            if (_ctx.Pledges.KeepsLastBreakReward)
                ApplyRememberedBoon(_ctx.Save.Data.lastBrokenSin);

            // The Croupier speaks at run start and run end, nowhere else.
            _ctx.Hud?.ShowSpeech("croupier", _ctx.Narrative.RunStartLine());
        }

        /// <summary>Called by SpinSystem after every resolved spin.</summary>
        public void AfterSpinResolved()
        {
            if (RunActive && _ctx.Health.IsDead)
            {
                EndRun(banked: false, bankedAmount: 0);
                return;
            }

            // The house has seen enough winnings to invite you deeper.
            if (RunActive && _ctx.Tables.ShouldInvite())
            {
                _ctx.Tables.RaiseInvite();
                _ctx.Hud?.ShowTableInvite();
                return;
            }

            // The Side Table: a hand on the one dial the player otherwise only
            // watches move, placed where the tension is highest.
            if (RunActive && !_sideTableOffered
                && _ctx.Notice.Fill >= _ctx.Interludes.Config.sideTableNoticeGate)
            {
                _sideTableOffered = true;
                _ctx.Hud?.ShowInterlude(sideTable: true);
                return;
            }

            // Reactive: a long unbanked streak earns approval from the wrong quarter.
            if (RunActive && !_greedNoticedThisRun && SpinsThisRun == 15 && _ctx.Debt.PaidThisRun == 0)
            {
                _greedNoticedThisRun = true;
                var line = _ctx.Narrative.Reactive?.never_banked_this_run;
                if (line != null)
                    _ctx.Hud?.ShowSpeech(line.speaker, line.line);
            }
        }

        /// <summary>Only between spins: a landing that has not resolved yet is still in play.</summary>
        public bool SpinSettled =>
            _ctx.Spin.State == SpinState.Idle || _ctx.Spin.State == SpinState.Cooldown;

        public bool CanTithe => RunActive && SpinSettled && _ctx.Wallet.RunCoins > 0;

        /// <summary>
        /// Pay part of the purse without ending the run. Costs a segment of
        /// Notice — and it is the one thing Gluttony cannot survive.
        /// </summary>
        public void Tithe()
        {
            if (!CanTithe) return;

            // Iron Tithe: free of Notice, but it will only take so much.
            float percent = _ctx.Pledges.TitheIsFree
                ? Mathf.Min(_ctx.Config.Tuning.tithePercentOfPurse, _ctx.Pledges.TitheCapPercent)
                : _ctx.Config.Tuning.tithePercentOfPurse;

            int banked = _ctx.Wallet.TitheRunCoins(percent, _ctx.Upgrades.BankingBonusMultiplier());
            if (banked <= 0) return;

            _ctx.Debt.RecordPayment(banked);
            if (!_ctx.Pledges.TitheIsFree) _ctx.Notice.OnTithe();
            _ctx.Hud?.Toast($"TITHED {banked}", Palette.Gold);
            Sfx.Reward();

            _ctx.Analytics.Track("tithe", "amount", banked, "spins", SpinsThisRun);
            _ctx.Bosses.OnTithe();
            _ctx.Save.Persist();
        }

        public void BankAndEndRun()
        {
            if (!RunActive || !SpinSettled) return;

            int banked = _ctx.Wallet.BankRunCoins(_ctx.Upgrades.BankingBonusMultiplier());
            _ctx.Debt.RecordPayment(banked);

            if (banked > _ctx.Save.Data.bestSingleBank)
                _ctx.Save.Data.bestSingleBank = banked;

            // Reactive: banking the moment the wheel warms up, three runs running.
            if (SpinsThisRun <= 3)
            {
                _ctx.Save.Data.consecutiveInstantBanks++;
                if (_ctx.Save.Data.consecutiveInstantBanks >= 3)
                {
                    var line = _ctx.Narrative.Reactive?.banked_instantly_thrice;
                    if (line != null) _ctx.Narrative.SetRunEndQuote(line.line, 3);
                    _ctx.Save.Data.consecutiveInstantBanks = 0;
                }
            }
            else
            {
                _ctx.Save.Data.consecutiveInstantBanks = 0;
            }

            // Ash Ledger arms itself on a bank and spends on the next run.
            if (_ctx.Pledges.Has("ash_ledger")) _ctx.Save.Data.ashLedgerCharged = true;

            _ctx.Analytics.Track("bank", "amount", banked, "spins", SpinsThisRun);
            EndRun(banked: true, bankedAmount: banked);
        }

        private void EndRun(bool banked, int bankedAmount)
        {
            RunActive = false;
            int purseAtEnd = banked ? bankedAmount : _ctx.Wallet.RunCoins;

            // Boss drop-off is the metric the sin difficulty curve is tuned on.
            _ctx.Bosses.AbandonEncounters(banked ? "banked_out" : "died");
            _ctx.Narrative.ChooseRunEndQuote(banked, purseAtEnd);

            if (!banked)
                _ctx.Wallet.ResetRun(); // unbanked winnings are forfeit

            // Widow's Debt: busting is not merely a loss, it compounds.
            if (!banked && _ctx.Pledges.DoublesDebtOnBust)
            {
                _ctx.Save.Data.debt *= 2;
                _ctx.Hud?.Toast("THE DEBT DOUBLES", Palette.Blood);
            }

            LastDebtOutcome = _ctx.Debt.Settle();
            LastMarksEarned = _ctx.Marks.CheckForNewMarks();
            // The house reclaims what was pledged as the debt clears.
            if (LastMarksEarned.Count > 0) _ctx.Pledges.ReclaimOnMark();

            if (_ctx.Tables.CurrentTable > _ctx.Save.Data.deepestTable)
                _ctx.Save.Data.deepestTable = _ctx.Tables.CurrentTable;

            _ctx.Save.Data.runsCompleted++;
            _ctx.Save.Persist();

            _ctx.Analytics.Track("run_end",
                "banked", banked, "amount", bankedAmount, "spins", SpinsThisRun,
                "level", _ctx.Xp.Level, "paid", _ctx.Save.Data.lastRunPaid,
                "quota_met", _ctx.Save.Data.lastRunMetQuota, "debt", _ctx.Debt.Debt,
                "table", _ctx.Tables.CurrentTable, "marks", _ctx.Marks.EarnedCount);

            _ctx.Hud?.ShowRunEnd(banked, bankedAmount, SpinsThisRun, LastDebtOutcome);
        }

        /// <summary>Understudy: the last boon you earned is yours again on arrival.</summary>
        private void ApplyRememberedBoon(string sinId)
        {
            if (string.IsNullOrEmpty(sinId)) return;
            switch (sinId)
            {
                case "sloth": _ctx.Run.SlothCooldownBonus = 0.3f; break;
                case "pride": _ctx.Run.PrideOddsLocked = true; break;
                case "lust": _ctx.Run.LustRingLocked = true; break;
                case "wrath": _ctx.Run.ExtraWedges.Add("coin_mid"); break;
                default: _ctx.Run.GuaranteedRewardSpins++; break;
            }
            _ctx.Hud?.Toast($"UNDERSTUDY KEEPS {sinId.ToUpperInvariant()}", Palette.Teal);
        }

        private void HandleLevelUp(int newLevel)
        {
            Sfx.LevelUp();
            _ctx.Hud?.Toast($"LEVEL {newLevel}", Palette.Gold);

            foreach (var sin in _ctx.Config.Sins.sins)
            {
                if (sin.unlockLevel == newLevel)
                    _ctx.Hud?.Toast($"{sin.displayName.ToUpperInvariant()} HAS NOTICED YOU", Palette.Purple);
            }

            _ctx.Analytics.Track("level_up", "level", newLevel);
            _ctx.Save.Persist();
        }
    }
}
