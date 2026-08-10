using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// Consecutive reward wedges build a chain; any risk wedge wipes it. This
    /// is what puts a decision on every spin rather than once per run — with a
    /// fat multiplier live, tithing now can beat spinning again.
    /// </summary>
    public sealed class StreakSystem
    {
        private readonly GameContext _ctx;

        public int Count { get; private set; }
        /// <summary>True on the spin that just broke a live chain, for the burst VFX.</summary>
        public bool JustBroke { get; private set; }

        public StreakSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public bool IsLive => Count >= _ctx.Config.Tuning.streakStartsAt;

        public float Multiplier
        {
            get
            {
                var t = _ctx.Config.Tuning;
                if (Count < t.streakStartsAt) return 1f;
                float m = 1f + t.streakStepMultiplier * (Count - (t.streakStartsAt - 1));
                return Mathf.Min(t.streakMaxMultiplier, m);
            }
        }

        public void ResetForRun()
        {
            Count = 0;
            JustBroke = false;
        }

        public void OnLanded(SegmentConfig segment)
        {
            JustBroke = false;
            if (segment.IsRisk)
            {
                JustBroke = IsLive;
                Count = 0;
                return;
            }
            Count++;
        }
    }
}
