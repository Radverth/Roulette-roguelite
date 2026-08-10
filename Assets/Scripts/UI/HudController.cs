using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SinWheel
{
    /// <summary>
    /// Pixel-art HUD on a 180x320 virtual-pixel grid with integer canvas
    /// scaling. It has to show three time horizons at once now: the chain on
    /// this spin, the Notice building across the run, and the quota that
    /// decides whether leaving was worth anything.
    /// </summary>
    public sealed class HudController
    {
        private const int VirtualW = 180;
        private const int VirtualH = 320;
        private const int StreakPips = 5;

        private readonly GameContext _ctx;

        public WheelController Wheel { get; private set; }

        private RectTransform _root;
        private RectTransform _frame;
        private RectTransform _shakeRoot;

        private PixelText _hpText;
        private Image _hpFill;
        private PixelText _runCoinsText;
        private PixelText _metaCoinsText;
        private PixelText _levelText;
        private Image _xpFill;
        private PixelText _statusLine;

        private Image _noticeEye;
        private Image _noticeFill;
        private readonly List<Image> _streakPips = new List<Image>();
        private Image _multiplierBadge;
        private PixelText _multiplierText;

        private Image _quotaMarker;
        private PixelText _quotaText;

        private Button _spinButton;
        private PixelText _spinLabel;
        private Button _bankButton;
        private Button _titheButton;

        private GameObject _bossStrip;
        private Image _bossSigil;
        private Image _bossBreakGlyph;
        private PixelText _bossName;
        private PixelText _bossStatus;
        private PixelText _bossSpinsLeft;

        private GameObject _voiceSlot;
        private Image _voicePlate;
        private GameObject _voiceComposed;
        private Image _voiceMask;
        private PixelText _voiceName;
        private PixelText _voiceLine;
        private Coroutine _voiceRoutine;

        private GameObject _menuPanel;
        private PixelText _menuFragments;
        private PixelText _menuDebt;
        private readonly List<Image> _markSeals = new List<Image>();

        private GameObject _runEndPanel;
        private Image _runEndIntertitle;
        private Image _debtSeal;
        private PixelText _runEndQuote;
        private PixelText _runEndStats;
        private GameObject _fragmentCard;
        private PixelText _fragmentText;

        private GameObject _upgradesPanel;
        private RectTransform _upgradesContent;

        private readonly List<Image> _depthPips = new List<Image>();
        private PixelText _tableLabel;
        private PixelText _tableMultiplier;
        private Image _bossSigilSecond;
        private RectTransform _foresightRow;

        private RectTransform _nudgeRow;
        private Button _nudgeLeft;
        private Image _nudgeLeftIcon;
        private Button _nudgeRight;
        private Image _nudgeRightIcon;
        private RectTransform _nudgeCostRow;
        private Image _nudgeCharge;
        private readonly List<Image> _nudgeCostPips = new List<Image>();

        private RectTransform _pledgeColumn;
        private readonly List<Image> _pledgeEmblems = new List<Image>();
        private string _pledgeSignature = "";

        private ScorePanel _scorePanel;
        private PledgeScreen _pledges;

        private ForgeScreen _forge;
        private TutorialScreen _tutorial;
        private TableInviteScreen _tableInvite;
        private InterludeScreen _interlude;
        private Coroutine _shakeRoutine;

        public HudController(GameContext ctx)
        {
            _ctx = ctx;
        }

        private Vector2 Top => new Vector2(0.5f, 1f);

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
            BuildPressureRow();
            BuildQuotaRow();
            BuildBossStrip();
            BuildWheelArea();
            BuildBottomBar();
            BuildVoiceSlot();
            BuildNudgeControls();
            BuildPledgeColumn();
            BuildRunEndPanel();
            BuildUpgradesPanel();
            BuildMainMenu();

            _scorePanel = new ScorePanel(_ctx, _frame);
            _pledges = new PledgeScreen(_ctx, _root, null);
            _forge = new ForgeScreen(_ctx, _root, () => _ctx.Game.StartRun());
            _tutorial = new TutorialScreen(_ctx, _root, OnTutorialClosed);
            _tableInvite = new TableInviteScreen(_ctx, _root, OnTableAccepted);
            _interlude = new InterludeScreen(_ctx, _root, null);
        }

        /// <summary>
        /// Reading the rules on a first run leads straight into it; opening the
        /// wizard from the menu returns to the menu.
        /// </summary>
        private void OnTutorialClosed()
        {
            if (_tutorialFromMenu)
            {
                _tutorialFromMenu = false;
                ShowMainMenu();
                return;
            }
            PlayFromMenu();
        }

        private bool _tutorialFromMenu;

        private void OpenTutorialFromMenu()
        {
            _tutorialFromMenu = true;
            _menuPanel.SetActive(false);
            _tutorial.Open();
        }

        // --- Construction ---

        private void BuildTopBar()
        {
            // The descent track replaces the title during a run: where you are,
            // how deep it goes, and what it is paying.
            _tableLabel = PixelText.Create(_shakeRoot, "TableLabel", "T I", Palette.Gold, 1, PxAlign.Left);
            UiFactory.Place((RectTransform)_tableLabel.transform, Top, new Vector2(-86f, -8f), Vector2.zero);

            for (int i = 0; i < 7; i++)
            {
                var pip = UiFactory.CreateSpriteImage(_shakeRoot, $"DepthPip_{i}", "Escalation/depth_pip_locked",
                    new Vector2(10f, 10f));
                UiFactory.Place((RectTransform)pip.transform, Top, new Vector2(-20f + i * 10f, -8f), new Vector2(10f, 10f));
                _depthPips.Add(pip);
            }

            _tableMultiplier = PixelText.Create(_shakeRoot, "TableMult", "", Palette.Bone, 1, PxAlign.Right);
            UiFactory.Place((RectTransform)_tableMultiplier.transform, Top, new Vector2(86f, -8f), Vector2.zero);

            var hpIcon = UiFactory.CreateSpriteImage(_shakeRoot, "HpIcon", "Icons/ui_hp", new Vector2(16f, 16f));
            UiFactory.Place((RectTransform)hpIcon.transform, Top, new Vector2(-80f, -22f), new Vector2(16f, 16f));

            _hpFill = UiFactory.CreatePixelBar(_shakeRoot, "HpBar", "UI/bar_fill_hp", Top, new Vector2(-38f, -22f));

            _hpText = PixelText.Create(_shakeRoot, "HpText", "", Palette.Bone, 1, PxAlign.Left);
            UiFactory.Place((RectTransform)_hpText.transform, Top, new Vector2(0f, -22f), Vector2.zero);

            var coinIcon = UiFactory.CreateSpriteImage(_shakeRoot, "CoinIcon", "Icons/seg_coin", new Vector2(16f, 16f));
            UiFactory.Place((RectTransform)coinIcon.transform, Top, new Vector2(-80f, -37f), new Vector2(16f, 16f));
            _runCoinsText = PixelText.Create(_shakeRoot, "RunCoins", "0", Palette.Gold, 1, PxAlign.Left);
            UiFactory.Place((RectTransform)_runCoinsText.transform, Top, new Vector2(-70f, -37f), Vector2.zero);

            var relicIcon = UiFactory.CreateSpriteImage(_shakeRoot, "RelicIcon", "Icons/ui_relic", new Vector2(16f, 16f));
            UiFactory.Place((RectTransform)relicIcon.transform, Top, new Vector2(-24f, -37f), new Vector2(16f, 16f));
            _metaCoinsText = PixelText.Create(_shakeRoot, "MetaCoins", "0", Palette.Bone, 1, PxAlign.Left);
            UiFactory.Place((RectTransform)_metaCoinsText.transform, Top, new Vector2(-14f, -37f), Vector2.zero);

            _levelText = PixelText.Create(_shakeRoot, "Level", "LV 1", Palette.Teal, 1, PxAlign.Left);
            UiFactory.Place((RectTransform)_levelText.transform, Top, new Vector2(-86f, -50f), Vector2.zero);

            _xpFill = UiFactory.CreatePixelBar(_shakeRoot, "XpBar", "UI/bar_fill_xp", Top, new Vector2(2f, -50f));
        }

        /// <summary>Notice on the left, the streak chain on the right: dread and greed, side by side.</summary>
        private void BuildPressureRow()
        {
            _noticeEye = UiFactory.CreateSpriteImage(_shakeRoot, "NoticeEye", "Loop/notice_eye_0", new Vector2(16f, 16f));
            UiFactory.Place((RectTransform)_noticeEye.transform, Top, new Vector2(-80f, -64f), new Vector2(16f, 16f));

            var track = UiFactory.CreateSpriteImage(_shakeRoot, "NoticeTrack", "Loop/notice_track", new Vector2(64f, 10f));
            UiFactory.Place((RectTransform)track.transform, Top, new Vector2(-38f, -64f), new Vector2(64f, 10f));

            var fill = UiFactory.CreateRect(track.transform, "Fill");
            UiFactory.Stretch(fill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _noticeFill = fill.gameObject.AddComponent<Image>();
            _noticeFill.sprite = ArtSprites.Get("Loop/notice_fill_cold");
            _noticeFill.type = Image.Type.Filled;
            _noticeFill.fillMethod = Image.FillMethod.Horizontal;
            _noticeFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _noticeFill.raycastTarget = false;

            var streakFrame = UiFactory.CreateSpriteImage(_shakeRoot, "StreakFrame", "Loop/streak_frame", new Vector2(56f, 14f));
            UiFactory.Place((RectTransform)streakFrame.transform, Top, new Vector2(22f, -64f), new Vector2(56f, 14f));

            for (int i = 0; i < StreakPips; i++)
            {
                var pip = UiFactory.CreateSpriteImage(streakFrame.transform, $"Pip_{i}", "Loop/streak_pip_empty", new Vector2(8f, 8f));
                UiFactory.Place((RectTransform)pip.transform, new Vector2(0.5f, 0.5f),
                    new Vector2(-20f + i * 10f, 0f), new Vector2(8f, 8f));
                _streakPips.Add(pip);
            }

            _multiplierBadge = UiFactory.CreateSpriteImage(_shakeRoot, "MultBadge", "Loop/multiplier_badge", new Vector2(28f, 14f));
            UiFactory.Place((RectTransform)_multiplierBadge.transform, Top, new Vector2(68f, -64f), new Vector2(28f, 14f));
            _multiplierText = PixelText.Create(_multiplierBadge.transform, "Mult", "X1", Palette.Bone);
            UiFactory.Place((RectTransform)_multiplierText.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), Vector2.zero);
        }

        /// <summary>What the house expects before you are allowed to feel safe.</summary>
        private void BuildQuotaRow()
        {
            var track = UiFactory.CreateSpriteImage(_shakeRoot, "QuotaTrack", "Loop/quota_track", new Vector2(96f, 12f));
            UiFactory.Place((RectTransform)track.transform, Top, new Vector2(-40f, -80f), new Vector2(96f, 12f));

            _quotaMarker = UiFactory.CreateSpriteImage(track.transform, "Marker", "Loop/quota_marker", new Vector2(9f, 16f));
            UiFactory.Place((RectTransform)_quotaMarker.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(9f, 16f));

            _quotaText = PixelText.Create(_shakeRoot, "QuotaText", "", Palette.Dim, 1, PxAlign.Left);
            UiFactory.Place((RectTransform)_quotaText.transform, Top, new Vector2(14f, -80f), Vector2.zero);
        }

        private void BuildBossStrip()
        {
            var strip = UiFactory.CreateNineSlice(_shakeRoot, "BossStrip", "UI/panel_danger");
            UiFactory.Place((RectTransform)strip.transform, Top, new Vector2(0f, -104f), new Vector2(172f, 32f));
            _bossStrip = strip.gameObject;

            _bossSigil = UiFactory.CreateSpriteImage(strip.transform, "Sigil", null, new Vector2(24f, 24f));
            UiFactory.Place((RectTransform)_bossSigil.transform, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(24f, 24f));

            // From Table IV they come in pairs, so the strip carries two marks.
            _bossSigilSecond = UiFactory.CreateSpriteImage(strip.transform, "SigilTwo", null, new Vector2(16f, 16f));
            UiFactory.Place((RectTransform)_bossSigilSecond.transform, new Vector2(0f, 0.5f), new Vector2(34f, -8f), new Vector2(16f, 16f));
            _bossSigilSecond.gameObject.SetActive(false);

            _bossName = PixelText.Create(strip.transform, "Name", "", Palette.Gold, 1, PxAlign.Left);
            UiFactory.Place((RectTransform)_bossName.transform, new Vector2(0f, 0.5f), new Vector2(34f, 7f), Vector2.zero);

            // Break progress on the left, spins remaining on the right: two
            // short strings instead of one long one that ran into the glyph.
            _bossStatus = PixelText.Create(strip.transform, "Status", "", Palette.Bone, 1, PxAlign.Left);
            UiFactory.Place((RectTransform)_bossStatus.transform, new Vector2(0f, 0.5f), new Vector2(34f, -4f), Vector2.zero);

            _bossSpinsLeft = PixelText.Create(strip.transform, "SpinsLeft", "", Palette.Dim, 1, PxAlign.Right);
            UiFactory.Place((RectTransform)_bossSpinsLeft.transform, new Vector2(1f, 0.5f), new Vector2(-8f, -4f), Vector2.zero);

            _bossBreakGlyph = UiFactory.CreateSpriteImage(strip.transform, "BreakGlyph", null, new Vector2(16f, 16f));
            UiFactory.Place((RectTransform)_bossBreakGlyph.transform, new Vector2(1f, 0.5f), new Vector2(-16f, 7f), new Vector2(16f, 16f));

            _bossStrip.SetActive(false);
        }

        private void BuildWheelArea()
        {
            var container = UiFactory.CreateRect(_shakeRoot, "WheelArea");
            UiFactory.Place(container, Top, new Vector2(0f, -197f), new Vector2(160f, 160f));
            Wheel = new WheelController(_ctx, container);

            _statusLine = PixelText.Create(_shakeRoot, "StatusLine", "", Palette.Bone);
            UiFactory.Place((RectTransform)_statusLine.transform, Top, new Vector2(0f, -266f), Vector2.zero);

            // The Understudy's winnings: the next three wedges, already rolled,
            // in the gap between the sin strip and the rim.
            _foresightRow = UiFactory.CreateRect(_shakeRoot, "Foresight");
            UiFactory.Place(_foresightRow, Top, new Vector2(0f, -127f), new Vector2(48f, 12f));
            _foresightRow.gameObject.SetActive(false);
        }

        private void BuildBottomBar()
        {
            _spinButton = UiFactory.CreatePixelButton(_shakeRoot, "SpinButton", "SPIN", true, 2,
                OnSpinPressed, out _spinLabel);
            UiFactory.Place((RectTransform)_spinButton.transform, Top, new Vector2(0f, -287f), new Vector2(96f, 32f));

            _titheButton = UiFactory.CreatePixelButton(_shakeRoot, "TitheButton", "TITHE", false, 1,
                () => _ctx.Game.Tithe(), out _, "Loop/button_tithe", 7f);
            UiFactory.Place((RectTransform)_titheButton.transform, Top, new Vector2(-58f, -313f), new Vector2(48f, 16f));

            _bankButton = UiFactory.CreatePixelButton(_shakeRoot, "BankButton", "BANK", false, 1,
                () => _ctx.Game.BankAndEndRun(), out _);
            UiFactory.Place((RectTransform)_bankButton.transform, Top, new Vector2(0f, -313f), new Vector2(48f, 16f));

            var upgrades = UiFactory.CreatePixelButton(_shakeRoot, "UpgradesButton", "UPGRADE", false, 1,
                ToggleUpgradesPanel, out _);
            UiFactory.Place((RectTransform)upgrades.transform, Top, new Vector2(58f, -313f), new Vector2(48f, 16f));
        }

        /// <summary>
        /// The decision inside the spin. Two thumbs flanking SPIN, and the
        /// price in Notice segments directly above them - the player is never
        /// asked to cheat without being shown the bill first.
        /// </summary>
        private void BuildNudgeControls()
        {
            _nudgeRow = UiFactory.CreateRect(_shakeRoot, "NudgeRow");
            UiFactory.Stretch(_nudgeRow, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _nudgeLeft = CreateNudgeButton("NudgeLeft", -70f, -1, out _nudgeLeftIcon);
            _nudgeRight = CreateNudgeButton("NudgeRight", 70f, 1, out _nudgeRightIcon);

            _nudgeCostRow = UiFactory.CreateRect(_nudgeRow, "NudgeCost");
            UiFactory.Place(_nudgeCostRow, Top, new Vector2(0f, -266f), new Vector2(48f, 10f));

            // The bar drains with the window. Filled rather than scaled, so
            // every frame of it still lands on whole pixels.
            _nudgeCharge = UiFactory.CreateSpriteImage(_nudgeCostRow, "ChargeBar", "Pledges/nudge_charge_bar",
                new Vector2(48f, 10f));
            UiFactory.Place((RectTransform)_nudgeCharge.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48f, 10f));
            _nudgeCharge.type = Image.Type.Filled;
            _nudgeCharge.fillMethod = Image.FillMethod.Horizontal;
            _nudgeCharge.fillOrigin = (int)Image.OriginHorizontal.Left;

            for (int i = 0; i < 3; i++)
            {
                var pip = UiFactory.CreateSpriteImage(_nudgeCostRow, $"Pip_{i}", "Pledges/nudge_cost_pip_free",
                    new Vector2(8f, 10f));
                UiFactory.Place((RectTransform)pip.transform, new Vector2(0.5f, 0.5f),
                    new Vector2(-9f + i * 9f, 0f), new Vector2(8f, 10f));
                _nudgeCostPips.Add(pip);
            }

            _nudgeRow.gameObject.SetActive(false);
        }

        private Button CreateNudgeButton(string name, float x, int direction, out Image icon)
        {
            icon = UiFactory.CreateSpriteImage(_nudgeRow, name, "Pledges/nudge_left_ready", new Vector2(28f, 28f));
            UiFactory.Place((RectTransform)icon.transform, Top, new Vector2(x, -287f), new Vector2(28f, 28f));

            // CreateSpriteImage opts out of raycasts by default; a button must opt back in.
            icon.raycastTarget = true;

            var button = icon.gameObject.AddComponent<Button>();
            button.targetGraphic = icon;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => _ctx.Spin.Nudge(direction));
            return button;
        }

        /// <summary>
        /// The build, down the left of the wheel: five emblems clear of the
        /// disc, each one a door into the Pledge screen.
        /// </summary>
        private void BuildPledgeColumn()
        {
            _pledgeColumn = UiFactory.CreateRect(_shakeRoot, "PledgeColumn");
            UiFactory.Stretch(_pledgeColumn, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            for (int i = 0; i < 5; i++)
            {
                var emblem = UiFactory.CreateSpriteImage(_pledgeColumn, $"Emblem_{i}", null, new Vector2(24f, 24f));
                UiFactory.Place((RectTransform)emblem.transform, Top,
                    new Vector2(-78f, -148f - i * 24f), new Vector2(24f, 24f));

                emblem.raycastTarget = true;
                var button = emblem.gameObject.AddComponent<Button>();
                button.targetGraphic = emblem;
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(OpenPledges);

                emblem.gameObject.SetActive(false);
                _pledgeEmblems.Add(emblem);
            }
        }

        public void OpenPledges()
        {
            Sfx.Tick();
            _pledges.Open();
        }

        /// <summary>
        /// One button, two verbs. While the nudge window is open it takes what
        /// is under the pointer instead — nobody should be made to wait out a
        /// decision they have already made.
        /// </summary>
        private void OnSpinPressed()
        {
            if (_ctx.Spin.State == SpinState.Nudging)
            {
                _ctx.Spin.CommitLanding();
                return;
            }
            _ctx.Spin.RequestSpin();
        }

        private void BuildVoiceSlot()
        {
            // Slides in over the wheel, never over the spin button; the player
            // can always spin through it. Auto-dismisses.
            _voiceSlot = UiFactory.CreateRect(_frame, "VoiceSlot").gameObject;
            UiFactory.Place((RectTransform)_voiceSlot.transform, Top, new Vector2(0f, -162f), new Vector2(160f, 64f));

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

            var card = UiFactory.CreateRect(frame, "StartCard");
            UiFactory.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0f, 52f), new Vector2(128f, 72f));
            var cardImg = card.gameObject.AddComponent<Image>();
            cardImg.sprite = ArtSprites.Get("Narrative/intertitle_start");
            var cardButton = card.gameObject.AddComponent<Button>();
            cardButton.transition = Selectable.Transition.None;
            cardButton.onClick.AddListener(PlayFromMenu);

            var play = UiFactory.CreatePixelButton(frame, "Play", "PLAY", true, 2, PlayFromMenu, out _);
            UiFactory.Place((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(96f, 32f));

            // Scale 2 keeps the 48x16 sprite on an integer multiple; the label
            // auto-shrinks to fit inside it.
            var howTo = UiFactory.CreatePixelButton(frame, "HowTo", "HOW TO PLAY", false, 2,
                OpenTutorialFromMenu, out _);
            UiFactory.Place((RectTransform)howTo.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -56f), new Vector2(96f, 32f));

            _menuDebt = PixelText.Create(frame, "Debt", "", Palette.Bone);
            UiFactory.Place((RectTransform)_menuDebt.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), Vector2.zero);

            // Seven sockets. What the house has taken back for letting you pay.
            var markTrack = UiFactory.CreateSpriteImage(frame, "MarkTrack", "Escalation/mark_track", new Vector2(104f, 16f));
            UiFactory.Place((RectTransform)markTrack.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -94f), new Vector2(104f, 16f));
            for (int i = 1; i <= 7; i++)
            {
                var seal = UiFactory.CreateSpriteImage(markTrack.transform, $"Mark_{i}", null, new Vector2(12f, 12f));
                UiFactory.Place((RectTransform)seal.transform, new Vector2(0.5f, 0.5f),
                    new Vector2(-42f + (i - 1) * 14f, 0f), new Vector2(12f, 12f));
                _markSeals.Add(seal);
            }

            var musicLabel = PixelText.Create(frame, "MusicLabel", "MUSIC", Palette.Bone, 1, PxAlign.Left);
            UiFactory.Place((RectTransform)musicLabel.transform, new Vector2(0.5f, 0.5f), new Vector2(-82f, -112f), Vector2.zero);

            UiFactory.CreatePixelSlider(frame, "MusicSlider", new Vector2(0.5f, 0.5f), new Vector2(14f, -112f),
                _ctx.Save.Data.musicVolume, v =>
                {
                    Music.SetVolume(v);
                    _ctx.Save.Data.musicVolume = v;
                });

            _menuFragments = PixelText.Create(frame, "Fragments", "", Palette.Dim);
            UiFactory.Place((RectTransform)_menuFragments.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -130f), Vector2.zero);

            var pledgeButton = UiFactory.CreatePixelButton(frame, "Pledges", "PLEDGES", false, 1,
                OpenPledges, out _);
            UiFactory.Place((RectTransform)pledgeButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -146f), new Vector2(64f, 16f));

            _menuPanel.SetActive(false);
        }

        private void PlayFromMenu()
        {
            _menuPanel.SetActive(false);
            _ctx.Save.Persist(); // volume changes commit when leaving the menu

            // First time out, the rules come before the wheel.
            if (!_ctx.Save.Data.tutorialSeen)
            {
                _tutorialFromMenu = false;
                _tutorial.Open();
                return;
            }

            _ctx.Game.StartRun();
        }

        public void ShowMainMenu()
        {
            _menuFragments.Text = $"FRAGMENTS {_ctx.Narrative.FragmentCount}/{NarrativeSystem.TotalFragments}";
            _menuDebt.Text = $"DEBT {_ctx.Debt.Debt} - QUOTA {_ctx.Debt.Quota}";
            for (int i = 0; i < _markSeals.Count; i++)
            {
                int mark = i + 1;
                _markSeals[i].sprite = ArtSprites.Get(
                    _ctx.Marks.IsEarned(mark) ? $"Escalation/mark_{mark}_earned" : $"Escalation/mark_{mark}_locked");
            }
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
            UiFactory.Place((RectTransform)_runEndIntertitle.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(128f, 72f));

            _debtSeal = UiFactory.CreateSpriteImage(frame, "DebtSeal", null, new Vector2(32f, 32f));
            UiFactory.Place((RectTransform)_debtSeal.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 52f), new Vector2(32f, 32f));

            _runEndQuote = PixelText.Create(frame, "Quote", "", Palette.Dim, 1, PxAlign.Center, 150);
            UiFactory.Place((RectTransform)_runEndQuote.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 22f), Vector2.zero);

            _runEndStats = PixelText.Create(frame, "Stats", "", Palette.Bone);
            UiFactory.Place((RectTransform)_runEndStats.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), Vector2.zero);

            var card = UiFactory.CreateSpriteImage(frame, "FragmentCard", "Narrative/fragment_card", new Vector2(80f, 112f));
            UiFactory.Place((RectTransform)card.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(80f, 112f));
            _fragmentCard = card.gameObject;
            _fragmentText = PixelText.Create(card.transform, "Text", "", Palette.Bone, 1, PxAlign.Center, 64);
            UiFactory.Place((RectTransform)_fragmentText.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), Vector2.zero);
            _fragmentCard.SetActive(false);

            var newRun = UiFactory.CreatePixelButton(frame, "NewRun", "THE FORGE", true, 2, () =>
            {
                _runEndPanel.SetActive(false);
                _forge.Open();
            }, out _);
            UiFactory.Place((RectTransform)newRun.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -96f), new Vector2(96f, 32f));

            var menu = UiFactory.CreatePixelButton(frame, "Menu", "MENU", false, 1, ShowMainMenu, out _);
            UiFactory.Place((RectTransform)menu.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -126f), new Vector2(48f, 16f));

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

            // Every unlocked sin adds a resistance row, so the list has to scroll.
            var viewport = UiFactory.CreateRect(frame, "Viewport");
            UiFactory.Stretch(viewport, Vector2.zero, Vector2.one, new Vector2(4f, 40f), new Vector2(-4f, -32f));
            viewport.gameObject.AddComponent<RectMask2D>();

            _upgradesContent = UiFactory.CreateRect(viewport, "Content");
            _upgradesContent.anchorMin = new Vector2(0f, 1f);
            _upgradesContent.anchorMax = new Vector2(1f, 1f);
            _upgradesContent.pivot = new Vector2(0.5f, 1f);
            _upgradesContent.offsetMin = new Vector2(0f, 0f);
            _upgradesContent.offsetMax = new Vector2(0f, 0f);

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = _upgradesContent;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 12f;

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

            float y = -26f;
            foreach (var cfg in _ctx.Config.Upgrades.upgrades)
            {
                if (cfg.category == "sin_resist" && !IsSinUnlocked(cfg.sinId)) continue;

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

            _upgradesContent.sizeDelta = new Vector2(_upgradesContent.sizeDelta.x, Mathf.Abs(y) + 8f);
        }

        private bool IsSinUnlocked(string sinId)
        {
            foreach (var sin in _ctx.Config.Sins.sins)
                if (sin.id == sinId) return sin.unlockLevel <= _ctx.Xp.Level;
            return false;
        }

        // --- Per-frame refresh ---

        /// <summary>The fork: deeper, or cash out and end the run.</summary>
        public void ShowTableInvite() => _tableInvite.Open();

        /// <summary>An interlude sits on the beat after a decision, never at random.</summary>
        public void ShowInterlude(bool sideTable) => _interlude.Open(sideTable);

        private void OnTableAccepted()
        {
            // The transition is the natural breath point for a mini-game.
            _interlude.Open(sideTable: false);
        }

        public void Tick()
        {
            Wheel.SyncToRing();
            _interlude.Tick(Time.deltaTime);

            var hp = _ctx.Health;
            _hpText.Text = $"{Mathf.CeilToInt(hp.CurrentHp)}/{hp.MaxHp}";
            UiFactory.SetPixelBarFill(_hpFill, hp.MaxHp > 0 ? hp.CurrentHp / hp.MaxHp : 0f);

            _runCoinsText.Text = _ctx.Wallet.RunCoins.ToString();
            _metaCoinsText.Text = _ctx.Wallet.MetaCoins.ToString();
            _levelText.Text = $"LV {_ctx.Xp.Level}";
            UiFactory.SetPixelBarFill(_xpFill, (float)_ctx.Xp.Xp / Mathf.Max(1, _ctx.Xp.XpToNextLevel()));

            RefreshDescent();
            RefreshNotice();
            RefreshStreak();
            RefreshQuota();
            RefreshForesight();
            RefreshPledges();
            RefreshNudge();
            RefreshStatusLine();
            RefreshSpinButton();

            bool settled = _ctx.Spin.State == SpinState.Idle || _ctx.Spin.State == SpinState.Cooldown;
            _bankButton.interactable = _ctx.Game.RunActive && settled && _ctx.Wallet.RunCoins > 0;
            _titheButton.interactable = _ctx.Game.CanTithe;
        }

        private void RefreshDescent()
        {
            int table = _ctx.Tables.CurrentTable;
            _tableLabel.Text = $"T {TableInviteScreen.Roman(table)}";
            _tableMultiplier.Text = $"X{_ctx.Tables.RewardMultiplier:0.0}";

            for (int i = 0; i < _depthPips.Count; i++)
            {
                string sprite = i + 1 < table ? "Escalation/depth_pip_passed"
                    : (i + 1 == table ? "Escalation/depth_pip_current" : "Escalation/depth_pip_locked");
                _depthPips[i].sprite = ArtSprites.Get(sprite);
            }

        }

        private void RefreshForesight()
        {
            var foreseen = _ctx.Run.ForeseenWedges;
            bool show = foreseen.Count > 0;
            if (_foresightRow.gameObject.activeSelf != show)
                _foresightRow.gameObject.SetActive(show);
            if (!show || _foresightRow.childCount == foreseen.Count) return;

            for (int i = _foresightRow.childCount - 1; i >= 0; i--)
                Object.Destroy(_foresightRow.GetChild(i).gameObject);

            for (int i = 0; i < foreseen.Count; i++)
            {
                var template = _ctx.Ring.Template(foreseen[i]);
                if (template == null) continue;
                var icon = UiFactory.CreateSpriteImage(_foresightRow, $"Foreseen_{i}", null, new Vector2(12f, 12f));
                icon.sprite = ArtSprites.IconForSegment(template);
                UiFactory.Place((RectTransform)icon.transform, new Vector2(0.5f, 0.5f),
                    new Vector2(-14f + i * 14f, 0f), new Vector2(12f, 12f));
            }
        }

        private void RefreshNotice()
        {
            _noticeFill.sprite = ArtSprites.Get(_ctx.Notice.FillSprite);
            // Snap to whole segments: the meter is eight discrete beats of dread.
            float segments = Mathf.Max(1f, _ctx.Notice.Segments);
            _noticeFill.fillAmount = Mathf.Round(_ctx.Notice.Fill * segments) / segments;
            _noticeEye.sprite = ArtSprites.Get($"Loop/notice_eye_{_ctx.Notice.EyeStage}");
        }

        private void RefreshStreak()
        {
            int count = _ctx.Streak.Count;
            bool live = _ctx.Streak.IsLive;

            for (int i = 0; i < _streakPips.Count; i++)
            {
                string sprite = i < count
                    ? (live ? "Loop/streak_pip_hot" : "Loop/streak_pip_full")
                    : "Loop/streak_pip_empty";
                _streakPips[i].sprite = ArtSprites.Get(sprite);
            }

            float mult = _ctx.Streak.Multiplier;
            _multiplierBadge.sprite = ArtSprites.Get(live ? "Loop/multiplier_badge_hot" : "Loop/multiplier_badge");
            _multiplierText.Text = $"X{mult:0.0}";
            _multiplierText.Color = live ? Palette.Gold : Palette.Dim;
        }

        private void RefreshQuota()
        {
            float fill = _ctx.Debt.QuotaFill;
            // Track is 96 wide with a 4px inset each side.
            var rt = (RectTransform)_quotaMarker.transform;
            rt.anchoredPosition = new Vector2(4f + Mathf.Round(fill * 88f), 0f);
            _quotaText.Text = $"{_ctx.Debt.PaidThisRun}/{_ctx.Debt.Quota}";
            _quotaText.Color = _ctx.Debt.QuotaMet ? Palette.Teal : Palette.Dim;
        }

        /// <summary>
        /// The five emblems, rebuilt only when the build actually changes -
        /// this runs every frame and a Pledge is taken once a run at most.
        /// </summary>
        private void RefreshPledges()
        {
            var held = _ctx.Pledges.Held;
            string signature = string.Join(",", held);
            if (signature == _pledgeSignature) return;
            _pledgeSignature = signature;

            for (int i = 0; i < _pledgeEmblems.Count; i++)
            {
                bool filled = i < held.Count;
                _pledgeEmblems[i].gameObject.SetActive(filled);
                if (!filled) continue;

                var cfg = _ctx.Pledges.Get(held[i]);
                _pledgeEmblems[i].sprite = ArtSprites.Get(cfg != null ? cfg.EmblemSprite : null);
            }
        }

        private void RefreshNudge()
        {
            if (!_ctx.Nudge.WindowOpen) return;

            _nudgeLeftIcon.sprite = ArtSprites.Get(_ctx.Nudge.ButtonSprite(true));
            _nudgeRightIcon.sprite = ArtSprites.Get(_ctx.Nudge.ButtonSprite(false));

            bool can = _ctx.Nudge.CanNudge;
            _nudgeLeft.interactable = can;
            _nudgeRight.interactable = can;

            // The bill for the next push, in Notice segments.
            int cost = can ? _ctx.Nudge.NextCost : 0;
            for (int i = 0; i < _nudgeCostPips.Count; i++)
                _nudgeCostPips[i].sprite = ArtSprites.Get(i < cost
                    ? "Pledges/nudge_cost_pip_spent" : "Pledges/nudge_cost_pip_free");

            // The window draining is the whole tension; show it on the bar,
            // snapped to whole pixels of the 48px track.
            float remaining = _ctx.Nudge.WindowLength > 0f
                ? Mathf.Clamp01(_ctx.Nudge.WindowRemaining / _ctx.Nudge.WindowLength) : 0f;
            _nudgeCharge.fillAmount = Mathf.Round(remaining * 48f) / 48f;
        }

        private void RefreshStatusLine()
        {
            // The decision inside the spin gets the slot to itself.
            bool nudging = _ctx.Nudge.WindowOpen;
            if (_statusLine.gameObject.activeSelf == nudging)
                _statusLine.gameObject.SetActive(!nudging);
            if (nudging) return;

            int buffs = 0, debuffs = 0;
            foreach (var e in _ctx.Buffs.Effects)
            {
                if (e.IsDebuff) debuffs++;
                else buffs++;
            }

            string line = "";
            if (buffs > 0) line += $"BLESS X{buffs} ";
            if (debuffs > 0) line += $"HEX X{debuffs} ";
            if (_ctx.Bosses.EncounterActive) line += $"SIN X{_ctx.Bosses.CurrentRewardMultiplier:0.0} ";
            if (_ctx.Tables.IsCroupierSeated) line += _ctx.Tables.SeatLabel;
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
                case SpinState.Nudging:
                    _spinLabel.Text = "TAKE";
                    _spinButton.interactable = true;
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

        // --- The nudge window ---

        /// <summary>The wheel has settled and nothing has been paid yet.</summary>
        public void OnNudgeWindowOpened()
        {
            _nudgeRow.gameObject.SetActive(true);
            Wheel.ShowNudgeGhosts(true);
            RefreshNudge();
        }

        /// <summary>A push landed: the ghosts follow the pointer to its new home.</summary>
        public void OnNudged()
        {
            Wheel.ShowNudgeGhosts(true);
            RefreshNudge();
        }

        public void OnNudgeWindowClosed()
        {
            _nudgeRow.gameObject.SetActive(false);
            Wheel.ShowNudgeGhosts(false);
        }

        // --- Voice (plates + speech), never modal ---

        public void ShowArrivalPlate(string sinId)
        {
            _voicePlate.sprite = ArtSprites.Get("Narrative/plate_" + sinId);
            _voicePlate.gameObject.SetActive(true);
            _voiceComposed.SetActive(false);
            PresentVoice(2.2f);
        }

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
            // Below two terms the assembly is noise, so the float alone says it.
            bool assembled = result.Score != null && result.Score.WorthShowing;
            if (assembled) _scorePanel.Show(result.Score);
            else SpawnFloatingText(result.Text, result.Color, 2, new Vector2(0f, -181f), 1.2f);

            switch (result.Type)
            {
                case SegmentType.Coins:
                case SegmentType.Xp:
                case SegmentType.Humility:
                    // The assembly plays its own chime when the plate lands.
                    if (!assembled) Sfx.Reward();
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

        /// <summary>A chain worth having just died. Make it hurt for a moment.</summary>
        public void ShowStreakBreak()
        {
            var burst = UiFactory.CreateSpriteImage(_frame, "StreakBreak", "Loop/streak_break", new Vector2(32f, 32f));
            UiFactory.Place((RectTransform)burst.transform, Top, new Vector2(22f, -64f), new Vector2(32f, 32f));
            _ctx.CoroutineHost.StartCoroutine(BurstAndFade(burst, 0.4f));
            Haptics.Heavy();
            Sfx.Damage();
        }

        private IEnumerator BurstAndFade(Image img, float lifetime)
        {
            var rt = (RectTransform)img.transform;
            float elapsed = 0f;
            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / lifetime);
                rt.localScale = Vector3.one * (0.7f + 0.8f * t);
                img.color = new Color(1f, 1f, 1f, 1f - t);
                yield return null;
            }
            Object.Destroy(img.gameObject);
        }

        public void Toast(string message, Color color)
        {
            SpawnFloatingText(message, color, 1, new Vector2(0f, -135f), 1.8f);
        }

        public void OnRunStarted()
        {
            _scorePanel.HideImmediate();
            OnNudgeWindowClosed();
            _menuPanel.SetActive(false);
            _runEndPanel.SetActive(false);
            _bossStrip.SetActive(false);
            HideVoice();
        }

        public void OnBossStarted(BossEncounter encounter)
        {
            if (encounter == null) return;

            _bossStrip.SetActive(true);
            var primary = _ctx.Bosses.Primary ?? encounter;
            _bossSigil.sprite = ArtSprites.SigilFor(primary.Config.id);
            _bossBreakGlyph.sprite = ArtSprites.Get($"Loop/break_{primary.Config.id}");
            RefreshBossNames();
            OnBossUpdated(primary);
            ShowArrivalPlate(encounter.Config.id);
            Shake();
        }

        /// <summary>The strip re-reads a new primary without replaying its arrival.</summary>
        public void OnBossRefreshed(BossEncounter encounter)
        {
            if (encounter == null) return;
            _bossSigil.sprite = ArtSprites.SigilFor(encounter.Config.id);
            _bossBreakGlyph.sprite = ArtSprites.Get($"Loop/break_{encounter.Config.id}");
            OnBossUpdated(encounter);
        }

        private void RefreshBossNames()
        {
            var active = _ctx.Bosses.Encounters;
            if (active.Count == 0) return;

            _bossName.Text = active.Count > 1
                ? $"{active[0].Config.displayName}+{active[1].Config.displayName}"
                : active[0].Config.displayName;

            bool stacked = active.Count > 1;
            _bossSigilSecond.gameObject.SetActive(stacked);
            if (stacked) _bossSigilSecond.sprite = ArtSprites.SigilFor(active[1].Config.id);
        }

        public void OnBossUpdated(BossEncounter encounter)
        {
            if (encounter == null) return;
            RefreshBossNames();
            _bossStatus.Text = encounter.Modifier.StatusText(encounter);
            _bossSpinsLeft.Text = $"{encounter.SpinsRemaining} LEFT";
        }

        public void OnBossEnded()
        {
            _bossStrip.SetActive(false);
            _bossSigilSecond.gameObject.SetActive(false);
        }

        public void ShowRunEnd(bool banked, int bankedAmount, int spins, DebtOutcome outcome)
        {
            _runEndPanel.SetActive(true);
            _runEndIntertitle.sprite = ArtSprites.Get(banked ? "Narrative/intertitle_bank" : "Narrative/intertitle_bust");
            _debtSeal.sprite = ArtSprites.Get(DebtSystem.SealSprite(outcome));

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

                var data = _ctx.Save.Data;
                string verdict = data.lastRunMetQuota ? "QUOTA MET - DEBT FALLS" : "QUOTA MISSED - DEBT RISES";
                string marks = "";
                foreach (var mark in _ctx.Game.LastMarksEarned)
                    marks += $"\nMARK {TableInviteScreen.Roman(mark.index)} - {mark.name}";

                _runEndStats.Text =
                    $"PAID {data.lastRunPaid} OF {data.lastRunQuota}" +
                    $"\n{verdict}" +
                    $"\nDEBT {_ctx.Debt.Debt}" +
                    $"\nREACHED TABLE {TableInviteScreen.Roman(_ctx.Tables.CurrentTable)}" +
                    $"\nSPINS {spins} - LV {_ctx.Xp.Level}" + marks;
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
