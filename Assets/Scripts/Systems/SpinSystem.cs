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

            List<SegmentConfig> segments = _ctx.Bosses.GetEffectiveSegments(_ctx.Config.Wheel.segments);
            _ctx.Bosses.OnSpinStarted(); // Gluttony charges its toll here

            int index = RollWeighted(segments);
            State = SpinState.Spinning;
            _ctx.Game.SpinsThisRun++;
            _ctx.Analytics.TrackSpin(index, segments[index].type, _ctx.Bosses.EncounterActive);

            _ctx.Hud.Wheel.SpinTo(index, _ctx.Config.Tuning.spinAnimDuration,
                () => Resolve(segments[index]));
        }

        private int RollWeighted(List<SegmentConfig> segments)
        {
            float total = 0f;
            foreach (var s in segments) total += Mathf.Max(0.01f, s.weight);

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

            OutcomeResult result = OutcomeResolver.Apply(_ctx, segment);
            _ctx.Hud.ShowOutcome(result);

            _ctx.Buffs.TickSpin();
            _ctx.Bosses.OnSpinResolved();
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
