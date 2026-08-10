using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// Owns sin summoning and the active encounters. Pressure is the Notice
    /// meter: it rises with spins, tithes and a fat purse, and when it fills the
    /// next risk wedge is a certainty rather than a coin flip.
    ///
    /// From Table IV the house sends them in pairs. The modifiers were built as
    /// independent hooks, so stacking is a matter of folding over a list —
    /// Greed taxing payouts while Lust reshuffles the ring composes for free.
    /// </summary>
    public sealed class SinBossSystem
    {
        private readonly GameContext _ctx;
        private readonly List<BossEncounter> _encounters = new List<BossEncounter>();
        private int _spinOfLastEncounterEnd = int.MinValue / 2;

        public IReadOnlyList<BossEncounter> Encounters => _encounters;
        public bool EncounterActive => _encounters.Count > 0;

        /// <summary>The one the strip names — whoever arrived first.</summary>
        public BossEncounter Primary => _encounters.Count > 0 ? _encounters[0] : null;

        public float CurrentRewardMultiplier
        {
            get
            {
                float m = 1f;
                foreach (var e in _encounters) m *= e.RewardMultiplier;
                return m;
            }
        }

        /// <summary>
        /// The house gives you a few spins of quiet after an encounter. Without
        /// it, back-to-back sins make being hunted the default state rather
        /// than an event.
        /// </summary>
        public bool InGracePeriod =>
            _ctx.Game.SpinsThisRun - _spinOfLastEncounterEnd <= _ctx.Config.Tuning.summonGraceSpins;

        public bool AtSinCapacity => _encounters.Count >= _ctx.Tables.MaxActiveSins;

        public SinBossSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public void ResetForRun()
        {
            _encounters.Clear();
            _spinOfLastEncounterEnd = int.MinValue / 2;
        }

        public HashSet<string> ActiveSinIds()
        {
            var ids = new HashSet<string>();
            foreach (var e in _encounters) ids.Add(e.Config.id);
            return ids;
        }

        public List<SinBossConfig> UnlockedSins()
        {
            var active = ActiveSinIds();
            return _ctx.Config.Sins.sins
                .Where(s => s.implemented && s.unlockLevel <= _ctx.Xp.Level && !active.Contains(s.id))
                .ToList();
        }

        /// <summary>The wheel landed on the summon wedge. Returns the banner line.</summary>
        public string OnSummonSegmentHit()
        {
            if (AtSinCapacity)
            {
                float dmg = _ctx.Health.ApplyDamage(5f * _ctx.Buffs.DamageMultiplier);
                return $"THE SIN STIRS -{Mathf.RoundToInt(dmg)} HP";
            }

            var candidates = UnlockedSins();
            if (candidates.Count == 0)
                return "SOMETHING WATCHES";
            if (InGracePeriod)
                return "THE HOUSE LOOKS AWAY";

            var t = _ctx.Config.Tuning;
            float chance = Mathf.Min(t.sinSummonChanceMax, t.sinSummonBaseChance + _ctx.Notice.Fill);
            if (_ctx.Rng.NextDouble() < chance)
            {
                var cfg = PickWeighted(candidates);
                StartEncounter(cfg);
                return $"{cfg.displayName.ToUpperInvariant()} AWAKENS";
            }

            return $"SIN CHANCE {Mathf.RoundToInt(chance * 100)}%";
        }

        /// <summary>
        /// A full Notice meter spends itself on the next risk wedge: the sin
        /// arrives, guaranteed, and the eye closes again.
        /// </summary>
        public bool TryForcedSummon(SegmentConfig landed)
        {
            if (AtSinCapacity || !landed.IsRisk || !_ctx.Notice.IsFull || InGracePeriod) return false;

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
            var encounter = new BossEncounter
            {
                Config = cfg,
                Modifier = BossModifierFactory.Create(_ctx, cfg),
                // Mark III: they outstay their welcome.
                SpinsRemaining = cfg.durationSpins + _ctx.Marks.SinDurationBonus,
                SpinsElapsed = 0
            };
            _encounters.Add(encounter);

            int visits = SaveData.IncrementCount(_ctx.Save.Data.sinEncounters, cfg.id);
            _ctx.Narrative.BeginEncounter(cfg.id, visits >= 3);
            _ctx.Ring.Rebuild(); // the sin's splices join the wheel

            _ctx.Analytics.Track("boss_encounter_start",
                "sin", cfg.id, "player_level", _ctx.Xp.Level,
                "table", _ctx.Tables.CurrentTable, "stacked", _encounters.Count);
            _ctx.Hud?.OnBossStarted(encounter);
            Sfx.Boss();
            Haptics.Heavy();
        }

        // --- Hooks called by SpinSystem / GameManager / WheelRingSystem ---

        public List<SegmentConfig> ModifySegments(List<SegmentConfig> ring)
        {
            foreach (var e in _encounters) ring = e.Modifier.ModifySegments(ring);
            return ring;
        }

        public float ModifyCooldown(float cooldown)
        {
            foreach (var e in _encounters) cooldown = e.Modifier.ModifyCooldown(cooldown);
            return Mathf.Max(0.1f, cooldown - _ctx.Run.SlothCooldownBonus - _ctx.Run.TableCooldownBonus);
        }

        public float ModifyRewardMultiplier(float multiplier)
        {
            foreach (var e in _encounters) multiplier = e.Modifier.ModifyRewardMultiplier(multiplier);
            return multiplier;
        }

        public int ModifyCoinGain(int coins)
        {
            foreach (var e in _encounters) coins = e.Modifier.ModifyCoinGain(coins);
            return coins;
        }

        public float ModifyDamage(float damage)
        {
            foreach (var e in _encounters) damage = e.Modifier.ModifyDamage(damage);
            return damage;
        }

        public void OnSpinStarted()
        {
            foreach (var e in _encounters) e.Modifier.OnSpinStarted(e);
        }

        /// <summary>Gluttony's exit: paying mid-encounter buys your way out.</summary>
        public void OnTithe()
        {
            foreach (var e in _encounters.ToList())
            {
                e.Modifier.OnTithe(e);
                if (IsBroken(e)) EndEncounter(e, defeated: true);
            }
        }

        public void OnSpinResolved(SegmentConfig landed)
        {
            foreach (var encounter in _encounters.ToList())
            {
                encounter.SpinsElapsed++;
                encounter.SpinsRemaining--;
                encounter.Modifier.OnSpinResolved(encounter, landed);

                if (IsBroken(encounter))
                {
                    EndEncounter(encounter, defeated: true);
                    continue;
                }
                if (encounter.SpinsRemaining <= 0)
                {
                    EndEncounter(encounter, defeated: false);
                    continue;
                }

                // The encounter has a middle: an occasional taunt, drawn without
                // replacement so nothing repeats within one visit.
                if (encounter == Primary && encounter.SpinsElapsed >= 2
                    && encounter.SpinsElapsed % 3 == 0 && _ctx.Rng.NextDouble() < 0.6)
                {
                    string taunt = _ctx.Narrative.NextTaunt();
                    if (taunt != null) _ctx.Hud?.ShowSpeech(encounter.Config.id, taunt);
                }
            }

            _ctx.Hud?.OnBossUpdated(Primary);
        }

        /// <summary>At his table the Croupier disables breaks: they run their course.</summary>
        private bool IsBroken(BossEncounter encounter)
        {
            if (_ctx.Tables.BreaksDisabled) return false;
            return encounter.Modifier.IsDefeated(encounter);
        }

        /// <summary>Run died or banked out while sins were active — log the drop-off.</summary>
        public void AbandonEncounters(string reason)
        {
            if (_encounters.Count == 0) return;

            if (reason == "banked_out" && Primary != null)
                _ctx.Narrative.SetRunEndQuote(
                    _ctx.Narrative.EncounterEndLine(Primary.Config.id, "player_fled"), 2);

            foreach (var e in _encounters)
                _ctx.Analytics.Track("boss_encounter_end",
                    "sin", e.Config.id, "outcome", reason, "spins", e.SpinsElapsed);

            _encounters.Clear();
            _spinOfLastEncounterEnd = _ctx.Game.SpinsThisRun;
            _ctx.Hud?.OnBossEnded();
            _ctx.Ring.Rebuild();
        }

        private void EndEncounter(BossEncounter encounter, bool defeated)
        {
            var cfg = encounter.Config;
            int spins = encounter.SpinsElapsed;
            _encounters.Remove(encounter);

            if (defeated)
            {
                encounter.Modifier.OnBroken(encounter);
                _ctx.Wallet.AddRunCoins(cfg.defeatCoins);
                _ctx.Tables.RecordCoinsEarned(cfg.defeatCoins);
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
                _ctx.Tables.RecordCoinsEarned(cfg.surviveCoins);
                _ctx.Hud?.Toast($"OUTLASTED {cfg.displayName.ToUpperInvariant()} +{cfg.surviveCoins}C", Palette.Bone);
            }

            _ctx.Notice.OnEncounterEnded();
            _ctx.Hud?.ShowSpeech(cfg.id,
                _ctx.Narrative.EncounterEndLine(cfg.id, defeated ? "defeated" : "expired"));

            _ctx.Analytics.Track("boss_encounter_end",
                "sin", cfg.id, "outcome", defeated ? "defeated" : "survived", "spins", spins);

            if (_encounters.Count == 0)
            {
                _spinOfLastEncounterEnd = _ctx.Game.SpinsThisRun;
                _ctx.Hud?.OnBossEnded();
            }
            else
            {
                // Whoever is left owns the strip now - but they have already
                // announced themselves, so no second plate.
                _ctx.Hud?.OnBossRefreshed(Primary);
            }

            _ctx.Ring.Rebuild(); // the splices leave with it
            Sfx.LevelUp();
        }
    }
}
