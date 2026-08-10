using System.Collections.Generic;
using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// Run-scoped state that outlives a single encounter. Breaking a sin does
    /// not merely end it — it leaves a benefit for the rest of the run, and
    /// that is what makes fighting worth the risk over waiting out the timer.
    /// Interlude winnings live here too, on their own clocks.
    /// </summary>
    public sealed class RunState
    {
        /// <summary>Wedge ids landed this run. Envy breaks when you find one it has not seen.</summary>
        public readonly HashSet<string> WedgesHit = new HashSet<string>();

        /// <summary>Wedges won for the remainder of the run (Wrath's teeth, turned to coin).</summary>
        public readonly List<string> ExtraWedges = new List<string>();

        public bool PrideOddsLocked;   // Pride's shrink cannot apply again this run
        public bool LustRingLocked;    // the ring stops shuffling
        public float SlothCooldownBonus;

        /// <summary>The Vigil's winnings: faster spins, but only until the next table.</summary>
        public float TableCooldownBonus;

        /// <summary>The Shell's winnings: the next spin cannot be a risk wedge.</summary>
        public int GuaranteedRewardSpins;

        /// <summary>The Understudy's winnings: wedges already rolled, in order.</summary>
        public readonly List<string> ForeseenWedges = new List<string>();

        public void Reset()
        {
            WedgesHit.Clear();
            ExtraWedges.Clear();
            PrideOddsLocked = false;
            LustRingLocked = false;
            SlothCooldownBonus = 0f;
            TableCooldownBonus = 0f;
            GuaranteedRewardSpins = 0;
            ForeseenWedges.Clear();
        }

        /// <summary>
        /// Roll the next few spins now and remember them, so the preview the
        /// player is shown is the truth rather than a guess.
        /// </summary>
        public void ForeseeSpins(GameContext ctx, int count)
        {
            ForeseenWedges.Clear();
            var ring = ctx.Ring.Effective;
            if (ring.Count == 0) return;

            for (int i = 0; i < count; i++)
            {
                float total = 0f;
                for (int w = 0; w < ring.Count; w++) total += Mathf.Max(0.01f, ring[w].weight);

                double roll = ctx.Rng.NextDouble() * total;
                for (int w = 0; w < ring.Count; w++)
                {
                    roll -= Mathf.Max(0.01f, ring[w].weight);
                    if (roll <= 0)
                    {
                        ForeseenWedges.Add(ring[w].id);
                        break;
                    }
                }
            }
        }
    }
}
