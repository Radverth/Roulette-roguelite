using System.Collections.Generic;

namespace SinWheel
{
    /// <summary>
    /// Run-scoped state that outlives a single encounter. Breaking a sin does
    /// not merely end it — it leaves a benefit for the rest of the run, and
    /// that is what makes fighting worth the risk over waiting out the timer.
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

        public void Reset()
        {
            WedgesHit.Clear();
            ExtraWedges.Clear();
            PrideOddsLocked = false;
            LustRingLocked = false;
            SlothCooldownBonus = 0f;
        }
    }
}
