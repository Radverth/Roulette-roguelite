using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SinWheel
{
    [Serializable]
    public class TutorialPage
    {
        public string title;
        public string sprite;
        public float spriteSize = 32f;
        public string body;
    }

    [Serializable]
    public class TutorialConfig
    {
        public List<TutorialPage> pages = new List<TutorialPage>();
    }

    /// <summary>
    /// How to play, in nine plates. Shown once on a first run and available
    /// from the menu after that. Copy lives in Resources/Config/tutorial.json
    /// so it can be reworded without a rebuild — same rule as the balance data.
    /// </summary>
    public sealed class TutorialScreen
    {
        private const int VirtualW = 180;
        private const int VirtualH = 320;

        private readonly GameContext _ctx;
        private readonly Action _onDone;
        private readonly TutorialConfig _config;

        private GameObject _panel;
        private PixelText _title;
        private PixelText _body;
        private PixelText _counter;
        private Image _art;
        private Button _backButton;
        private PixelText _nextLabel;
        private int _index;

        public TutorialScreen(GameContext ctx, RectTransform canvasRoot, Action onDone)
        {
            _ctx = ctx;
            _onDone = onDone;

            var asset = Resources.Load<TextAsset>("Config/tutorial");
            if (asset == null)
            {
                Debug.LogError("[Tutorial] Missing Resources/Config/tutorial.json");
                _config = new TutorialConfig();
            }
            else
            {
                _config = JsonUtility.FromJson<TutorialConfig>(asset.text) ?? new TutorialConfig();
            }

            Build(canvasRoot);
        }

        private void Build(RectTransform canvasRoot)
        {
            var overlay = UiFactory.CreatePanel(canvasRoot, "TutorialPanel", Palette.Night);
            UiFactory.Stretch(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _panel = overlay.gameObject;

            var frame = UiFactory.CreateRect(overlay, "Frame");
            UiFactory.Place(frame, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(VirtualW, VirtualH));

            var header = PixelText.Create(frame, "Header", "HOW TO PLAY", Palette.Dim);
            UiFactory.Place((RectTransform)header.transform, new Vector2(0.5f, 1f), new Vector2(0f, -18f), Vector2.zero);

            var plate = UiFactory.CreateNineSlice(frame, "Plate", "UI/panel");
            UiFactory.Place((RectTransform)plate.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 24f), new Vector2(164f, 168f));

            _title = PixelText.Create(plate.transform, "Title", "", Palette.Gold, 2);
            UiFactory.Place((RectTransform)_title.transform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), Vector2.zero);

            var divider = UiFactory.CreateSpriteImage(plate.transform, "Divider", "Narrative/ornament_divider", new Vector2(32f, 8f));
            UiFactory.Place((RectTransform)divider.transform, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(32f, 8f));

            _art = UiFactory.CreateSpriteImage(plate.transform, "Art", null, new Vector2(32f, 32f));
            UiFactory.Place((RectTransform)_art.transform, new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(32f, 32f));

            _body = PixelText.Create(plate.transform, "Body", "", Palette.Bone, 1, PxAlign.Center, 148);
            UiFactory.Place((RectTransform)_body.transform, new Vector2(0.5f, 0f), new Vector2(0f, 36f), Vector2.zero);

            _counter = PixelText.Create(frame, "Counter", "", Palette.Dim);
            UiFactory.Place((RectTransform)_counter.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), Vector2.zero);

            _backButton = UiFactory.CreatePixelButton(frame, "Back", "BACK", false, 1, Back, out _);
            UiFactory.Place((RectTransform)_backButton.transform, new Vector2(0.5f, 0f), new Vector2(-56f, 66f), new Vector2(48f, 16f));

            var next = UiFactory.CreatePixelButton(frame, "Next", "NEXT", true, 1, Next, out _nextLabel);
            UiFactory.Place((RectTransform)next.transform, new Vector2(0.5f, 0f), new Vector2(30f, 66f), new Vector2(48f, 16f));

            var skip = UiFactory.CreatePixelButton(frame, "Skip", "SKIP", false, 1, Close, out _);
            UiFactory.Place((RectTransform)skip.transform, new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(48f, 16f));

            _panel.SetActive(false);
        }

        public void Open()
        {
            if (_config.pages.Count == 0)
            {
                Close();
                return;
            }
            _index = 0;
            Refresh();
            _panel.SetActive(true);
        }

        private void Refresh()
        {
            var page = _config.pages[_index];
            _title.Text = page.title ?? "";
            _body.Text = page.body ?? "";
            _counter.Text = $"{_index + 1} OF {_config.pages.Count}";

            float size = page.spriteSize <= 0f ? 32f : page.spriteSize;
            var rt = (RectTransform)_art.transform;
            rt.sizeDelta = new Vector2(size, size);
            _art.sprite = string.IsNullOrEmpty(page.sprite) ? null : ArtSprites.Get(page.sprite);
            _art.enabled = _art.sprite != null;

            _backButton.interactable = _index > 0;
            _nextLabel.Text = _index == _config.pages.Count - 1 ? "BEGIN" : "NEXT";
        }

        private void Next()
        {
            Sfx.Tick();
            if (_index >= _config.pages.Count - 1)
            {
                Close();
                return;
            }
            _index++;
            Refresh();
        }

        private void Back()
        {
            if (_index <= 0) return;
            Sfx.Tick();
            _index--;
            Refresh();
        }

        private void Close()
        {
            _panel.SetActive(false);
            if (!_ctx.Save.Data.tutorialSeen)
            {
                _ctx.Save.Data.tutorialSeen = true;
                _ctx.Save.Persist();
            }
            _onDone?.Invoke();
        }
    }
}
