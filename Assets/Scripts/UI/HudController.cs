using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SinWheel
{
    /// <summary>
    /// Builds and drives the entire HUD from code. Poll-based refresh in Tick()
    /// keeps event wiring minimal for the slice; individual moments (outcomes,
    /// bosses, run end) come in through explicit calls.
    /// </summary>
    public sealed class HudController
    {
        private const float RefWidth = 1080f;
        private const float RefHeight = 1920f;

        private readonly GameContext _ctx;

        public WheelController Wheel { get; private set; }

        private RectTransform _root;       // canvas root
        private RectTransform _shakeRoot;  // everything that trembles on big hits

        private Text _hpText;
        private RectTransform _hpFill;
        private Text _runCoinsText;
        private Text _metaCoinsText;
        private Text _gemsText;
        private Text _levelText;
        private RectTransform _xpFill;
        private Text _buffText;

        private Button _spinButton;
        private Text _spinLabel;
        private Button _bankButton;

        private GameObject _bossPanel;
        private Text _bossNameText;
        private Text _bossTaglineText;
        private Text _bossStatusText;
        private RectTransform _resistFill;
        private GameObject _resistRow;

        private GameObject _runEndPanel;
        private Text _runEndTitle;
        private Text _runEndStats;

        private GameObject _upgradesPanel;
        private RectTransform _upgradesContent;

        private Vector2 _wheelCenter;
        private Coroutine _shakeRoutine;

        public HudController(GameContext ctx)
        {
            _ctx = ctx;
        }

        public void Build()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _root = (RectTransform)canvasGo.transform;

            var background = UiFactory.CreatePanel(_root, "Background", Palette.Night);
            UiFactory.Stretch(background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _shakeRoot = UiFactory.CreateRect(_root, "ShakeRoot");
            UiFactory.Stretch(_shakeRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            BuildTopBar();
            BuildWheelArea();
            BuildBossBanner();
            BuildBottomBar();
            BuildRunEndPanel();
            BuildUpgradesPanel();
        }

        // --- Construction ---

        private void BuildTopBar()
        {
            var bar = UiFactory.CreatePanel(_shakeRoot, "TopBar", Palette.Panel);
            UiFactory.Stretch(bar, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -290f), Vector2.zero);

            var title = UiFactory.CreateText(bar, "Title", "S I N   W H E E L", 40, Palette.Gold);
            UiFactory.Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(600f, 60f));

            _hpText = UiFactory.CreateText(bar, "HpText", "", 30, Palette.Bone, TextAnchor.MiddleLeft);
            UiFactory.Place((RectTransform)_hpText.transform, new Vector2(0f, 1f), new Vector2(220f, -110f), new Vector2(400f, 40f));

            var hpBar = UiFactory.CreateRect(bar, "HpBar");
            UiFactory.Place(hpBar, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(980f, 30f));
            _hpFill = UiFactory.CreateBar(hpBar, "HpMeter", Palette.BloodDark, Palette.Blood);
            UiFactory.Stretch((RectTransform)_hpFill.parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _runCoinsText = UiFactory.CreateText(bar, "RunCoins", "", 32, Palette.Gold, TextAnchor.MiddleLeft);
            UiFactory.Place((RectTransform)_runCoinsText.transform, new Vector2(0f, 1f), new Vector2(190f, -205f), new Vector2(340f, 40f));

            _metaCoinsText = UiFactory.CreateText(bar, "MetaCoins", "", 32, Palette.Bone, TextAnchor.MiddleCenter);
            UiFactory.Place((RectTransform)_metaCoinsText.transform, new Vector2(0.5f, 1f), new Vector2(0f, -205f), new Vector2(340f, 40f));

            _gemsText = UiFactory.CreateText(bar, "Gems", "", 32, Palette.Purple, TextAnchor.MiddleRight);
            UiFactory.Place((RectTransform)_gemsText.transform, new Vector2(1f, 1f), new Vector2(-190f, -205f), new Vector2(300f, 40f));

            _levelText = UiFactory.CreateText(bar, "Level", "", 28, Palette.Teal, TextAnchor.MiddleLeft);
            UiFactory.Place((RectTransform)_levelText.transform, new Vector2(0f, 1f), new Vector2(190f, -255f), new Vector2(340f, 36f));

            var xpBar = UiFactory.CreateRect(bar, "XpBar");
            UiFactory.Place(xpBar, new Vector2(0.5f, 1f), new Vector2(80f, -255f), new Vector2(640f, 14f));
            _xpFill = UiFactory.CreateBar(xpBar, "XpMeter", Palette.PanelLight, Palette.Teal);
            UiFactory.Stretch((RectTransform)_xpFill.parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private void BuildWheelArea()
        {
            var container = UiFactory.CreateRect(_shakeRoot, "WheelArea");
            UiFactory.Place(container, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(960f, 960f));
            _wheelCenter = new Vector2(0f, 40f);

            Wheel = new WheelController(_ctx, container, 880f);

            _buffText = UiFactory.CreateText(_shakeRoot, "BuffText", "", 26, Palette.Bone);
            UiFactory.Place((RectTransform)_buffText.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -500f), new Vector2(900f, 36f));
        }

        private void BuildBossBanner()
        {
            var panel = UiFactory.CreatePanel(_shakeRoot, "BossBanner", Palette.BloodDark);
            UiFactory.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0f, 570f), new Vector2(1000f, 170f));
            _bossPanel = panel.gameObject;

            _bossNameText = UiFactory.CreateText(panel, "Name", "", 44, Palette.Gold);
            UiFactory.Place((RectTransform)_bossNameText.transform, new Vector2(0.5f, 1f), new Vector2(0f, -35f), new Vector2(900f, 50f));

            _bossTaglineText = UiFactory.CreateText(panel, "Tagline", "", 24, Palette.Bone, TextAnchor.MiddleCenter, FontStyle.Italic);
            UiFactory.Place((RectTransform)_bossTaglineText.transform, new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(950f, 34f));

            _bossStatusText = UiFactory.CreateText(panel, "Status", "", 26, Palette.Bone);
            UiFactory.Place((RectTransform)_bossStatusText.transform, new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(950f, 34f));

            var resistRow = UiFactory.CreateRect(panel, "ResistRow");
            UiFactory.Place(resistRow, new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(900f, 16f));
            _resistFill = UiFactory.CreateBar(resistRow, "ResistMeter", Palette.Panel, Palette.Teal);
            UiFactory.Stretch((RectTransform)_resistFill.parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _resistRow = resistRow.gameObject;

            _bossPanel.SetActive(false);
        }

        private void BuildBottomBar()
        {
            _spinButton = UiFactory.CreateButton(_shakeRoot, "SpinButton", "SPIN", Palette.Blood, Palette.Bone, 52,
                () => _ctx.Spin.RequestSpin());
            UiFactory.Place((RectTransform)_spinButton.transform, new Vector2(0.5f, 0f), new Vector2(0f, 330f), new Vector2(560f, 150f));
            _spinLabel = _spinButton.GetComponentInChildren<Text>();

            _bankButton = UiFactory.CreateButton(_shakeRoot, "BankButton", "BANK & END RUN", Palette.PanelLight, Palette.Gold, 34,
                () => _ctx.Game.BankAndEndRun());
            UiFactory.Place((RectTransform)_bankButton.transform, new Vector2(0.5f, 0f), new Vector2(-240f, 160f), new Vector2(480f, 110f));

            var upgradesButton = UiFactory.CreateButton(_shakeRoot, "UpgradesButton", "UPGRADES", Palette.PanelLight, Palette.Teal, 34,
                ToggleUpgradesPanel);
            UiFactory.Place((RectTransform)upgradesButton.transform, new Vector2(0.5f, 0f), new Vector2(240f, 160f), new Vector2(480f, 110f));
        }

        private void BuildRunEndPanel()
        {
            var overlay = UiFactory.CreatePanel(_root, "RunEndPanel", new Color(0.05f, 0.03f, 0.08f, 0.95f));
            UiFactory.Stretch(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _runEndPanel = overlay.gameObject;

            _runEndTitle = UiFactory.CreateText(overlay, "Title", "", 60, Palette.Gold);
            UiFactory.Place((RectTransform)_runEndTitle.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 320f), new Vector2(1000f, 80f));

            _runEndStats = UiFactory.CreateText(overlay, "Stats", "", 34, Palette.Bone);
            UiFactory.Place((RectTransform)_runEndStats.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(1000f, 400f));

            var newRun = UiFactory.CreateButton(overlay, "NewRun", "SPIN AGAIN", Palette.Blood, Palette.Bone, 44, () =>
            {
                _runEndPanel.SetActive(false);
                _ctx.Game.StartRun();
            });
            UiFactory.Place((RectTransform)newRun.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -260f), new Vector2(560f, 140f));

            _runEndPanel.SetActive(false);
        }

        private void BuildUpgradesPanel()
        {
            var overlay = UiFactory.CreatePanel(_root, "UpgradesPanel", new Color(0.05f, 0.03f, 0.08f, 0.97f));
            UiFactory.Stretch(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _upgradesPanel = overlay.gameObject;

            var title = UiFactory.CreateText(overlay, "Title", "PERMANENT UPGRADES", 44, Palette.Gold);
            UiFactory.Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(1000f, 60f));

            _upgradesContent = UiFactory.CreateRect(overlay, "Content");
            UiFactory.Stretch(_upgradesContent, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(40f, 200f), new Vector2(-40f, -160f));

            var close = UiFactory.CreateButton(overlay, "Close", "CLOSE", Palette.PanelLight, Palette.Bone, 36,
                () => _upgradesPanel.SetActive(false));
            UiFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(400f, 110f));

            _upgradesPanel.SetActive(false);
        }

        private void ToggleUpgradesPanel()
        {
            bool show = !_upgradesPanel.activeSelf;
            if (show) RefreshUpgradeRows();
            _upgradesPanel.SetActive(show);
        }

        private void RefreshUpgradeRows()
        {
            for (int i = _upgradesContent.childCount - 1; i >= 0; i--)
                Object.Destroy(_upgradesContent.GetChild(i).gameObject);

            float y = -10f;
            foreach (var cfg in _ctx.Config.Upgrades.upgrades)
            {
                // Slice scope: meta tree + Sloth's resistance tree.
                if (cfg.category == "sin_resist" && cfg.sinId != "sloth") continue;

                var row = UiFactory.CreatePanel(_upgradesContent, $"Row_{cfg.id}", Palette.Panel);
                UiFactory.Stretch(row, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, y - 170f), new Vector2(0f, y));

                int tier = _ctx.Upgrades.GetTier(cfg.id);

                var name = UiFactory.CreateText(row, "Name", $"{cfg.displayName}  [{tier}/{cfg.maxTier}]", 34, Palette.Bone, TextAnchor.MiddleLeft);
                UiFactory.Place((RectTransform)name.transform, new Vector2(0f, 1f), new Vector2(330f, -45f), new Vector2(620f, 44f));

                var desc = UiFactory.CreateText(row, "Desc", cfg.description, 24, Palette.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                UiFactory.Place((RectTransform)desc.transform, new Vector2(0f, 1f), new Vector2(330f, -105f), new Vector2(620f, 70f));

                bool maxed = tier >= cfg.maxTier;
                string buyLabel = maxed ? "MAXED" : $"BUY  {_ctx.Upgrades.GetCost(cfg, tier)}";
                var buy = UiFactory.CreateButton(row, "Buy", buyLabel, maxed ? Palette.PanelLight : Palette.Blood, Palette.Gold, 28, () =>
                {
                    if (_ctx.Upgrades.TryPurchase(cfg.id))
                    {
                        Sfx.Reward();
                        RefreshUpgradeRows();
                    }
                    else
                    {
                        Sfx.Damage();
                    }
                });
                UiFactory.Place((RectTransform)buy.transform, new Vector2(1f, 0.5f), new Vector2(-170f, 0f), new Vector2(280f, 100f));
                buy.interactable = !maxed;

                y -= 185f;
            }
        }

        // --- Per-frame refresh ---

        public void Tick()
        {
            var hp = _ctx.Health;
            _hpText.text = $"RESILIENCE  {Mathf.CeilToInt(hp.CurrentHp)}/{hp.MaxHp}";
            UiFactory.SetBarFill(_hpFill, hp.MaxHp > 0 ? hp.CurrentHp / hp.MaxHp : 0f);

            _runCoinsText.text = $"RUN  {_ctx.Wallet.RunCoins}";
            _metaCoinsText.text = $"BANK  {_ctx.Wallet.MetaCoins}";
            _gemsText.text = $"GEMS  {_ctx.Wallet.Gems}";
            _levelText.text = $"LV {_ctx.Xp.Level}";
            UiFactory.SetBarFill(_xpFill, (float)_ctx.Xp.Xp / Mathf.Max(1, _ctx.Xp.XpToNextLevel()));

            RefreshBuffLine();
            RefreshSpinButton();

            _bankButton.interactable = _ctx.Game.RunActive
                && _ctx.Spin.State != SpinState.Spinning
                && _ctx.Wallet.RunCoins > 0;
        }

        private void RefreshBuffLine()
        {
            int buffs = 0, debuffs = 0;
            foreach (var e in _ctx.Buffs.Effects)
            {
                if (e.IsDebuff) debuffs++;
                else buffs++;
            }

            if (buffs == 0 && debuffs == 0)
            {
                _buffText.text = _ctx.Bosses.EncounterActive
                    ? $"SIN REWARD x{_ctx.Bosses.CurrentRewardMultiplier:0.0}"
                    : "";
                return;
            }

            string line = "";
            if (buffs > 0) line += $"BLESSINGS x{buffs}  ";
            if (debuffs > 0) line += $"HEXES x{debuffs}  ";
            if (_ctx.Bosses.EncounterActive) line += $"SIN REWARD x{_ctx.Bosses.CurrentRewardMultiplier:0.0}";
            _buffText.text = line.TrimEnd();
        }

        private void RefreshSpinButton()
        {
            switch (_ctx.Spin.State)
            {
                case SpinState.Spinning:
                case SpinState.Resolving:
                    _spinLabel.text = "· · ·";
                    _spinButton.interactable = false;
                    break;
                case SpinState.Cooldown:
                    _spinLabel.text = $"{_ctx.Spin.CooldownRemaining:0.0}s";
                    _spinButton.interactable = false;
                    break;
                default:
                    _spinLabel.text = _ctx.Game.RunActive ? "SPIN" : "—";
                    _spinButton.interactable = _ctx.Game.RunActive;
                    break;
            }
        }

        // --- Moments ---

        public void ShowOutcome(OutcomeResult result)
        {
            SpawnFloatingText(result.Text, result.Color, 44);

            switch (result.Type)
            {
                case SegmentType.Coins:
                case SegmentType.Gems:
                case SegmentType.Xp:
                    Sfx.Reward();
                    break;
                case SegmentType.Damage:
                case SegmentType.CoinLoss:
                case SegmentType.Debuff:
                    Sfx.Damage();
                    break;
            }

            if (result.BigHit)
            {
                Haptics.Heavy();
                Shake();
            }
        }

        public void Toast(string message, Color color)
        {
            SpawnFloatingText(message, color, 38, new Vector2(0f, 620f), 1.8f);
        }

        public void OnRunStarted()
        {
            _runEndPanel.SetActive(false);
            _bossPanel.SetActive(false);
        }

        public void OnBossStarted(BossEncounter encounter)
        {
            _bossPanel.SetActive(true);
            _bossNameText.text = encounter.Config.displayName.ToUpperInvariant();
            _bossTaglineText.text = encounter.Config.tagline;
            _resistRow.SetActive(encounter.Config.resistThreshold > 0);
            OnBossUpdated(encounter);
            Shake();
        }

        public void OnBossUpdated(BossEncounter encounter)
        {
            _bossStatusText.text = encounter.Modifier.StatusText(encounter);
            if (encounter.Config.resistThreshold > 0)
                UiFactory.SetBarFill(_resistFill, (float)encounter.Resist / encounter.Config.resistThreshold);
        }

        public void OnBossEnded()
        {
            _bossPanel.SetActive(false);
        }

        public void ShowRunEnd(bool banked, int bankedAmount, int spins)
        {
            _runEndPanel.SetActive(true);
            _runEndTitle.text = banked ? "WINNINGS BANKED" : "THE WHEEL CLAIMS YOU";
            _runEndStats.text =
                (banked ? $"+{bankedAmount} coins banked\n" : "Unbanked coins forfeited\n") +
                $"Spins this run: {spins}\n" +
                $"Bank total: {_ctx.Wallet.MetaCoins}\n" +
                $"Level {_ctx.Xp.Level}";
        }

        // --- Juice ---

        private void SpawnFloatingText(string message, Color color, int fontSize)
        {
            SpawnFloatingText(message, color, fontSize, _wheelCenter, 1.2f);
        }

        private void SpawnFloatingText(string message, Color color, int fontSize, Vector2 position, float lifetime)
        {
            var text = UiFactory.CreateText(_root, "Floating", message, fontSize, color);
            UiFactory.Place((RectTransform)text.transform, new Vector2(0.5f, 0.5f), position, new Vector2(1000f, 70f));
            _ctx.CoroutineHost.StartCoroutine(FloatAndFade(text, position, lifetime));
        }

        private IEnumerator FloatAndFade(Text text, Vector2 start, float lifetime)
        {
            var rt = (RectTransform)text.transform;
            Color baseColor = text.color;
            float elapsed = 0f;

            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / lifetime);
                rt.anchoredPosition = start + new Vector2(0f, 180f * t);
                text.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - t * t);
                yield return null;
            }

            Object.Destroy(text.gameObject);
        }

        private void Shake()
        {
            if (_shakeRoutine != null)
                _ctx.CoroutineHost.StopCoroutine(_shakeRoutine);
            _shakeRoutine = _ctx.CoroutineHost.StartCoroutine(ShakeRoutine());
        }

        private IEnumerator ShakeRoutine()
        {
            const float duration = 0.3f;
            const float magnitude = 14f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float damper = 1f - elapsed / duration;
                _shakeRoot.anchoredPosition = new Vector2(
                    (Random.value - 0.5f) * 2f * magnitude * damper,
                    (Random.value - 0.5f) * 2f * magnitude * damper);
                yield return null;
            }

            _shakeRoot.anchoredPosition = Vector2.zero;
            _shakeRoutine = null;
        }
    }
}
