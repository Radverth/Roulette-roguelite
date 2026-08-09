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
        public string icon; // optional sprite under Art/Icons; falls back to type default

        public SegmentType ParsedType
        {
            get
            {
                if (Enum.TryParse(type, true, out SegmentType t)) return t;
                Debug.LogError($"[Config] Unknown segment type '{type}' on segment '{id}', defaulting to Coins");
                return SegmentType.Coins;
            }
        }

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
                weight = weight, amount = amount, colorHex = colorHex, icon = icon
            };
        }
    }

    [Serializable]
    public class WheelConfig
    {
        public string wheelId;
        public List<SegmentConfig> segments = new List<SegmentConfig>();
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

        public float sinSummonBaseChance = 0.25f;
        public float sinSummonChanceIncrement = 0.15f;
        public float sinSummonChanceMax = 0.95f;

        public int xpBase = 20;
        public float xpGrowth = 1.35f;

        public float bankingBonusBase = 1f;

        public float buffRewardMultiplier = 1.25f;
        public int buffDurationSpins = 5;
        public float debuffDamageMultiplier = 1.5f;
        public int debuffDurationSpins = 5;

        public int wheelTextureSize = 640;
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
