using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// Owns sin summoning and the active encounter. Pressure is the Notice
    /// meter now: it rises with spins, tithes and a fat purse, and when it
    /// fills, the next risk wedge is a certainty rather than a coin flip.
    /// Bosses unlock by level, then get picked weighted-random so returning
    /// players face variety instead of a fixed order.
    /// </summary>
    public sealed class SinBossSystem
    {
        private readonly GameContext _ctx;

        public BossEncounter Encounter { get; private set; }
        public bool EncounterActive => Encounter != null;

        public float CurrentRewardMultiplier => Encounter?.RewardMultiplier ?? 1f;

        public SinBossSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public void ResetForRun()
        {
            Encounter = null;
        }

        public List<SinBossConfig> UnlockedSins()
        {
            return _ctx.Config.Sins.sins
                .Where(s => s.implemented && s.unlockLevel <= _ctx.Xp.Level)
                .ToList();
        }

        /// <summary>The wheel landed on the summon wedge. Returns the banner line.</summary>
        public string OnSummonSegmentHit()
        {
            if (EncounterActive)
            {
                float dmg = _ctx.Health.ApplyDamage(5f * _ctx.Buffs.DamageMultiplier);
                return $"THE SIN STIRS -{Mathf.RoundToInt(dmg)} HP";
            }

            var candidates = UnlockedSins();
            if (candidates.Count == 0)
                return "SOMETHING WATCHES";

            // Summon chance is the base plus however far the Notice has filled.
            var t = _ctx.Config.Tuning;
            float chance = Mathf.Min(t.sinSummonChanceMax, t.sinSummonBaseChance + _ctx.Notice.Fill);
            if (_ctx.Rng.NextDouble() < chance)
            {
                StartEncounter(PickWeighted(candidates));
                return $"{Encounter.Config.displayName.ToUpperInvariant()} AWAKENS";
            }

            return $"SIN CHANCE {Mathf.RoundToInt(chance * 100)}%";
        }

        /// <summary>
        /// A full Notice meter spends itself on the next risk wedge: the sin
        /// arrives, guaranteed, and the eye closes again.
        /// </summary>
        public bool TryForcedSummon(SegmentConfig landed)
        {
            if (EncounterActive || !landed.IsRisk || !_ctx.Notice.IsFull) return false;

            var candidates = UnlockedSins();
            if (candidates.Count == 0) return false;

            _ctx.Notice.ConsumeFull();
            StartEncounter(PickWeighted(candidates));
            return true;
        }

        private SinBossConfig PickWeighted(List<SinBossConfig> candidates)
        {
            float total = candidates.Sum(c => Mathf.Max(0.01f, c.weight));
            double roll = _ctx.Rng.NextDouble() * total;
            foreach (var c in candidates)
            {
                roll -= Mathf.Max(0.01f, c.weight);
                if (roll <= 0) return c;
            }
            return candidates[candidates.Count - 1];
        }

        private void StartEncounter(SinBossConfig cfg)
        {
            Encounter = new BossEncounter
            {
                Config = cfg,
                Modifier = BossModifierFactory.Create(_ctx, cfg),
                SpinsRemaining = cfg.durationSpins,
                SpinsElapsed = 0
            };

            int visits = SaveData.IncrementCount(_ctx.Save.Data.sinEncounters, cfg.id);
            _ctx.Narrative.BeginEncounter(cfg.id, visits >= 3);
            _ctx.Ring.Rebuild(); // the sin's splices join the wheel

            _ctx.Analytics.Track("boss_encounter_start", "sin", cfg.id, "player_level", _ctx.Xp.Level);
            _ctx.Hud?.OnBossStarted(Encounter);
            Sfx.Boss();
            Haptics.Heavy();
        }

        // --- Hooks called by SpinSystem / GameManager ---

        public float ModifyCooldown(float cooldown)
        {
            float withBoss = EncounterActive ? Encounter.Modifier.ModifyCooldown(cooldown) : cooldown;
            return Mathf.Max(0.1f, withBoss - _ctx.Run.SlothCooldownBonus);
        }

        public float ModifyRewardMultiplier(float multiplier) =>
            EncounterActive ? Encounter.Modifier.ModifyRewardMultiplier(multiplier) : multiplier;

        public int ModifyCoinGain(int coins) =>
            EncounterActive ? Encounter.Modifier.ModifyCoinGain(coins) : coins;

        public float ModifyDamage(float damage) =>
            EncounterActive ? Encounter.Modifier.ModifyDamage(damage) : damage;

        public void OnSpinStarted()
        {
            if (EncounterActive) Encounter.Modifier.OnSpinStarted(Encounter);
        }

        /// <summary>Gluttony's exit: paying mid-encounter buys your way out.</summary>
        public void OnTithe()
        {
            if (!EncounterActive) return;
            Encounter.Modifier.OnTithe(Encounter);
            if (Encounter.Modifier.IsDefeated(Encounter))
                EndEncounter(defeated: true);
        }

        public void OnSpinResolved(SegmentConfig landed)
        {
            if (!EncounterActive) return;

            Encounter.SpinsElapsed++;
            Encounter.SpinsRemaining--;
            Encounter.Modifier.OnSpinResolved(Encounter, landed);

            if (Encounter.Modifier.IsDefeated(Encounter))
            {
                EndEncounter(defeated: true);
                return;
            }
            if (Encounter.SpinsRemaining <= 0)
            {
                EndEncounter(defeated: false);
                return;
            }

            _ctx.Hud?.OnBossUpdated(Encounter);

            // The encounter has a middle: an occasional taunt, drawn without
            // replacement so nothing repeats within one visit.
            if (Encounter.SpinsElapsed >= 2 && Encounter.SpinsElapsed % 3 == 0
                && _ctx.Rng.NextDouble() < 0.6)
            {
                string taunt = _ctx.Narrative.NextTaunt();
                if (taunt != null)
                    _ctx.Hud?.ShowSpeech(Encounter.Config.id, taunt);
            }
        }

        /// <summary>Run died or banked out while a sin was active — log the drop-off.</summary>
        public void AbandonEncounter(string reason)
        {
            if (!EncounterActive) return;

            if (reason == "banked_out")
                _ctx.Narrative.SetRunEndQuote(
                    _ctx.Narrative.EncounterEndLine(Encounter.Config.id, "player_fled"), 2);

            _ctx.Analytics.Track("boss_encounter_end",
                "sin", Encounter.Config.id, "outcome", reason, "spins", Encounter.SpinsElapsed);
            Encounter = null;
            _ctx.Hud?.OnBossEnded();
            _ctx.Ring.Rebuild();
        }

        private void EndEncounter(bool defeated)
        {
            var encounter = Encounter;
            var cfg = encounter.Config;
            int spins = encounter.SpinsElapsed;

            if (defeated)
            {
                encounter.Modifier.OnBroken(encounter);
                _ctx.Wallet.AddRunCoins(cfg.defeatCoins);
                _ctx.Wallet.AddGems(cfg.defeatGems);
                _ctx.Notice.OnSinBroken();
                _ctx.Hud?.Toast($"{cfg.displayName.ToUpperInvariant()} BROKEN +{cfg.defeatCoins}C +{cfg.defeatGems}G", Palette.Gold);

                int defeats = SaveData.IncrementCount(_ctx.Save.Data.sinDefeats, cfg.id);
                string fragKey = cfg.id + "_3";
                if (defeats >= 3 && !_ctx.Save.Data.unlockedFragments.Contains(fragKey))
                {
                    _ctx.Save.Data.unlockedFragments.Add(fragKey);
                    _ctx.Narrative.SetPendingFragment(_ctx.Narrative.FragmentFor(cfg.id));
                    _ctx.Analytics.Track("fragment_unlocked", "sin", cfg.id);
                }
            }
            else
            {
                _ctx.Wallet.AddRunCoins(cfg.surviveCoins);
                _ctx.Hud?.Toast($"OUTLASTED {cfg.displayName.ToUpperInvariant()} +{cfg.surviveCoins}C", Palette.Bone);
            }

            _ctx.Notice.OnEncounterEnded();
            _ctx.Hud?.ShowSpeech(cfg.id,
                _ctx.Narrative.EncounterEndLine(cfg.id, defeated ? "defeated" : "expired"));

            _ctx.Analytics.Track("boss_encounter_end",
                "sin", cfg.id, "outcome", defeated ? "defeated" : "survived", "spins", spins);

            Encounter = null;
            _ctx.Hud?.OnBossEnded();
            _ctx.Ring.Rebuild(); // the splices leave with it
            Sfx.LevelUp();
        }
    }
}
