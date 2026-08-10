using System.Collections.Generic;
using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// Idle → Spinning → Resolving → Cooldown → Idle.
    /// One spin per tap; cooldown is upgrade-reduced and boss-warped (Sloth).
    /// </summary>
    public enum SpinState
    {
        Idle,
        Spinning,
        Resolving,
        Cooldown
    }

    public sealed class SpinSystem
    {
        private readonly GameContext _ctx;
        private float _cooldownRemaining;

        public SpinState State { get; private set; } = SpinState.Idle;
        public float CooldownRemaining => Mathf.Max(0f, _cooldownRemaining);
        public float CurrentCooldownDuration { get; private set; } = 1f;

        public bool CanSpin => State == SpinState.Idle && _ctx.Game.RunActive;

        public SpinSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public void ResetForRun()
        {
            State = SpinState.Idle;
            _cooldownRemaining = 0f;
        }

        public void Tick(float dt)
        {
            if (State != SpinState.Cooldown) return;
            _cooldownRemaining -= dt;
            if (_cooldownRemaining <= 0f)
                State = SpinState.Idle;
        }

        public void RequestSpin()
        {
            if (!CanSpin) return;

            IReadOnlyList<SegmentConfig> ring = _ctx.Ring.Effective;
            if (ring.Count == 0) return;

            _ctx.Bosses.OnSpinStarted(); // Gluttony charges its toll here

            int index = RollWeighted(ring);
            State = SpinState.Spinning;
            _ctx.Game.SpinsThisRun++;
            _ctx.Analytics.TrackSpin(index, ring[index].type, _ctx.Bosses.EncounterActive);

            var landed = ring[index];
            _ctx.Hud.Wheel.SpinTo(index, _ctx.Config.Tuning.spinAnimDuration, () => Resolve(landed));
        }

        private int RollWeighted(IReadOnlyList<SegmentConfig> segments)
        {
            float total = 0f;
            for (int i = 0; i < segments.Count; i++) total += Mathf.Max(0.01f, segments[i].weight);

            double roll = _ctx.Rng.NextDouble() * total;
            for (int i = 0; i < segments.Count; i++)
            {
                roll -= Mathf.Max(0.01f, segments[i].weight);
                if (roll <= 0) return i;
            }
            return segments.Count - 1;
        }

        private void Resolve(SegmentConfig segment)
        {
            State = SpinState.Resolving;
            bool hadEncounter = _ctx.Bosses.EncounterActive;

            // Streak first: a reward that completes the chain should be paid at
            // the multiplier it just earned.
            _ctx.Streak.OnLanded(segment);

            OutcomeResult result = OutcomeResolver.Apply(_ctx, segment);
            _ctx.Hud.ShowOutcome(result);
            if (_ctx.Streak.JustBroke) _ctx.Hud.ShowStreakBreak();

            _ctx.Buffs.TickSpin();
            _ctx.Notice.OnSpin(_ctx.Wallet.RunCoins);

            if (hadEncounter)
            {
                if (_ctx.Bosses.EncounterActive) _ctx.Bosses.OnSpinResolved(segment);
            }
            else
            {
                // A full meter spends itself on the next risk wedge.
                _ctx.Bosses.TryForcedSummon(segment);
            }

            // Recorded after the boss hook so Envy can ask what this run has
            // never produced.
            _ctx.Run.WedgesHit.Add(segment.id);
            _ctx.Game.AfterSpinResolved();

            if (!_ctx.Game.RunActive)
            {
                State = SpinState.Idle;
                return;
            }

            CurrentCooldownDuration = _ctx.Bosses.ModifyCooldown(_ctx.Upgrades.EffectiveSpinCooldown());
            _cooldownRemaining = CurrentCooldownDuration;
            State = SpinState.Cooldown;
        }
    }
}
