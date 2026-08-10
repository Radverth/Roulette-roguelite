using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SinWheel
{
    public enum ForgeAction
    {
        Add,
        Strike,
        Temper
    }

    public enum ForgeRarity
    {
        Common,
        Rare,
        Cursed
    }

    /// <summary>One draft card. Its target is decided when the offer is made, so the card can name it.</summary>
    public sealed class ForgeOffer
    {
        public ForgeAction Action;
        public ForgeRarity Rarity;
        public string TemplateId;      // Add: what goes in. Temper/Strike: what is targeted.
        public int SlotIndex = -1;     // Temper/Strike target.
        public int TemperSteps = 1;
        public string CursedRiskId;    // cursed Add/Temper drags this in alongside
        public int CursedStrikeSlot = -1;
        public string Title;
        public string Detail;

        public string CardSprite =>
            $"Loop/draft_{Rarity.ToString().ToLowerInvariant()}_{Action.ToString().ToLowerInvariant()}";

        public string ActionSprite => $"Loop/action_{Action.ToString().ToLowerInvariant()}";
    }

    /// <summary>
    /// The Forge turns a fixed wheel into a deck: three cards between runs,
    /// take one. Offers are weighted rather than uniform, because uniform
    /// random offers produce boring wheels — see the rules in LOOP_DESIGN.md.
    /// </summary>
    public sealed class ForgeSystem
    {
        private readonly GameContext _ctx;

        public List<ForgeOffer> Offers { get; private set; } = new List<ForgeOffer>();
        public bool RerollUsed { get; private set; }

        public ForgeSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public int RerollCost => _ctx.Config.Tuning.forgeRerollCost;
        public bool CanReroll => !RerollUsed && _ctx.Wallet.MetaCoins >= RerollCost;

        /// <summary>Open a visit: three fresh offers, one reroll available.</summary>
        public void BeginVisit()
        {
            RerollUsed = false;
            _ctx.Save.Data.forgeVisits++;
            Offers = GenerateOffers();
        }

        public bool TryReroll()
        {
            if (!CanReroll) return false;
            if (!_ctx.Wallet.TrySpendMetaCoins(RerollCost)) return false;
            RerollUsed = true;
            Offers = GenerateOffers();
            _ctx.Analytics.Track("forge_reroll", "cost", RerollCost);
            return true;
        }

        public void Take(ForgeOffer offer)
        {
            if (offer == null) return;
            var ring = _ctx.Ring;

            switch (offer.Action)
            {
                case ForgeAction.Add:
                    ring.AddWedge(offer.TemplateId);
                    break;

                case ForgeAction.Strike:
                    // Strike the named slot first; a cursed strike takes a
                    // second wedge with it, so resolve the higher index first
                    // to keep the other index valid.
                    if (offer.CursedStrikeSlot >= 0)
                    {
                        int first = Mathf.Max(offer.SlotIndex, offer.CursedStrikeSlot);
                        int second = Mathf.Min(offer.SlotIndex, offer.CursedStrikeSlot);
                        ring.StrikeWedge(first);
                        ring.StrikeWedge(second);
                    }
                    else
                    {
                        ring.StrikeWedge(offer.SlotIndex);
                    }
                    break;

                case ForgeAction.Temper:
                    ring.TemperWedge(offer.SlotIndex, offer.TemperSteps);
                    break;
            }

            // Cursed offers are free, but the house gets its wedge.
            if (!string.IsNullOrEmpty(offer.CursedRiskId))
                ring.AddWedge(offer.CursedRiskId);

            _ctx.Analytics.Track("forge_take",
                "action", offer.Action.ToString(), "rarity", offer.Rarity.ToString(),
                "template", offer.TemplateId ?? "", "ring_size", ring.Slots.Count);

            _ctx.Save.Persist();
        }

        // --- Offer generation ---

        private List<ForgeOffer> GenerateOffers()
        {
            var data = _ctx.Save.Data;
            int ringSize = _ctx.Ring.Slots.Count;
            bool strikeAllowed = ringSize > _ctx.Config.Tuning.forgeMinRingSize;
            bool temperAllowed = _ctx.Ring.TemperableSlots().Count > 0;

            // Nobody should get locked into one shape by variance: force the
            // missing verb if it has been absent for two visits already.
            var forced = new List<ForgeAction>();
            if (data.visitsSinceAddOffered >= 2) forced.Add(ForgeAction.Add);
            if (strikeAllowed && data.visitsSinceStrikeOffered >= 2) forced.Add(ForgeAction.Strike);

            var actions = new List<ForgeAction>(forced);
            var pool = new List<ForgeAction> { ForgeAction.Add };
            if (strikeAllowed) pool.Add(ForgeAction.Strike);
            if (temperAllowed) pool.Add(ForgeAction.Temper);

            // Prefer three distinct verbs, then top up from the pool.
            foreach (var action in pool.OrderBy(_ => _ctx.Rng.Next()))
            {
                if (actions.Count >= 3) break;
                if (!actions.Contains(action)) actions.Add(action);
            }
            while (actions.Count < 3)
                actions.Add(pool[_ctx.Rng.Next(pool.Count)]);

            var offers = new List<ForgeOffer>();
            foreach (var action in actions.Take(3))
            {
                var offer = BuildOffer(action, ringSize);
                if (offer != null) offers.Add(offer);
            }

            data.visitsSinceAddOffered = offers.Any(o => o.Action == ForgeAction.Add)
                ? 0 : data.visitsSinceAddOffered + 1;
            data.visitsSinceStrikeOffered = offers.Any(o => o.Action == ForgeAction.Strike)
                ? 0 : data.visitsSinceStrikeOffered + 1;

            return offers;
        }

        private ForgeRarity RollRarity(int ringSize)
        {
            bool cursedAllowed = ringSize >= _ctx.Config.Tuning.forgeCursedFromRingSize;
            int roll = _ctx.Rng.Next(100);
            if (cursedAllowed && roll < 15) return ForgeRarity.Cursed;
            if (roll < 45) return ForgeRarity.Rare;
            return ForgeRarity.Common;
        }

        private ForgeOffer BuildOffer(ForgeAction action, int ringSize)
        {
            ForgeRarity rarity = RollRarity(ringSize);

            switch (action)
            {
                case ForgeAction.Add: return BuildAdd(rarity);
                case ForgeAction.Strike: return BuildStrike(rarity);
                default: return BuildTemper(rarity);
            }
        }

        private ForgeOffer BuildAdd(ForgeRarity rarity)
        {
            string wanted = rarity.ToString().ToLowerInvariant();
            var candidates = _ctx.Config.Wheel.catalog
                .Where(c => c.draftable && c.IsReward && c.rarity == wanted).ToList();

            if (candidates.Count == 0)
            {
                candidates = _ctx.Config.Wheel.catalog.Where(c => c.draftable && c.IsReward).ToList();
                rarity = ForgeRarity.Common;
            }
            if (candidates.Count == 0) return null;

            var pick = candidates[_ctx.Rng.Next(candidates.Count)];
            var offer = new ForgeOffer
            {
                Action = ForgeAction.Add,
                Rarity = rarity,
                TemplateId = pick.id,
                Title = pick.label,
                Detail = $"+{Mathf.RoundToInt(pick.amount)}"
            };

            if (rarity == ForgeRarity.Cursed)
            {
                offer.CursedRiskId = RandomRiskTemplateId();
                offer.Detail = $"+{Mathf.RoundToInt(pick.amount)} AND A WOUND";
            }
            return offer;
        }

        private ForgeOffer BuildStrike(ForgeRarity rarity)
        {
            var slots = _ctx.Ring.StrikeableSlots();
            if (slots.Count == 0) return null;

            // Weight toward risk wedges: clearing danger is what Strike is for,
            // but the occasional reward target keeps the choice honest.
            var riskSlots = slots.Where(i => IsRiskSlot(i)).ToList();
            int target = (riskSlots.Count > 0 && _ctx.Rng.NextDouble() < 0.7)
                ? riskSlots[_ctx.Rng.Next(riskSlots.Count)]
                : slots[_ctx.Rng.Next(slots.Count)];

            var template = _ctx.Ring.Template(_ctx.Ring.Slots[target].templateId);
            var offer = new ForgeOffer
            {
                Action = ForgeAction.Strike,
                Rarity = rarity,
                TemplateId = template?.id,
                SlotIndex = target,
                Title = template?.label ?? "WEDGE",
                Detail = "REMOVE"
            };

            // A cursed strike takes two wedges, so it needs one more than the
            // usual headroom or it would drop the ring below the floor.
            bool roomForDouble = _ctx.Ring.Slots.Count > _ctx.Config.Tuning.forgeMinRingSize + 1;

            if (offer.Rarity == ForgeRarity.Cursed && roomForDouble)
            {
                // The house takes one back: a second, random reward wedge goes too.
                var rewardSlots = slots.Where(i => i != target && !IsRiskSlot(i)).ToList();
                if (rewardSlots.Count > 0)
                {
                    offer.CursedStrikeSlot = rewardSlots[_ctx.Rng.Next(rewardSlots.Count)];
                    var second = _ctx.Ring.Template(_ctx.Ring.Slots[offer.CursedStrikeSlot].templateId);
                    offer.Detail = $"REMOVE - LOSE {second?.label ?? "A WEDGE"} TOO";
                }
                else
                {
                    offer.Rarity = ForgeRarity.Common;
                }
            }
            else if (offer.Rarity == ForgeRarity.Cursed)
            {
                offer.Rarity = ForgeRarity.Rare;
            }
            return offer;
        }

        private ForgeOffer BuildTemper(ForgeRarity rarity)
        {
            var slots = _ctx.Ring.TemperableSlots();
            if (slots.Count == 0) return null;

            int target = slots[_ctx.Rng.Next(slots.Count)];
            var template = _ctx.Ring.Template(_ctx.Ring.Slots[target].templateId);
            int steps = rarity == ForgeRarity.Common ? 1 : 2;

            var offer = new ForgeOffer
            {
                Action = ForgeAction.Temper,
                Rarity = rarity,
                TemplateId = template?.id,
                SlotIndex = target,
                TemperSteps = steps,
                Title = template?.label ?? "WEDGE",
                Detail = steps > 1 ? "UPGRADE X2" : "UPGRADE"
            };

            if (rarity == ForgeRarity.Cursed)
            {
                offer.CursedRiskId = RandomRiskTemplateId();
                offer.Detail = "UPGRADE X2 AND A WOUND";
            }
            return offer;
        }

        private bool IsRiskSlot(int slotIndex)
        {
            var template = _ctx.Ring.Template(_ctx.Ring.Slots[slotIndex].templateId);
            return template != null && template.IsRisk;
        }

        private string RandomRiskTemplateId()
        {
            var risks = _ctx.Config.Wheel.catalog.Where(c => c.draftable && c.IsRisk).ToList();
            return risks.Count == 0 ? null : risks[_ctx.Rng.Next(risks.Count)].id;
        }
    }
}
