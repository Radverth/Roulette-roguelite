using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// The ring is the build. This owns the player's persistent wedge list
    /// (catalog id + temper tier per slot), applies run-scoped warping — the
    /// active sin's splices, Lust's shuffles, unpaid-debt penalty wedges — and
    /// publishes one effective ring that the spin roll and the disc renderer
    /// both read. Version bumps whenever the shape changes so the wheel knows
    /// to redraw.
    /// </summary>
    public sealed class WheelRingSystem
    {
        private readonly GameContext _ctx;
        private List<SegmentConfig> _effective = new List<SegmentConfig>();
        private int[] _shuffleOrder;

        public IReadOnlyList<SegmentConfig> Effective => _effective;
        public int Version { get; private set; }
        public int Count => _effective.Count;

        public WheelRingSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public SegmentConfig Template(string id) =>
            _ctx.Config.Wheel.catalog.FirstOrDefault(c => c.id == id);

        public List<RingSlot> Slots => _ctx.Save.Data.ring;

        /// <summary>Seed a fresh player's ring from the authored starting layout.</summary>
        public void EnsureSeeded()
        {
            if (Slots.Count > 0) return;
            foreach (string id in _ctx.Config.Wheel.startingRing)
                Slots.Add(new RingSlot { templateId = id, tier = 1 });
        }

        // --- Forge operations ---

        public void AddWedge(string templateId, int tier = 1)
        {
            // Splice opposite the pointer rather than appending, so a growing
            // ring stays visually balanced instead of clumping.
            int at = Slots.Count / 2;
            Slots.Insert(at, new RingSlot { templateId = templateId, tier = Mathf.Max(1, tier) });
            Rebuild();
        }

        public void StrikeWedge(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= Slots.Count) return;
            Slots.RemoveAt(slotIndex);
            Rebuild();
        }

        public void TemperWedge(int slotIndex, int steps)
        {
            if (slotIndex < 0 || slotIndex >= Slots.Count) return;
            Slots[slotIndex].tier = Mathf.Clamp(Slots[slotIndex].tier + steps, 1, _ctx.Config.Tuning.forgeMaxTier);
            Rebuild();
        }

        /// <summary>Ring slots that could still take a temper (reward wedges below max tier).</summary>
        public List<int> TemperableSlots()
        {
            var result = new List<int>();
            for (int i = 0; i < Slots.Count; i++)
            {
                var template = Template(Slots[i].templateId);
                if (template == null || template.IsRisk) continue;
                if (Slots[i].tier >= _ctx.Config.Tuning.forgeMaxTier) continue;
                result.Add(i);
            }
            return result;
        }

        /// <summary>Ring slots the player could remove. Risk first — that is the point of Strike.</summary>
        public List<int> StrikeableSlots()
        {
            var result = new List<int>();
            for (int i = 0; i < Slots.Count; i++)
            {
                var template = Template(Slots[i].templateId);
                if (template == null) continue;
                result.Add(i);
            }
            return result;
        }

        // --- Run-scoped warping ---

        /// <summary>Lust reorders the ring; the disc redraws to match.</summary>
        public void Shuffle()
        {
            int n = _effective.Count;
            if (n < 2) return;

            var order = Enumerable.Range(0, n).ToArray();
            for (int i = n - 1; i > 0; i--)
            {
                int j = _ctx.Rng.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            _shuffleOrder = order;
            Rebuild();
        }

        public void ClearShuffle()
        {
            _shuffleOrder = null;
            Rebuild();
        }

        /// <summary>
        /// Recompute the effective ring. Called on run start, encounter
        /// start/end, Forge edits and shuffles — never per frame.
        /// </summary>
        public void Rebuild()
        {
            EnsureSeeded();

            var ring = new List<SegmentConfig>();
            foreach (var slot in Slots)
            {
                var template = Template(slot.templateId);
                if (template == null)
                {
                    Debug.LogError($"[Ring] Unknown wedge template '{slot.templateId}', skipping");
                    continue;
                }
                var wedge = template.Clone();
                wedge.tier = Mathf.Clamp(slot.tier, 1, _ctx.Config.Tuning.forgeMaxTier);
                ring.Add(wedge);
            }

            // The house adds a wedge for every run that left the quota unpaid.
            int penalty = Mathf.Clamp(_ctx.Save.Data.penaltyRiskWedges, 0, _ctx.Config.Tuning.maxPenaltyRiskWedges);
            for (int i = 0; i < penalty; i++)
            {
                var template = Template("damage_small");
                if (template == null) break;
                var wedge = template.Clone();
                wedge.id = "damage_debt";
                ring.Insert(Mathf.Min(ring.Count, 3 + i * 4), wedge);
            }

            // Wedges won for the rest of the run (Wrath's teeth, turned to coin).
            if (_ctx.Run != null)
            {
                foreach (string id in _ctx.Run.ExtraWedges)
                {
                    var template = Template(id);
                    if (template != null) ring.Add(template.Clone());
                }
            }

            // The descent and the Marks each put wedges of their own in the ring.
            int houseWedges = (_ctx.Tables?.ExtraRiskWedges ?? 0) + (_ctx.Marks?.ExtraRiskWedges ?? 0);
            for (int i = 0; i < houseWedges; i++)
            {
                var template = Template("damage_small");
                if (template == null) break;
                var wedge = template.Clone();
                wedge.id = "damage_house";
                ring.Insert(Mathf.Min(ring.Count, 5 + i * 4), wedge);
            }

            if (_ctx.Bosses != null && _ctx.Bosses.EncounterActive)
                ring = _ctx.Bosses.ModifySegments(ring);

            if (_shuffleOrder != null && _shuffleOrder.Length == ring.Count)
            {
                var shuffled = new List<SegmentConfig>(ring.Count);
                foreach (int index in _shuffleOrder) shuffled.Add(ring[index]);
                ring = shuffled;
            }
            else
            {
                _shuffleOrder = null;
            }

            _effective = ring;
            Version++;
        }
    }
}
