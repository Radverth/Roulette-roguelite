using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// Owns sin summoning and the active encounter. Summon chance scales with
    /// every SinSummon segment hit and resets when a boss actually appears.
    /// Bosses unlock by player level, then get picked weighted-random so
    /// returning players face variety instead of a fixed order.
    /// </summary>
    public sealed class SinBossSystem
    {
        private readonly GameContext _ctx;
        private float _summonChance;

        public BossEncounter Encounter { get; private set; }
        public bool EncounterActive => Encounter != null;
        public float SummonChance => _summonChance;

        public float CurrentRewardMultiplier => Encounter?.RewardMultiplier ?? 1f;

        public SinBossSystem(GameContext ctx)
        {
            _ctx = ctx;
        }

        public void ResetForRun()
        {
            _summonChance = _ctx.Config.Tuning.sinSummonBaseChance;
            Encounter = null;
        }

        public List<SinBossConfig> UnlockedSins()
        {
            return _ctx.Config.Sins.sins
                .Where(s => s.implemented && s.unlockLevel <= _ctx.Xp.Level)
                .ToList();
        }

        /// <summary>Called when the wheel lands on a SinSummon segment. Returns banner text.</summary>
        public string OnSummonSegmentHit()
        {
            if (EncounterActive)
            {
                // The wheel already answers to a sin; the omen just stings.
                float dmg = _ctx.Health.ApplyDamage(5f * _ctx.Buffs.DamageMultiplier);
                return $"THE SIN STIRS -{Mathf.RoundToInt(dmg)} HP";
            }

            var candidates = UnlockedSins();
            if (candidates.Count == 0)
            {
                _summonChance = Mathf.Min(_ctx.Config.Tuning.sinSummonChanceMax,
                    _summonChance + _ctx.Config.Tuning.sinSummonChanceIncrement);
                return "SOMETHING WATCHES...";
            }

            if (_ctx.Rng.NextDouble() < _summonChance)
            {
                StartEncounter(PickWeighted(candidates));
                return $"{Encounter.Config.displayName.ToUpperInvariant()} AWAKENS!";
            }

            _summonChance = Mathf.Min(_ctx.Config.Tuning.sinSummonChanceMax,
                _summonChance + _ctx.Config.Tuning.sinSummonChanceIncrement);
            return $"SIN CHANCE {Mathf.RoundToInt(_summonChance * 100)}%";
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
                SpinsElapsed = 0,
                Resist = 0
            };
            _summonChance = _ctx.Config.Tuning.sinSummonBaseChance;

            _ctx.Analytics.Track("boss_encounter_start", "sin", cfg.id, "player_level", _ctx.Xp.Level);
            _ctx.Hud?.OnBossStarted(Encounter);
            Sfx.Boss();
            Haptics.Heavy();
        }

        // --- Hooks called by SpinSystem ---

        public List<SegmentConfig> GetEffectiveSegments(List<SegmentConfig> baseSegments)
        {
            if (!EncounterActive) return baseSegments;
            return Encounter.Modifier.ModifySegments(baseSegments);
        }

        public float ModifyCooldown(float cooldown)
        {
            return EncounterActive ? Encounter.Modifier.ModifyCooldown(cooldown) : cooldown;
        }

        public int ModifyCoinGain(int coins)
        {
            return EncounterActive ? Encounter.Modifier.ModifyCoinGain(coins) : coins;
        }

        public void OnSpinStarted()
        {
            if (EncounterActive) Encounter.Modifier.OnSpinStarted(Encounter);
        }

        public void OnSpinResolved()
        {
            if (!EncounterActive) return;

            Encounter.SpinsElapsed++;
            Encounter.SpinsRemaining--;
            Encounter.Modifier.OnSpinResolved(Encounter);

            if (Encounter.Modifier.IsDefeated(Encounter))
                EndEncounter(defeated: true);
            else if (Encounter.SpinsRemaining <= 0)
                EndEncounter(defeated: false);
            else
                _ctx.Hud?.OnBossUpdated(Encounter);
        }

        /// <summary>Run died or banked out while a sin was active — log the drop-off.</summary>
        public void AbandonEncounter(string reason)
        {
            if (!EncounterActive) return;
            _ctx.Analytics.Track("boss_encounter_end",
                "sin", Encounter.Config.id, "outcome", reason, "spins", Encounter.SpinsElapsed);
            Encounter = null;
            _ctx.Hud?.OnBossEnded();
        }

        private void EndEncounter(bool defeated)
        {
            var cfg = Encounter.Config;
            int spins = Encounter.SpinsElapsed;

            if (defeated)
            {
                _ctx.Wallet.AddRunCoins(cfg.defeatCoins);
                _ctx.Wallet.AddGems(cfg.defeatGems);
                _ctx.Hud?.Toast($"{cfg.displayName.ToUpperInvariant()} REPENTS +{cfg.defeatCoins}C +{cfg.defeatGems}G", Palette.Gold);
            }
            else
            {
                _ctx.Wallet.AddRunCoins(cfg.surviveCoins);
                _ctx.Hud?.Toast($"OUTLASTED {cfg.displayName.ToUpperInvariant()} +{cfg.surviveCoins}C", Palette.Bone);
            }

            _ctx.Analytics.Track("boss_encounter_end",
                "sin", cfg.id, "outcome", defeated ? "defeated" : "survived", "spins", spins);

            Encounter = null;
            _ctx.Hud?.OnBossEnded();
            Sfx.LevelUp();
        }
    }
}
