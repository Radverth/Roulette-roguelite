using System.Collections.Generic;
using UnityEngine;

namespace SinWheel
{
    public enum MultOp
    {
        Add,      // brass: raises the multiplier
        Multiply, // violet: scales it
        Reduce    // red: takes from it
    }

    public struct MultTerm
    {
        public string Label;
        public MultOp Op;
        public float Value;
    }

    /// <summary>
    /// One spin's arithmetic, assembled so the player can watch it happen.
    /// Every multiplier in the game feeds this single figure — Table, Streak,
    /// blessings, sins and Pledges stop competing for attention the moment
    /// they contribute to the same number.
    /// </summary>
    public sealed class ScoreBreakdown
    {
        public int Take;
        public float Mult = 1f;
        public int Total;
        public readonly List<MultTerm> Terms = new List<MultTerm>();

        public void Add(string label, float value)
        {
            if (Mathf.Approximately(value, 0f)) return;
            Terms.Add(new MultTerm { Label = label, Op = MultOp.Add, Value = value });
        }

        public void Multiply(string label, float value)
        {
            if (Mathf.Approximately(value, 1f)) return;
            Terms.Add(new MultTerm
            {
                Label = label,
                Op = value < 1f ? MultOp.Reduce : MultOp.Multiply,
                Value = value
            });
        }

        /// <summary>Additions raise the base, then every scale applies to the result.</summary>
        public float Resolve()
        {
            Mult = ResolveTerms(Terms);
            return Mult;
        }

        /// <summary>
        /// The same arithmetic over an arbitrary slice, so the panel can show a
        /// running figure as each chip lands and still finish on the total.
        /// </summary>
        public static float ResolveTerms(IReadOnlyList<MultTerm> terms)
        {
            float mult = 1f;
            for (int i = 0; i < terms.Count; i++)
                if (terms[i].Op == MultOp.Add) mult += terms[i].Value;

            for (int i = 0; i < terms.Count; i++)
                if (terms[i].Op != MultOp.Add) mult *= terms[i].Value;

            return Mathf.Max(0f, mult);
        }

        /// <summary>Below this the assembly animation is noise, so the HUD skips it.</summary>
        public bool WorthShowing => Terms.Count >= 2;

        public static string ChipSprite(MultOp op)
        {
            switch (op)
            {
                case MultOp.Add: return "Pledges/term_chip_add";
                case MultOp.Reduce: return "Pledges/term_chip_reduce";
                default: return "Pledges/term_chip_mult";
            }
        }

        public static string OpSprite(MultOp op)
        {
            switch (op)
            {
                case MultOp.Add: return "Pledges/op_plus";
                case MultOp.Reduce: return "Pledges/op_minus";
                default: return "Pledges/op_times";
            }
        }
    }
}
