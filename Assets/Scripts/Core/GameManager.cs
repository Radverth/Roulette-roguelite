using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// Run lifecycle: a run lasts until resilience hits zero (unbanked coins
    /// forfeit) or the player banks out voluntarily (never punished — the whole
    /// tension is choosing when to stop spinning).
    /// </summary>
    public sealed class GameManager
    {
        private readonly GameContext _ctx;

        public bool RunActive { get; private set; }
        public int SpinsThisRun;
        private bool _greedNoticedThisRun;

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
            SpinsThisRun = 0;
            _greedNoticedThisRun = false;
            RunActive = true;

            _ctx.Analytics.Track("run_start", "level", _ctx.Xp.Level);
            _ctx.Hud?.OnRunStarted();

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

            // Reactive: a long unbanked streak earns approval from the wrong quarter.
            if (RunActive && !_greedNoticedThisRun && SpinsThisRun == 15 && _ctx.Wallet.RunCoins > 0)
            {
                _greedNoticedThisRun = true;
                var line = _ctx.Narrative.Reactive?.never_banked_this_run;
                if (line != null)
                    _ctx.Hud?.ShowSpeech(line.speaker, line.line);
            }
        }

        public void BankAndEndRun()
        {
            if (!RunActive || _ctx.Spin.State == SpinState.Spinning) return;

            int banked = _ctx.Wallet.BankRunCoins(_ctx.Upgrades.BankingBonusMultiplier());
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

            _ctx.Analytics.Track("bank", "amount", banked, "spins", SpinsThisRun);
            EndRun(banked: true, bankedAmount: banked);
        }

        private void EndRun(bool banked, int bankedAmount)
        {
            RunActive = false;
            int purseAtEnd = banked ? bankedAmount : _ctx.Wallet.RunCoins;

            // Boss drop-off is the metric the sin difficulty curve is tuned on.
            // A fled sin may claim the ledger quote (priority 2) before the
            // Croupier's bookend (priority 1) is offered.
            _ctx.Bosses.AbandonEncounter(banked ? "banked_out" : "died");
            _ctx.Narrative.ChooseRunEndQuote(banked, purseAtEnd);

            if (!banked)
                _ctx.Wallet.ResetRun(); // unbanked winnings are forfeit

            _ctx.Save.Data.runsCompleted++;
            _ctx.Save.Persist();

            _ctx.Analytics.Track("run_end",
                "banked", banked, "amount", bankedAmount, "spins", SpinsThisRun, "level", _ctx.Xp.Level);

            _ctx.Hud?.ShowRunEnd(banked, bankedAmount, SpinsThisRun);
        }

        private void HandleLevelUp(int newLevel)
        {
            Sfx.LevelUp();
            _ctx.Hud?.Toast($"LEVEL {newLevel}!", Palette.Gold);

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
