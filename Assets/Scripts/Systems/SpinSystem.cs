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
        private OutcomeResult? _withheldOutcome;
        private int _calledWedge = -1;

        public SpinState State { get; private set; } = SpinState.Idle;
        public float CooldownRemaining => Mathf.Max(0f, _cooldownRemaining);
        public float CurrentCooldownDuration { get; private set; } = 1f;

        /// <summary>The wedge the Croupier named, or -1. The HUD shows it before you commit.</summary>
        public int CalledWedge => _calledWedge;

        public bool CanSpin => State == SpinState.Idle && _ctx.Game.RunActive;

        public SpinSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public void ResetForRun()
        {
            State = SpinState.Idle;
            _cooldownRemaining = 0f;
            _withheldOutcome = null;
            _calledWedge = -1;
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

            // Committing to another spin is what buys you the last one's result.
            if (_withheldOutcome.HasValue)
            {
                _ctx.Hud.ShowOutcome(_withheldOutcome.Value);
                _withheldOutcome = null;
            }

            _ctx.Bosses.OnSpinStarted(); // Gluttony charges its toll here

            int index = RollWedge(ring);
            bool called = _calledWedge >= 0 && _calledWedge == index;
            _calledWedge = -1;

            State = SpinState.Spinning;
            _ctx.Game.SpinsThisRun++;
            _ctx.Analytics.TrackSpin(index, ring[index].type, _ctx.Bosses.EncounterActive);

            var landed = ring[index];
            _ctx.Hud.Wheel.SpinTo(index, _ctx.Config.Tuning.spinAnimDuration, () => Resolve(landed, called));
        }

        /// <summary>
        /// Picks the landing wedge. Foresight is spent first — the preview the
        /// player was shown has to be the truth — then the Shell's promise of a
        /// kind spin, then the ordinary weighted roll.
        /// </summary>
        private int RollWedge(IReadOnlyList<SegmentConfig> ring)
        {
            if (_ctx.Run.ForeseenWedges.Count > 0)
            {
                string id = _ctx.Run.ForeseenWedges[0];
                _ctx.Run.ForeseenWedges.RemoveAt(0);
                for (int i = 0; i < ring.Count; i++)
                    if (ring[i].id == id) return i;
                // The ring changed under the prophecy; fall through and re-roll.
            }

            if (_ctx.Run.GuaranteedRewardSpins > 0)
            {
                _ctx.Run.GuaranteedRewardSpins--;
                for (int attempt = 0; attempt < 24; attempt++)
                {
                    int index = RollWeighted(ring);
                    if (ring[index].IsReward) return index;
                }
            }

            return RollWeighted(ring);
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

        private void Resolve(SegmentConfig segment, bool calledCorrectly)
        {
            State = SpinState.Resolving;
            bool hadEncounter = _ctx.Bosses.EncounterActive;

            // Streak first: a reward that completes the chain should be paid at
            // the multiplier it just earned.
            _ctx.Streak.OnLanded(segment);

            if (calledCorrectly)
            {
                // He named it before you spun, so it pays nothing.
                _ctx.Hud.ShowOutcome(new OutcomeResult
                {
                    Type = segment.ParsedType,
                    Text = "HE CALLED IT",
                    Color = Palette.Dim
                });
            }
            else
            {
                OutcomeResult result = OutcomeResolver.Apply(_ctx, segment);
                if (_ctx.Tables.BlindSpin) _withheldOutcome = result;
                else _ctx.Hud.ShowOutcome(result);
            }

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

            // He names the next wedge while you think about it.
            if (_ctx.Tables.CallsTheWedge && _ctx.Ring.Count > 0)
                _calledWedge = _ctx.Rng.Next(_ctx.Ring.Count);
        }
    }
}
