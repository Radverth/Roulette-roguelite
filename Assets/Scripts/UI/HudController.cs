using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SinWheel
{
    /// <summary>
    /// Pixel-art HUD on a 180x320 virtual-pixel grid with integer canvas
    /// scaling. Narrative delivery follows the design doc: plates and speech
    /// slide in over the wheel and auto-dismiss — nothing is ever modal during
    /// a run; quotes and fragments wait for the ledger (run-end) screen.
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

        // Single non-modal voice slot over the wheel: authored arrival plates
        // or composed mask+line speech.
        private GameObject _voiceSlot;
        private Image _voicePlate;
        private GameObject _voiceComposed;
        private Image _voiceMask;
        private PixelText _voiceName;
        private PixelText _voiceLine;
        private Coroutine _voiceRoutine;

        private GameObject _menuPanel;
        private PixelText _menuFragments;

        private GameObject _runEndPanel;
        private Image _runEndIntertitle;
        private PixelText _runEndQuote;
        private PixelText _runEndStats;
        private GameObject _fragmentCard;
        private PixelText _fragmentText;

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

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.referencePixelsPerUnit = 16f; // 1 canvas unit = 1 art px
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
            BuildVoiceSlot();
            BuildRunEndPanel();
            BuildUpgradesPanel();
            BuildMainMenu();
        }

        private Vector2 Top => new Vector2(0.5f, 1f);

        // --- Construction ---

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

        private void BuildVoiceSlot()
        {
            // Slides in over the wheel, never over the spin button; the player
            // can always spin through it. Auto-dismisses.
            _voiceSlot = UiFactory.CreateRect(_frame, "VoiceSlot").gameObject;
            UiFactory.Place((RectTransform)_voiceSlot.transform, Top, new Vector2(0f, -138f), new Vector2(160f, 64f));

            _voicePlate = UiFactory.CreateSpriteImage(_voiceSlot.transform, "Plate", null, new Vector2(160f, 64f));
            UiFactory.Place((RectTransform)_voicePlate.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 64f));

            var composed = UiFactory.CreateNineSlice(_voiceSlot.transform, "Composed", "UI/panel");
            UiFactory.Stretch((RectTransform)composed.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            composed.raycastTarget = false;
            _voiceComposed = composed.gameObject;

            _voiceMask = UiFactory.CreateSpriteImage(composed.transform, "Mask", null, new Vector2(48f, 48f));
            UiFactory.Place((RectTransform)_voiceMask.transform, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(48f, 48f));

            _voiceName = PixelText.Create(composed.transform, "Name", "", Palette.Bone, 1, PxAlign.Left);
            UiFactory.Place((RectTransform)_voiceName.transform, new Vector2(0f, 0.5f), new Vector2(56f, 21f), Vector2.zero);

            var divider = UiFactory.CreateSpriteImage(composed.transform, "Divider", "Narrative/ornament_divider", new Vector2(32f, 8f));
            UiFactory.Place((RectTransform)divider.transform, new Vector2(0f, 0.5f), new Vector2(72f, 12f), new Vector2(32f, 8f));

            _voiceLine = PixelText.Create(composed.transform, "Line", "", Palette.Bone, 1, PxAlign.Left, 96);
            UiFactory.Place((RectTransform)_voiceLine.transform, new Vector2(0f, 0.5f), new Vector2(56f, -8f), Vector2.zero);

            _voiceSlot.SetActive(false);
        }

        private void BuildMainMenu()
        {
            var overlay = UiFactory.CreatePanel(_root, "MainMenu", Palette.Night);
            UiFactory.Stretch(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _menuPanel = overlay.gameObject;

            var frame = UiFactory.CreateRect(overlay, "Frame");
            UiFactory.Place(frame, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(VirtualW, VirtualH));

            var title = PixelText.Create(frame, "Title", "SIN WHEEL", Palette.Gold, 2);
            UiFactory.Place((RectTransform)title.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 118f), Vector2.zero);

            // The intertitle carries its own text: THE WHEEL / SEVEN WILL ANSWER / TAP TO BEGIN.
            var card = UiFactory.CreateRect(frame, "StartCard");
            UiFactory.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0f, 52f), new Vector2(128f, 72f));
            var cardImg = card.gameObject.AddComponent<Image>();
            cardImg.sprite = ArtSprites.Get("Narrative/intertitle_start");
            var cardButton = card.gameObject.AddComponent<Button>();
            cardButton.transition = Selectable.Transition.None;
            cardButton.onClick.AddListener(PlayFromMenu);

            var play = UiFactory.CreatePixelButton(frame, "Play", "PLAY", true, 2, PlayFromMenu, out _);
            UiFactory.Place((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -34f), new Vector2(96f, 32f));

            var musicLabel = PixelText.Create(frame, "MusicLabel", "MUSIC", Palette.Bone, 1, PxAlign.Left);
            UiFactory.Place((RectTransform)musicLabel.transform, new Vector2(0.5f, 0.5f), new Vector2(-82f, -78f), Vector2.zero);

            UiFactory.CreatePixelSlider(frame, "MusicSlider", new Vector2(0.5f, 0.5f), new Vector2(14f, -78f),
                _ctx.Save.Data.musicVolume, v =>
                {
                    Music.SetVolume(v);
                    _ctx.Save.Data.musicVolume = v;
                });

            _menuFragments = PixelText.Create(frame, "Fragments", "", Palette.Dim);
            UiFactory.Place((RectTransform)_menuFragments.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -112f), Vector2.zero);

            _menuPanel.SetActive(false);
        }

        private void PlayFromMenu()
        {
            _menuPanel.SetActive(false);
            _ctx.Save.Persist(); // volume changes commit when leaving the menu
            _ctx.Game.StartRun();
        }

        public void ShowMainMenu()
        {
            _menuFragments.Text = $"FRAGMENTS {_ctx.Narrative.FragmentCount}/{NarrativeSystem.TotalFragments}";
            HideVoice();
            _runEndPanel.SetActive(false);
            _menuPanel.SetActive(true);
        }

        private void BuildRunEndPanel()
        {
            var overlay = UiFactory.CreatePanel(_root, "RunEndPanel", Palette.Night);
            UiFactory.Stretch(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _runEndPanel = overlay.gameObject;

            var frame = UiFactory.CreateRect(overlay, "Frame");
            UiFactory.Place(frame, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(VirtualW, VirtualH));

            _runEndIntertitle = UiFactory.CreateSpriteImage(frame, "Intertitle", null, new Vector2(128f, 72f));
            UiFactory.Place((RectTransform)_runEndIntertitle.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 96f), new Vector2(128f, 72f));

            _runEndQuote = PixelText.Create(frame, "Quote", "", Palette.Dim, 1, PxAlign.Center, 150);
            UiFactory.Place((RectTransform)_runEndQuote.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 34f), Vector2.zero);

            _runEndStats = PixelText.Create(frame, "Stats", "", Palette.Bone);
            UiFactory.Place((RectTransform)_runEndStats.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), Vector2.zero);

            // Fragment plate: blank body in the art, filled at runtime.
            var card = UiFactory.CreateSpriteImage(frame, "FragmentCard", "Narrative/fragment_card", new Vector2(80f, 112f));
            UiFactory.Place((RectTransform)card.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(80f, 112f));
            _fragmentCard = card.gameObject;
            _fragmentText = PixelText.Create(card.transform, "Text", "", Palette.Bone, 1, PxAlign.Center, 64);
            UiFactory.Place((RectTransform)_fragmentText.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), Vector2.zero);
            _fragmentCard.SetActive(false);

            var newRun = UiFactory.CreatePixelButton(frame, "NewRun", "SPIN AGAIN", true, 2, () =>
            {
                _runEndPanel.SetActive(false);
                _ctx.Game.StartRun();
            }, out _);
            UiFactory.Place((RectTransform)newRun.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -92f), new Vector2(96f, 32f));

            var menu = UiFactory.CreatePixelButton(frame, "Menu", "MENU", false, 1, ShowMainMenu, out _);
            UiFactory.Place((RectTransform)menu.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -122f), new Vector2(48f, 16f));

            _runEndPanel.SetActive(false);
        }

        private void BuildUpgradesPanel()
        {
            var overlay = UiFactory.CreatePanel(_root, "UpgradesPanel", Palette.Night);
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
                UiFactory.Place((RectTransform)desc.transform, new Vector2(0f, 0.5f), new Vector2(8f, -8f), Vector2.zero);

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

        // --- Voice (plates + speech), never modal ---

        /// <summary>Authored arrival plate — the sin's one line, baked into the art.</summary>
        public void ShowArrivalPlate(string sinId)
        {
            _voicePlate.sprite = ArtSprites.Get("Narrative/plate_" + sinId);
            _voicePlate.gameObject.SetActive(true);
            _voiceComposed.SetActive(false);
            PresentVoice(2.2f);
        }

        /// <summary>Composed speech: mask + name + line, for everything with variable text.</summary>
        public void ShowSpeech(string speakerId, string line)
        {
            if (string.IsNullOrEmpty(line)) return;

            _voiceMask.sprite = ArtSprites.Get("Narrative/mask_" + speakerId);
            _voiceName.Text = NarrativeSystem.SpeakerName(speakerId);
            _voiceName.Color = _ctx.Narrative.SpeakerColor(speakerId);
            _voiceLine.Text = line;
            _voicePlate.gameObject.SetActive(false);
            _voiceComposed.SetActive(true);
            PresentVoice(2.2f);
        }

        private void PresentVoice(float seconds)
        {
            if (_voiceRoutine != null) _ctx.CoroutineHost.StopCoroutine(_voiceRoutine);
            _voiceSlot.SetActive(true);
            _voiceRoutine = _ctx.CoroutineHost.StartCoroutine(VoiceAutoHide(seconds));
        }

        private IEnumerator VoiceAutoHide(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            _voiceSlot.SetActive(false);
            _voiceRoutine = null;
        }

        private void HideVoice()
        {
            if (_voiceRoutine != null)
            {
                _ctx.CoroutineHost.StopCoroutine(_voiceRoutine);
                _voiceRoutine = null;
            }
            _voiceSlot.SetActive(false);
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
            _menuPanel.SetActive(false);
            _runEndPanel.SetActive(false);
            _bossStrip.SetActive(false);
            HideVoice();
        }

        public void OnBossStarted(BossEncounter encounter)
        {
            _bossStrip.SetActive(true);
            _bossSigil.sprite = ArtSprites.SigilFor(encounter.Config.id);
            _bossName.Text = encounter.Config.displayName;
            _resistRow.SetActive(encounter.Config.resistThreshold > 0);
            OnBossUpdated(encounter);
            ShowArrivalPlate(encounter.Config.id);
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

        public void ShowRunEnd(bool banked, int bankedAmount, int spins)
        {
            _runEndPanel.SetActive(true);
            _runEndIntertitle.sprite = ArtSprites.Get(banked ? "Narrative/intertitle_bank" : "Narrative/intertitle_bust");

            string fragment = _ctx.Narrative.ConsumePendingFragment();
            if (!string.IsNullOrEmpty(fragment))
            {
                // A fragment owns the ledger this time; the quote can wait.
                _ctx.Narrative.ConsumeRunEndQuote();
                _fragmentCard.SetActive(true);
                _fragmentText.Text = fragment;
                _runEndQuote.Text = "";
                _runEndStats.Text = "";
            }
            else
            {
                _fragmentCard.SetActive(false);
                _runEndQuote.Text = _ctx.Narrative.ConsumeRunEndQuote() ?? "";
                _runEndStats.Text =
                    (banked ? $"+{bankedAmount} COINS BANKED" : "UNBANKED COINS FORFEIT") +
                    $"\nSPINS {spins} - BANK {_ctx.Wallet.MetaCoins} - LV {_ctx.Xp.Level}";
            }
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
            const float magnitude = 3f;
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
