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
            RunActive = true;

            _ctx.Analytics.Track("run_start", "level", _ctx.Xp.Level);
            _ctx.Hud?.OnRunStarted();
        }

        /// <summary>Called by SpinSystem after every resolved spin.</summary>
        public void AfterSpinResolved()
        {
            if (RunActive && _ctx.Health.IsDead)
                EndRun(banked: false, bankedAmount: 0);
        }

        public void BankAndEndRun()
        {
            if (!RunActive || _ctx.Spin.State == SpinState.Spinning) return;

            int banked = _ctx.Wallet.BankRunCoins(_ctx.Upgrades.BankingBonusMultiplier());
            if (banked > _ctx.Save.Data.bestSingleBank)
                _ctx.Save.Data.bestSingleBank = banked;

            _ctx.Analytics.Track("bank", "amount", banked, "spins", SpinsThisRun);
            EndRun(banked: true, bankedAmount: banked);
        }

        private void EndRun(bool banked, int bankedAmount)
        {
            RunActive = false;

            // Boss drop-off is the metric the sin difficulty curve is tuned on.
            _ctx.Bosses.AbandonEncounter(banked ? "banked_out" : "died");

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
