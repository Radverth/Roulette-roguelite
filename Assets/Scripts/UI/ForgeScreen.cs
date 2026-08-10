using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SinWheel
{
    /// <summary>
    /// The Forge: three cards between runs, take one. This is where the wheel
    /// stops being fixed furniture and becomes the build — so the screen shows
    /// what the ring currently is, not just what is on offer.
    /// </summary>
    public sealed class ForgeScreen
    {
        private const int VirtualW = 180;
        private const int VirtualH = 320;

        private readonly GameContext _ctx;
        private readonly Action _onDone;

        private GameObject _panel;
        private RectTransform _cardRow;
        private PixelText _ringSummary;
        private Button _rerollButton;
        private PixelText _rerollLabel;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public ForgeScreen(GameContext ctx, RectTransform canvasRoot, Action onDone)
        {
            _ctx = ctx;
            _onDone = onDone;
            Build(canvasRoot);
        }

        private void Build(RectTransform canvasRoot)
        {
            var overlay = UiFactory.CreatePanel(canvasRoot, "ForgePanel", Palette.Night);
            UiFactory.Stretch(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _panel = overlay.gameObject;

            var frame = UiFactory.CreateRect(overlay, "Frame");
            UiFactory.Place(frame, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(VirtualW, VirtualH));

            var banner = UiFactory.CreateSpriteImage(frame, "Banner", "Loop/forge_banner", new Vector2(112f, 20f));
            UiFactory.Place((RectTransform)banner.transform, new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(112f, 20f));

            _ringSummary = PixelText.Create(frame, "RingSummary", "", Palette.Dim);
            UiFactory.Place((RectTransform)_ringSummary.transform, new Vector2(0.5f, 1f), new Vector2(0f, -46f), Vector2.zero);

            _cardRow = UiFactory.CreateRect(frame, "Cards");
            UiFactory.Place(_cardRow, new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(VirtualW, 110f));

            _rerollButton = UiFactory.CreatePixelButton(frame, "Reroll", "REROLL", false, 1,
                Reroll, out _rerollLabel);
            UiFactory.Place((RectTransform)_rerollButton.transform, new Vector2(0.5f, 0f), new Vector2(-30f, 74f), new Vector2(48f, 16f));

            var rerollIcon = UiFactory.CreateSpriteImage(frame, "RerollIcon", "Loop/action_reroll", new Vector2(16f, 16f));
            UiFactory.Place((RectTransform)rerollIcon.transform, new Vector2(0.5f, 0f), new Vector2(-62f, 74f), new Vector2(16f, 16f));

            var skip = UiFactory.CreatePixelButton(frame, "Skip", "SKIP", false, 1, Skip, out _);
            UiFactory.Place((RectTransform)skip.transform, new Vector2(0.5f, 0f), new Vector2(38f, 74f), new Vector2(48f, 16f));

            var hint = PixelText.Create(frame, "Hint", "TAKE ONE. THE REST BURN.", Palette.Dim);
            UiFactory.Place((RectTransform)hint.transform, new Vector2(0.5f, 0f), new Vector2(0f, 50f), Vector2.zero);

            _panel.SetActive(false);
        }

        public void Open()
        {
            _ctx.Forge.BeginVisit();
            Refresh();
            _panel.SetActive(true);
        }

        private void Refresh()
        {
            for (int i = _cardRow.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_cardRow.GetChild(i).gameObject);

            List<ForgeOffer> offers = _ctx.Forge.Offers;
            float pitch = 58f;
            float x0 = -pitch * (offers.Count - 1) * 0.5f;

            for (int i = 0; i < offers.Count; i++)
                BuildCard(offers[i], new Vector2(x0 + i * pitch, 0f));

            _ringSummary.Text = $"RING {_ctx.Ring.Slots.Count} WEDGES";
            _rerollLabel.Text = _ctx.Forge.RerollUsed ? "USED" : _ctx.Forge.RerollCost.ToString();
            _rerollButton.interactable = _ctx.Forge.CanReroll;
        }

        private void BuildCard(ForgeOffer offer, Vector2 position)
        {
            var card = UiFactory.CreateRect(_cardRow, "Card");
            UiFactory.Place(card, new Vector2(0.5f, 0.5f), position, new Vector2(48f, 68f));

            var img = card.gameObject.AddComponent<Image>();
            img.sprite = ArtSprites.Get(offer.CardSprite);

            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.onClick.AddListener(() => Take(offer));

            // The card art leaves a well in the middle; the target wedge goes there.
            var template = string.IsNullOrEmpty(offer.TemplateId) ? null : _ctx.Ring.Template(offer.TemplateId);
            if (template != null)
            {
                var icon = UiFactory.CreateSpriteImage(card, "Icon", null, new Vector2(16f, 16f));
                icon.sprite = ArtSprites.IconForSegment(template);
                UiFactory.Place((RectTransform)icon.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(16f, 16f));
            }

            if (offer.Action == ForgeAction.Temper && offer.SlotIndex >= 0)
            {
                int tier = Mathf.Clamp(_ctx.Ring.Slots[offer.SlotIndex].tier + offer.TemperSteps, 0, 3);
                var pips = UiFactory.CreateSpriteImage(card, "Pips", $"Loop/tier_pips_{tier}", new Vector2(16f, 6f));
                UiFactory.Place((RectTransform)pips.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(16f, 6f));
            }

            var title = PixelText.Create(card, "Title", offer.Title ?? "", Palette.Bone, 1, PxAlign.Center, 46);
            UiFactory.Place((RectTransform)title.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), Vector2.zero);

            var detail = PixelText.Create(_cardRow, "Detail", offer.Detail ?? "", Palette.Dim, 1, PxAlign.Center, 56);
            UiFactory.Place((RectTransform)detail.transform, new Vector2(0.5f, 0.5f),
                position + new Vector2(0f, -46f), Vector2.zero);
        }

        private void Take(ForgeOffer offer)
        {
            _ctx.Forge.Take(offer);
            Sfx.Reward();
            Close();
        }

        private void Reroll()
        {
            if (_ctx.Forge.TryReroll())
            {
                Sfx.Tick();
                Refresh();
            }
            else
            {
                Sfx.Damage();
            }
        }

        private void Skip()
        {
            _ctx.Analytics.Track("forge_skip", "ring_size", _ctx.Ring.Slots.Count);
            Close();
        }

        private void Close()
        {
            _panel.SetActive(false);
            _onDone?.Invoke();
        }
    }
}
