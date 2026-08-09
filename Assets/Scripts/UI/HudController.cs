using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SinWheel
{
    /// <summary>
    /// Pixel-art HUD. Designed on a 180x320 virtual-pixel grid; the canvas
    /// scales that grid by an integer factor (x6 on a 1080x1920 phone) so the
    /// art never leaves the pixel grid. All widgets come from the authored
    /// sprite set (Resources/Art) and text uses the 5x7 bitmap font.
    /// </summary>
    public sealed class HudController
    {
        private const int VirtualW = 180;
        private const int VirtualH = 320;

        private readonly GameContext _ctx;

        public WheelController Wheel { get; private set; }

        private RectTransform _root;   // canvas
        private RectTransform _frame;  // centered 180x320 design frame
        private RectTransform _shakeRoot;

        private PixelText _hpText;
        private Image _hpFill;
        private PixelText _runCoinsText;
        private PixelText _metaCoinsText;
        private PixelText _gemsText;
        private PixelText _levelText;
        private Image _xpFill;
        private PixelText _statusLine;

        private Button _spinButton;
        private PixelText _spinLabel;
        private Button _bankButton;

        private GameObject _bossStrip;
        private Image _bossSigil;
        private PixelText _bossName;
        private PixelText _bossStatus;
        private Image _resistFill;
        private GameObject _resistRow;
        private GameObject _bossCardOverlay;
        private Image _bossCardImage;
        private Coroutine _bossCardRoutine;

        private GameObject _runEndPanel;
        private Image _runEndBanner;
        private PixelText _runEndStats;

        private GameObject _upgradesPanel;
        private RectTransform _upgradesContent;

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
            canvas.pixelPerfect = true;

            // Integer scaling keeps the pixel grid intact: x6 on 1080x1920,
            // x8 on 1440p, x1 fallback in tiny editor windows.
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.referencePixelsPerUnit = 16f; // match sprite PPU: 1 unit = 1 art px
            scaler.scaleFactor = Mathf.Max(1, Mathf.FloorToInt(
                Mathf.Min(Screen.width / (float)VirtualW, Screen.height / (float)VirtualH)));

            _root = (RectTransform)canvasGo.transform;

            var background = UiFactory.CreatePanel(_root, "Background", Palette.Night);
            UiFactory.Stretch(background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _frame = UiFactory.CreateRect(_root, "Frame");
            UiFactory.Place(_frame, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(VirtualW, VirtualH));

            _shakeRoot = UiFactory.CreateRect(_frame, "ShakeRoot");
            UiFactory.Stretch(_shakeRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            BuildTopBar();
            BuildBossStrip();
            BuildWheelArea();
            BuildBottomBar();
            BuildBossCardOverlay();
            BuildRunEndPanel();
            BuildUpgradesPanel();
        }

        // --- Construction (positions in virtual px, anchored to frame top-center) ---

        private Vector2 Top => new Vector2(0.5f, 1f);

        private void BuildTopBar()
        {
            var title = PixelText.Create(_shakeRoot, "Title", "SIN WHEEL", Palette.Gold);
            UiFactory.Place((RectTransform)title.transform, Top, new Vector2(0f, -9f), Vector2.zero);

            var hpIcon = UiFactory.CreateSpriteImage(_shakeRoot, "HpIcon", "Icons/ui_hp", new Vector2(16f, 16f));
            UiFactory.Place((RectTransform)hpIcon.transform, Top, new Vector2(-76f, -25f), new Vector2(16f, 16f));

            _hpFill = UiFactory.CreatePixelBar(_shakeRoot, "HpBar", "UI/bar_fill_hp", Top, new Vector2(-30f, -25f));

            _hpText = PixelText.Create(_shakeRoot, "HpText", "", Palette.Bone, 1, PxAlign.Left);
            UiFactory.Place((RectTransform)_hpText.transform, Top, new Vector2(8f, -25f), Vector2.zero);

            var coinIcon = UiFactory.CreateSpriteImage(_shakeRoot, "CoinIcon", "Icons/seg_coin", new Vector2(16f, 16f));
            UiFactory.Place((RectTransform)coinIcon.transform, Top, new Vector2(-76f, -42f), new Vector2(16f, 16f));
            _runCoinsText = PixelText.Create(_shakeRoot, "RunCoins", "0", Palette.Gold, 1, PxAlign.Left);
            UiFactory.Place((RectTransform)_runCoinsText.transform, Top, new Vector2(-66f, -42f), Vector2.zero);

            var relicIcon = UiFactory.CreateSpriteImage(_shakeRoot, "RelicIcon", "Icons/ui_relic", new Vector2(16f, 16f));
            UiFactory.Place((RectTransform)relicIcon.transform, Top, new Vector2(-20f, -42f), new Vector2(16f, 16f));
            _metaCoinsText = PixelText.Create(_shakeRoot, "MetaCoins", "0", Palette.Bone, 1, PxAlign.Left);
            UiFactory.Place((RectTransform)_metaCoinsText.transform, Top, new Vector2(-10f, -42f), Vector2.zero);

            var shardIcon = UiFactory.CreateSpriteImage(_shakeRoot, "ShardIcon", "Icons/seg_shard", new Vector2(16f, 16f));
            UiFactory.Place((RectTransform)shardIcon.transform, Top, new Vector2(40f, -42f), new Vector2(16f, 16f));
            _gemsText = PixelText.Create(_shakeRoot, "Gems", "0", Palette.Purple, 1, PxAlign.Left);
            UiFactory.Place((RectTransform)_gemsText.transform, Top, new Vector2(50f, -42f), Vector2.zero);

            _levelText = PixelText.Create(_shakeRoot, "Level", "LV 1", Palette.Teal, 1, PxAlign.Left);
            UiFactory.Place((RectTransform)_levelText.transform, Top, new Vector2(-84f, -57f), Vector2.zero);

            _xpFill = UiFactory.CreatePixelBar(_shakeRoot, "XpBar", "UI/bar_fill_xp", Top, new Vector2(4f, -57f));
        }

        private void BuildBossStrip()
        {
            var strip = UiFactory.CreateNineSlice(_shakeRoot, "BossStrip", "UI/panel_danger");
            UiFactory.Place((RectTransform)strip.transform, Top, new Vector2(0f, -85f), new Vector2(172f, 38f));
            _bossStrip = strip.gameObject;

            _bossSigil = UiFactory.CreateSpriteImage(strip.transform, "Sigil", null, new Vector2(32f, 32f));
            UiFactory.Place((RectTransform)_bossSigil.transform, new Vector2(0f, 0.5f), new Vector2(21f, 0f), new Vector2(32f, 32f));

            _bossName = PixelText.Create(strip.transform, "Name", "", Palette.Gold, 1, PxAlign.Left);
            UiFactory.Place((RectTransform)_bossName.transform, new Vector2(0f, 0.5f), new Vector2(42f, 10f), Vector2.zero);

            _bossStatus = PixelText.Create(strip.transform, "Status", "", Palette.Bone, 1, PxAlign.Left);
            UiFactory.Place((RectTransform)_bossStatus.transform, new Vector2(0f, 0.5f), new Vector2(42f, 0f), Vector2.zero);

            var resistRow = UiFactory.CreateRect(strip.transform, "ResistRow");
            UiFactory.Place(resistRow, new Vector2(0f, 0.5f), new Vector2(74f, -11f), new Vector2(64f, 8f));
            _resistFill = UiFactory.CreatePixelBar(resistRow, "Resist", "UI/bar_fill_resist",
                new Vector2(0.5f, 0.5f), Vector2.zero);
            _resistRow = resistRow.gameObject;

            _bossStrip.SetActive(false);
        }

        private void BuildWheelArea()
        {
            var container = UiFactory.CreateRect(_shakeRoot, "WheelArea");
            UiFactory.Place(container, Top, new Vector2(0f, -177f), new Vector2(160f, 160f));
            Wheel = new WheelController(_ctx, container);

            _statusLine = PixelText.Create(_shakeRoot, "StatusLine", "", Palette.Bone);
            UiFactory.Place((RectTransform)_statusLine.transform, Top, new Vector2(0f, -253f), Vector2.zero);
        }

        private void BuildBottomBar()
        {
            _spinButton = UiFactory.CreatePixelButton(_shakeRoot, "SpinButton", "SPIN", true, 2,
                () => _ctx.Spin.RequestSpin(), out _spinLabel);
            UiFactory.Place((RectTransform)_spinButton.transform, Top, new Vector2(0f, -278f), new Vector2(96f, 32f));

            _bankButton = UiFactory.CreatePixelButton(_shakeRoot, "BankButton", "BANK", false, 1,
                () => _ctx.Game.BankAndEndRun(), out _);
            UiFactory.Place((RectTransform)_bankButton.transform, Top, new Vector2(-45f, -306f), new Vector2(48f, 16f));

            var upgrades = UiFactory.CreatePixelButton(_shakeRoot, "UpgradesButton", "UPGRADE", false, 1,
                ToggleUpgradesPanel, out _);
            UiFactory.Place((RectTransform)upgrades.transform, Top, new Vector2(45f, -306f), new Vector2(48f, 16f));
        }

        private void BuildBossCardOverlay()
        {
            var dim = UiFactory.CreatePanel(_root, "BossCardOverlay", new Color(0.02f, 0.01f, 0.04f, 0.88f));
            UiFactory.Stretch(dim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _bossCardOverlay = dim.gameObject;

            var dimButton = dim.gameObject.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;
            dimButton.onClick.AddListener(HideBossCard);

            _bossCardImage = UiFactory.CreateSpriteImage(dim, "Card", null, new Vector2(96f, 144f));
            UiFactory.Place((RectTransform)_bossCardImage.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96f, 144f));

            var hint = PixelText.Create(dim, "Hint", "TAP TO FACE IT", Palette.Dim);
            UiFactory.Place((RectTransform)hint.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -86f), Vector2.zero);

            _bossCardOverlay.SetActive(false);
        }

        private void BuildRunEndPanel()
        {
            var overlay = UiFactory.CreatePanel(_root, "RunEndPanel", new Color(0.02f, 0.01f, 0.04f, 0.92f));
            UiFactory.Stretch(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _runEndPanel = overlay.gameObject;

            _runEndBanner = UiFactory.CreateSpriteImage(overlay, "Banner", null, new Vector2(160f, 48f));
            UiFactory.Place((RectTransform)_runEndBanner.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(160f, 48f));

            _runEndStats = PixelText.Create(overlay, "Stats", "", Palette.Bone);
            UiFactory.Place((RectTransform)_runEndStats.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), Vector2.zero);

            var newRun = UiFactory.CreatePixelButton(overlay, "NewRun", "SPIN AGAIN", true, 2, () =>
            {
                _runEndPanel.SetActive(false);
                _ctx.Game.StartRun();
            }, out _);
            UiFactory.Place((RectTransform)newRun.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(96f, 32f));

            _runEndPanel.SetActive(false);
        }

        private void BuildUpgradesPanel()
        {
            var overlay = UiFactory.CreatePanel(_root, "UpgradesPanel", new Color(0.02f, 0.01f, 0.04f, 0.94f));
            UiFactory.Stretch(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _upgradesPanel = overlay.gameObject;

            var frame = UiFactory.CreateRect(overlay, "Frame");
            UiFactory.Place(frame, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(VirtualW, VirtualH));

            var title = PixelText.Create(frame, "Title", "PERMANENT UPGRADES", Palette.Gold);
            UiFactory.Place((RectTransform)title.transform, Top, new Vector2(0f, -20f), Vector2.zero);

            _upgradesContent = UiFactory.CreateRect(frame, "Content");
            UiFactory.Stretch(_upgradesContent, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(4f, 40f), new Vector2(-4f, -32f));

            var close = UiFactory.CreatePixelButton(frame, "Close", "CLOSE", false, 1,
                () => _upgradesPanel.SetActive(false), out _);
            UiFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(48f, 16f));

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

            float y = -24f;
            foreach (var cfg in _ctx.Config.Upgrades.upgrades)
            {
                // Slice scope: meta tree + Sloth's resistance tree.
                if (cfg.category == "sin_resist" && cfg.sinId != "sloth") continue;

                var row = UiFactory.CreateNineSlice(_upgradesContent, $"Row_{cfg.id}", "UI/panel");
                UiFactory.Place((RectTransform)row.transform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(172f, 44f));

                int tier = _ctx.Upgrades.GetTier(cfg.id);

                var name = PixelText.Create(row.transform, "Name", $"{cfg.displayName} {tier}/{cfg.maxTier}", Palette.Bone, 1, PxAlign.Left);
                UiFactory.Place((RectTransform)name.transform, new Vector2(0f, 0.5f), new Vector2(8f, 13f), Vector2.zero);

                var desc = PixelText.Create(row.transform, "Desc", cfg.description, Palette.Dim, 1, PxAlign.Left, 104);
                UiFactory.Place((RectTransform)desc.transform, new Vector2(0f, 0.5f), new Vector2(8f, -6f), Vector2.zero);

                bool maxed = tier >= cfg.maxTier;
                string buyLabel = maxed ? "MAX" : _ctx.Upgrades.GetCost(cfg, tier).ToString();
                var buy = UiFactory.CreatePixelButton(row.transform, "Buy", buyLabel, true, 1, () =>
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
                }, out _);
                UiFactory.Place((RectTransform)buy.transform, new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(48f, 16f));
                buy.interactable = !maxed;

                y -= 50f;
            }
        }

        // --- Per-frame refresh ---

        public void Tick()
        {
            var hp = _ctx.Health;
            _hpText.Text = $"{Mathf.CeilToInt(hp.CurrentHp)}/{hp.MaxHp}";
            UiFactory.SetPixelBarFill(_hpFill, hp.MaxHp > 0 ? hp.CurrentHp / hp.MaxHp : 0f);

            _runCoinsText.Text = _ctx.Wallet.RunCoins.ToString();
            _metaCoinsText.Text = _ctx.Wallet.MetaCoins.ToString();
            _gemsText.Text = _ctx.Wallet.Gems.ToString();
            _levelText.Text = $"LV {_ctx.Xp.Level}";
            UiFactory.SetPixelBarFill(_xpFill, (float)_ctx.Xp.Xp / Mathf.Max(1, _ctx.Xp.XpToNextLevel()));

            RefreshStatusLine();
            RefreshSpinButton();

            _bankButton.interactable = _ctx.Game.RunActive
                && _ctx.Spin.State != SpinState.Spinning
                && _ctx.Wallet.RunCoins > 0;
        }

        private void RefreshStatusLine()
        {
            int buffs = 0, debuffs = 0;
            foreach (var e in _ctx.Buffs.Effects)
            {
                if (e.IsDebuff) debuffs++;
                else buffs++;
            }

            string line = "";
            if (buffs > 0) line += $"BLESS X{buffs} ";
            if (debuffs > 0) line += $"HEX X{debuffs} ";
            if (_ctx.Bosses.EncounterActive) line += $"SIN X{_ctx.Bosses.CurrentRewardMultiplier:0.0}";
            _statusLine.Text = line.TrimEnd();
        }

        private void RefreshSpinButton()
        {
            switch (_ctx.Spin.State)
            {
                case SpinState.Spinning:
                case SpinState.Resolving:
                    _spinLabel.Text = "...";
                    _spinButton.interactable = false;
                    break;
                case SpinState.Cooldown:
                    _spinLabel.Text = $"{_ctx.Spin.CooldownRemaining:0.0}";
                    _spinButton.interactable = false;
                    break;
                default:
                    _spinLabel.Text = _ctx.Game.RunActive ? "SPIN" : "-";
                    _spinButton.interactable = _ctx.Game.RunActive;
                    break;
            }
        }

        // --- Moments ---

        public void ShowOutcome(OutcomeResult result)
        {
            SpawnFloatingText(result.Text, result.Color, 2, new Vector2(0f, -160f), 1.2f);

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
            SpawnFloatingText(message, color, 1, new Vector2(0f, -110f), 1.8f);
        }

        public void OnRunStarted()
        {
            _runEndPanel.SetActive(false);
            _bossStrip.SetActive(false);
            HideBossCard();
        }

        public void OnBossStarted(BossEncounter encounter)
        {
            _bossStrip.SetActive(true);
            _bossSigil.sprite = ArtSprites.SigilFor(encounter.Config.id);
            _bossName.Text = encounter.Config.displayName;
            _resistRow.SetActive(encounter.Config.resistThreshold > 0);
            OnBossUpdated(encounter);
            ShowBossCard(encounter.Config.id);
            Shake();
        }

        public void OnBossUpdated(BossEncounter encounter)
        {
            _bossStatus.Text = encounter.Modifier.StatusText(encounter);
            if (encounter.Config.resistThreshold > 0)
                UiFactory.SetPixelBarFill(_resistFill, (float)encounter.Resist / encounter.Config.resistThreshold);
        }

        public void OnBossEnded()
        {
            _bossStrip.SetActive(false);
        }

        private void ShowBossCard(string sinId)
        {
            _bossCardImage.sprite = ArtSprites.CardFor(sinId);
            _bossCardOverlay.SetActive(true);
            if (_bossCardRoutine != null) _ctx.CoroutineHost.StopCoroutine(_bossCardRoutine);
            _bossCardRoutine = _ctx.CoroutineHost.StartCoroutine(BossCardAutoHide());
        }

        private IEnumerator BossCardAutoHide()
        {
            yield return new WaitForSeconds(2.6f);
            HideBossCard();
        }

        private void HideBossCard()
        {
            if (_bossCardRoutine != null)
            {
                _ctx.CoroutineHost.StopCoroutine(_bossCardRoutine);
                _bossCardRoutine = null;
            }
            _bossCardOverlay.SetActive(false);
        }

        public void ShowRunEnd(bool banked, int bankedAmount, int spins)
        {
            _runEndPanel.SetActive(true);
            _runEndBanner.sprite = ArtSprites.Get(banked ? "UI/banner_bank" : "UI/banner_bust");
            _runEndStats.Text =
                (banked ? $"+{bankedAmount} COINS BANKED" : "UNBANKED COINS FORFEIT") +
                $"\nSPINS: {spins}" +
                $"\nBANK: {_ctx.Wallet.MetaCoins}" +
                $"\nLEVEL {_ctx.Xp.Level}";
        }

        // --- Juice ---

        private void SpawnFloatingText(string message, Color color, int scale, Vector2 position, float lifetime)
        {
            var text = PixelText.Create(_frame, "Floating", message, color, scale);
            UiFactory.Place((RectTransform)text.transform, Top, position, Vector2.zero);
            _ctx.CoroutineHost.StartCoroutine(FloatAndFade(text, position, lifetime));
        }

        private IEnumerator FloatAndFade(PixelText text, Vector2 start, float lifetime)
        {
            var rt = (RectTransform)text.transform;
            Color baseColor = text.Color;
            float elapsed = 0f;

            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / lifetime);
                rt.anchoredPosition = start + new Vector2(0f, 34f * t);
                text.Color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - t * t);
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
            const float magnitude = 3f; // virtual px — big at x6 device scale
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float damper = 1f - elapsed / duration;
                _shakeRoot.anchoredPosition = new Vector2(
                    Mathf.Round((Random.value - 0.5f) * 2f * magnitude * damper),
                    Mathf.Round((Random.value - 0.5f) * 2f * magnitude * damper));
                yield return null;
            }

            _shakeRoot.anchoredPosition = Vector2.zero;
            _shakeRoutine = null;
        }
    }
}
