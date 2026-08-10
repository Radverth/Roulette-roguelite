using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SinWheel
{
    /// <summary>
    /// Hosts the mini-games: offers two cards, runs the chosen one against its
    /// clock, then shows the outcome banner. Skip is always on screen — a forced
    /// mini-game is a chore by session twenty.
    /// </summary>
    public sealed class InterludeScreen
    {
        private const int VirtualW = 180;
        private const int VirtualH = 320;

        private readonly GameContext _ctx;
        private readonly Action _onDone;

        private GameObject _panel;
        private RectTransform _offerRow;
        private PixelText _header;

        private GameObject _stage;
        private RectTransform _stageArea;
        private PixelText _instruction;
        private Image _timerFill;
        private PointerSurface _tapSurface;

        private GameObject _resultPanel;
        private Image _resultBanner;

        private InterludeConfig _playing;
        private InterludeGame _game;
        private float _timeLeft;
        private float _timeTotal;
        private bool _sideTable;
        private Coroutine _resultRoutine;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public InterludeScreen(GameContext ctx, RectTransform canvasRoot, Action onDone)
        {
            _ctx = ctx;
            _onDone = onDone;
            Build(canvasRoot);
        }

        private void Build(RectTransform canvasRoot)
        {
            var overlay = UiFactory.CreatePanel(canvasRoot, "InterludePanel", Palette.Night);
            UiFactory.Stretch(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _panel = overlay.gameObject;

            var frame = UiFactory.CreateRect(overlay, "Frame");
            UiFactory.Place(frame, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(VirtualW, VirtualH));

            _header = PixelText.Create(frame, "Header", "A SIDE TABLE", Palette.Gold);
            UiFactory.Place((RectTransform)_header.transform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), Vector2.zero);

            _offerRow = UiFactory.CreateRect(frame, "Offers");
            UiFactory.Place(_offerRow, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(VirtualW, 120f));

            var skip = UiFactory.CreatePixelButton(frame, "Skip", "SKIP", false, 1, SkipOffer, out _,
                "Escalation/button_skip");
            UiFactory.Place((RectTransform)skip.transform, new Vector2(0.5f, 0f), new Vector2(0f, 72f), new Vector2(40f, 12f));

            var skipNote = PixelText.Create(frame, "SkipNote", "", Palette.Dim);
            UiFactory.Place((RectTransform)skipNote.transform, new Vector2(0.5f, 0f), new Vector2(0f, 56f), Vector2.zero);
            _skipNote = skipNote;

            BuildStage(frame);
            BuildResult(frame);

            _panel.SetActive(false);
        }

        private PixelText _skipNote;

        private void BuildStage(RectTransform frame)
        {
            var stage = UiFactory.CreateRect(frame, "Stage");
            UiFactory.Stretch(stage, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _stage = stage.gameObject;

            // A full-screen press target sits behind the game's own widgets, so
            // the timing and rhythm games can be played by tapping anywhere.
            var surface = UiFactory.CreateRect(stage, "TapSurface");
            UiFactory.Stretch(surface, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var surfaceImg = surface.gameObject.AddComponent<Image>();
            surfaceImg.color = new Color(0f, 0f, 0f, 0f);
            _tapSurface = surface.gameObject.AddComponent<PointerSurface>();
            _tapSurface.Pressed += () => _game?.OnPressed();
            _tapSurface.Released += () => _game?.OnReleased();

            _instruction = PixelText.Create(stage, "Instruction", "", Palette.Bone, 2);
            UiFactory.Place((RectTransform)_instruction.transform, new Vector2(0.5f, 1f), new Vector2(0f, -46f), Vector2.zero);

            var track = UiFactory.CreateSpriteImage(stage, "TimerTrack", "UI/bar_track", new Vector2(64f, 8f));
            UiFactory.Place((RectTransform)track.transform, new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(64f, 8f));

            var fill = UiFactory.CreateRect(track.transform, "Fill");
            UiFactory.Stretch(fill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _timerFill = fill.gameObject.AddComponent<Image>();
            _timerFill.sprite = ArtSprites.Get("UI/bar_fill_resist");
            _timerFill.type = Image.Type.Filled;
            _timerFill.fillMethod = Image.FillMethod.Horizontal;
            _timerFill.raycastTarget = false;

            // Where each game builds itself.
            _stageArea = UiFactory.CreateRect(stage, "Area");
            UiFactory.Place(_stageArea, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(160f, 120f));

            _stage.SetActive(false);
        }

        private void BuildResult(RectTransform frame)
        {
            var result = UiFactory.CreateRect(frame, "Result");
            UiFactory.Stretch(result, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _resultPanel = result.gameObject;

            var bg = result.gameObject.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.01f, 0.04f, 0.92f);

            _resultBanner = UiFactory.CreateSpriteImage(result, "Banner", null, new Vector2(96f, 24f));
            UiFactory.Place((RectTransform)_resultBanner.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96f, 24f));

            _resultPanel.SetActive(false);
        }

        /// <summary>Open at a table transition, or mid-run when the eye is wide.</summary>
        public void Open(bool sideTable)
        {
            _sideTable = sideTable;
            _header.Text = sideTable ? "A SIDE TABLE" : "THE HOUSE OFFERS";
            _skipNote.Text = $"WALK PAST FOR {_ctx.Interludes.SkipReward}";

            var offers = _ctx.Interludes.Offer();
            if (offers.Count == 0)
            {
                Close();
                return;
            }

            for (int i = _offerRow.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_offerRow.GetChild(i).gameObject);

            float pitch = 66f;
            float x0 = -pitch * (offers.Count - 1) * 0.5f;
            for (int i = 0; i < offers.Count; i++)
                BuildOfferCard(offers[i], new Vector2(x0 + i * pitch, 0f));

            _offerRow.gameObject.SetActive(true);
            _stage.SetActive(false);
            _resultPanel.SetActive(false);
            _panel.SetActive(true);
        }

        private void BuildOfferCard(InterludeConfig cfg, Vector2 position)
        {
            var card = UiFactory.CreateRect(_offerRow, "Offer");
            UiFactory.Place(card, new Vector2(0.5f, 0.5f), position, new Vector2(56f, 76f));

            var img = card.gameObject.AddComponent<Image>();
            img.sprite = ArtSprites.Get(cfg.CardSprite);

            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.onClick.AddListener(() => Play(cfg));

            var emblem = UiFactory.CreateSpriteImage(card, "Emblem", cfg.EmblemSprite, new Vector2(24f, 24f));
            UiFactory.Place((RectTransform)emblem.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(24f, 24f));

            var verb = PixelText.Create(card, "Verb", cfg.verb ?? "", Palette.Bone, 1, PxAlign.Center, 52);
            UiFactory.Place((RectTransform)verb.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), Vector2.zero);
        }

        private void Play(InterludeConfig cfg)
        {
            _playing = cfg;
            _game = InterludeGameFactory.Create(cfg.id);
            if (_game == null)
            {
                Close();
                return;
            }

            for (int i = _stageArea.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_stageArea.GetChild(i).gameObject);

            _offerRow.gameObject.SetActive(false);
            _stage.SetActive(true);

            _instruction.Text = _game.Instruction;
            _timeTotal = Mathf.Max(1f, cfg.seconds);
            _timeLeft = _timeTotal;
            _timerFill.fillAmount = 1f;

            // Interludes ramp with the descent, or they are a free reward by Table V.
            float difficulty = Mathf.Clamp01((_ctx.Tables.CurrentTable - 1) / 6f);
            _game.Start(_ctx, _stageArea, difficulty, OnGameFinished);
        }

        public void Tick(float dt)
        {
            if (!IsOpen || _game == null || !_stage.activeSelf) return;

            _timeLeft -= dt;
            _timerFill.fillAmount = Mathf.Clamp01(_timeLeft / _timeTotal);
            _game.Tick(dt);

            if (_timeLeft <= 0f)
            {
                var finishing = _game;
                _game = null;
                finishing.OnTimeout();
            }
        }

        private void OnGameFinished(InterludeResult result, float score)
        {
            _game = null;
            _ctx.Interludes.Resolve(_playing, result, score, _sideTable);

            _resultBanner.sprite = ArtSprites.Get(result == InterludeResult.Success
                ? "Escalation/result_success"
                : (result == InterludeResult.Partial ? "Escalation/result_partial" : "Escalation/result_fail"));
            _resultPanel.SetActive(true);

            if (_resultRoutine != null) _ctx.CoroutineHost.StopCoroutine(_resultRoutine);
            _resultRoutine = _ctx.CoroutineHost.StartCoroutine(CloseAfterResult());
        }

        private IEnumerator CloseAfterResult()
        {
            yield return new WaitForSeconds(1.3f);
            _resultRoutine = null;
            Close();
        }

        private void SkipOffer()
        {
            _ctx.Interludes.Skip();
            Close();
        }

        private void Close()
        {
            _game = null;
            _playing = null;
            _stage.SetActive(false);
            _resultPanel.SetActive(false);
            _panel.SetActive(false);
            _onDone?.Invoke();
        }
    }
}
