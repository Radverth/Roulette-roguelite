using System;
using System.Collections.Generic;
using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// All balance data lives in JSON files under Assets/Resources/Config.
    /// Nothing in here is hardcoded gameplay tuning — designers edit JSON, not C#.
    /// </summary>
    public enum SegmentType
    {
        Coins,
        Xp,
        Gems,
        Buff,
        Humility,
        Damage,
        CoinLoss,
        Debuff,
        SinSummon
    }

    [Serializable]
    public class SegmentConfig
    {
        public string id;
        public string type;
        public string label;
        public float weight;
        public float amount;
        public string colorHex;
        public string icon;        // optional sprite under Art/Icons
        public string wedgeClass;  // "reward" | "risk" — drives disc shading and streaks
        public string rarity;      // "common" | "rare" | "cursed" — Forge offer pool
        public float tierScale = 1.6f;
        public bool draftable = true;

        /// <summary>Temper level 1-3. Runtime only: the ring stores it, not the catalog.</summary>
        [NonSerialized] public int tier = 1;

        public SegmentType ParsedType
        {
            get
            {
                if (Enum.TryParse(type, true, out SegmentType t)) return t;
                Debug.LogError($"[Config] Unknown segment type '{type}' on segment '{id}', defaulting to Coins");
                return SegmentType.Coins;
            }
        }

        public bool IsRisk => wedgeClass == "risk";
        public bool IsReward => !IsRisk;

        /// <summary>Base value scaled by temper tier.</summary>
        public float EffectiveAmount => amount * Mathf.Pow(Mathf.Max(1f, tierScale), Mathf.Max(1, tier) - 1);

        public Color ParsedColor
        {
            get
            {
                if (ColorUtility.TryParseHtmlString(colorHex, out Color c)) return c;
                return Color.magenta;
            }
        }

        public SegmentConfig Clone()
        {
            return new SegmentConfig
            {
                id = id, type = type, label = label,
                weight = weight, amount = amount, colorHex = colorHex, icon = icon,
                wedgeClass = wedgeClass, rarity = rarity, tierScale = tierScale,
                draftable = draftable, tier = tier
            };
        }
    }

    [Serializable]
    public class WheelConfig
    {
        public string wheelId;
        /// <summary>Every wedge the game knows about. The ring is built from these by id.</summary>
        public List<SegmentConfig> catalog = new List<SegmentConfig>();
        /// <summary>Catalog ids making up a fresh player's ring, in wheel order.</summary>
        public List<string> startingRing = new List<string>();
    }

    [Serializable]
    public class SinBossConfig
    {
        public string id;
        public string displayName;
        public string tagline;
        public int unlockLevel;
        public float weight = 1f;
        public bool implemented;
        public int durationSpins = 12;

        // Modifier parameters — each sin uses the subset it needs.
        public float cooldownMultiplier = 1f;   // Sloth
        public int resistThreshold;             // Sloth: consecutive spins to break free
        public float coinTaxPercent;            // Greed
        public int extraDamageSegments;         // Wrath
        public int shufflePeriodSpins;          // Lust
        public float spinCostPercentOfBank;     // Gluttony
        public float rewardShrinkPercent;       // Pride
        public int humilityWedges = 2;          // Pride splices these in to be broken
        public int breakTarget = 3;             // generic "N of something" break threshold
        public float wrathHealthFloorPercent = 25f;

        /// <summary>Shown on the encounter strip: how to break this sin.</summary>
        public string breakHint;

        // Risk/reward scaling while the sin is active.
        public float rewardMultiplierStart = 1f;
        public float rewardMultiplierPerSpin;

        // Payout when the encounter ends.
        public int defeatCoins;
        public int defeatGems;
        public int surviveCoins;

        public string colorHex;
    }

    [Serializable]
    public class SinsConfig
    {
        public List<SinBossConfig> sins = new List<SinBossConfig>();
    }

    [Serializable]
    public class UpgradeConfig
    {
        public string id;
        public string displayName;
        public string description;
        public string category;   // "meta" or "sin_resist"
        public string sinId;      // set for sin_resist upgrades
        public int maxTier = 3;
        public int baseCost = 50;
        public float costGrowth = 2f;
        public float effectPerTier;
    }

    [Serializable]
    public class UpgradesConfig
    {
        public List<UpgradeConfig> upgrades = new List<UpgradeConfig>();
    }

    [Serializable]
    public class GameTuningConfig
    {
        public float baseSpinCooldown = 1.5f;
        public float minSpinCooldown = 0.5f;
        public float spinAnimDuration = 2.2f;
        public int baseMaxHp = 100;

        public float sinSummonBaseChance = 0.15f;
        public float sinSummonChanceIncrement = 0.15f;
        public float sinSummonChanceMax = 0.95f;

        public int xpBase = 20;
        public float xpGrowth = 1.35f;

        public float bankingBonusBase = 1f;

        public float buffRewardMultiplier = 1.25f;
        public int buffDurationSpins = 5;
        public float debuffDamageMultiplier = 1.5f;
        public int debuffDurationSpins = 5;

        // --- Notice: the escalation made visible (8 segments) ---
        public float noticeSegments = 8f;
        public float noticePerSpin = 0.25f;
        public float noticePerTithe = 1f;
        public float noticeHighPurseThreshold = 200f;
        public float noticeHighPurseBonus = 0.25f;
        public float noticeOnSinBroken = -3f;
        public float noticeOnHumility = -1f;
        public float noticeOnEncounterEnd = -4f;
        /// <summary>Spins of quiet the house grants after an encounter ends.</summary>
        public int summonGraceSpins = 5;

        // --- Streak: tension on every spin ---
        public int streakStartsAt = 3;
        public float streakStepMultiplier = 0.25f;
        public float streakMaxMultiplier = 3f;

        // --- Quota, debt and the tithe ---
        public int debtStart = 5000;
        public int quotaBase = 250;
        public float quotaGrowth = 1.15f;
        public float quotaRelief = 0.92f;
        public float tithePercentOfPurse = 50f;
        public int maxPenaltyRiskWedges = 4;

        // --- The Forge ---
        public int forgeMinRingSize = 12;
        public int forgeCursedFromRingSize = 15;
        public int forgeMaxTier = 3;
        public int forgeRerollCost = 50;

        // --- Near-miss: bias the resting angle toward the richest neighbour ---
        public float nearMissChance = 0.6f;
    }

    /// <summary>Aggregated root handed to every system via GameContext.</summary>
    public class GameConfigRoot
    {
        public GameTuningConfig Tuning;
        public WheelConfig Wheel;
        public SinsConfig Sins;
        public UpgradesConfig Upgrades;
    }
}
