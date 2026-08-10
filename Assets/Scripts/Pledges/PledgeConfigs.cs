using System;
using System.Collections.Generic;

namespace SinWheel
{
    /// <summary>
    /// A thing put up against the debt. The behaviour is keyed by id in
    /// PledgeSystem — a Pledge rewrites a rule, and rules do not serialise —
    /// but every number it turns on lives here.
    /// </summary>
    [Serializable]
    public class PledgeConfig
    {
        public string id;
        public string name;
        public string rarity;      // common | uncommon | rare | cursed
        public float value;        // the pledge's tunable figure
        public string description;

        public bool IsCursed => rarity == "cursed";

        public string CardSprite => "Pledges/pledge_" + id;
        public string EmblemSprite => "Pledges/emblem_" + id;
    }

    [Serializable]
    public class PledgesConfig
    {
        public int slots = 5;
        public float offerRate = 0.33f;
        public float sellRefundPercent = 50f;
        public bool lostOnMark = true;
        public List<PledgeConfig> pledges = new List<PledgeConfig>();
    }
}
