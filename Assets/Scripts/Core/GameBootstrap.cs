using UnityEngine;
using UnityEngine.EventSystems;

namespace SinWheel
{
    /// <summary>
    /// Single scene entry point. The Main scene contains only this component;
    /// everything else — camera, UI, systems — is composed here so the whole
    /// game stays reviewable as code.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private GameContext _ctx;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            CreateCamera();
            CreateEventSystem();
            Sfx.Init();

            _ctx = new GameContext { CoroutineHost = this };
            _ctx.Config = ConfigLoader.LoadAll();
            _ctx.Analytics = new AnalyticsSystem(new DebugLogAnalyticsSink());
            _ctx.Save = new SaveSystem(new NullCloudSaveProvider());
            _ctx.Save.Load();
            Music.Init(_ctx.Save.Data.musicVolume);

            _ctx.Narrative = new NarrativeSystem(_ctx);
            _ctx.Upgrades = new UpgradeSystem(_ctx);
            _ctx.Health = new HealthSystem();
            _ctx.Wallet = new CurrencySystem(_ctx);
            _ctx.Xp = new XpSystem(_ctx);
            _ctx.Buffs = new BuffSystem();

            _ctx.Run = new RunState();
            _ctx.Ring = new WheelRingSystem(_ctx);
            _ctx.Notice = new NoticeSystem(_ctx);
            _ctx.Streak = new StreakSystem(_ctx);
            _ctx.Debt = new DebtSystem(_ctx);
            _ctx.Forge = new ForgeSystem(_ctx);

            _ctx.Bosses = new SinBossSystem(_ctx);
            _ctx.Spin = new SpinSystem(_ctx);
            _ctx.Game = new GameManager(_ctx);

            _ctx.Debt.EnsureSeeded();
            _ctx.Ring.Rebuild();

            _ctx.Hud = new HudController(_ctx);
            _ctx.Hud.Build();

            _ctx.Analytics.TrackSessionStart();
            _ctx.Hud.ShowMainMenu();
        }

        private void Update()
        {
            _ctx.Spin.Tick(Time.deltaTime);
            _ctx.Hud.Tick();
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            _ctx.Save.Persist();
            _ctx.Analytics.TrackSessionEnd();
        }

        private void OnApplicationQuit()
        {
            _ctx.Save.Persist();
            _ctx.Analytics.TrackSessionEnd();
        }

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Night;
            go.AddComponent<AudioListener>();
        }

        private static void CreateEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(go);
        }
    }
}
