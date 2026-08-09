using UnityEngine;

namespace SinWheel
{
    /// <summary>
    /// Composition root wiring. Built once by GameBootstrap and handed to every
    /// system, so there are no scattered singletons and everything is testable
    /// with a fake context.
    /// </summary>
    public sealed class GameContext
    {
        public GameConfigRoot Config;
        public System.Random Rng = new System.Random();
        public MonoBehaviour CoroutineHost;

        public SaveSystem Save;
        public AnalyticsSystem Analytics;
        public UpgradeSystem Upgrades;
        public NarrativeSystem Narrative;

        public HealthSystem Health;
        public CurrencySystem Wallet;
        public XpSystem Xp;
        public BuffSystem Buffs;

        public SinBossSystem Bosses;
        public SpinSystem Spin;
        public GameManager Game;

        public HudController Hud;
    }
}
