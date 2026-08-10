using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SinWheel
{
    /// <summary>
    /// The five slots, laid out so the whole build is one glance: three across
    /// the top, two beneath. Tapping a slot names what it does and offers the
    /// refund — half in relics, so experimenting is never punished. Cursed
    /// Pledges show no sell button at all. That is the curse.
    /// </summary>
    public sealed class PledgeScreen
    {
        private const int VirtualW = 180;
        private const int VirtualH = 320;

        private static readonly Vector2[] SlotPositions =
        {
            new Vector2(-48f, -76f), new Vector2(0f, -76f), new Vector2(48f, -76f),
            new Vector2(-24f, -142f), new Vector2(24f, -142f)
        };

        private sealed class Slot
        {
            public Image Frame;
            public Image Card;
            public Button Button;
        }

        private readonly GameContext _ctx;
        private readonly Action _onClosed;

        private GameObject _panel;
        private readonly List<Slot> _slots = new List<Slot>();
        private PixelText _title;
        private PixelText _detail;
        private RectTransform _sellRow;
        private Button _sellButton;
        private PixelText _sellLabel;
        private PixelText _sellRefund;
        private int _selected = -1;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public PledgeScreen(GameContext ctx, RectTransform canvasRoot, Action onClosed)
        {
            _ctx = ctx;
            _onClosed = onClosed;
            Build(canvasRoot);
        }

        private void Build(RectTransform canvasRoot)
        {
            var overlay = UiFactory.CreatePanel(canvasRoot, "PledgePanel", Palette.Night);
            UiFactory.Stretch(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _panel = overlay.gameObject;

            var frame = UiFactory.CreateRect(overlay, "Frame");
            UiFactory.Place(frame, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(VirtualW, VirtualH));
            var top = new Vector2(0.5f, 1f);

            var heading = PixelText.Create(frame, "Heading", "PLEDGES", Palette.Gold, 2);
            UiFactory.Place((RectTransform)heading.transform, top, new Vector2(0f, -18f), Vector2.zero);

            var sub = PixelText.Create(frame, "Sub", "WHAT THE HOUSE HOLDS", Palette.Dim);
            UiFactory.Place((RectTransform)sub.transform, top, new Vector2(0f, -32f), Vector2.zero);

            for (int i = 0; i < SlotPositions.Length; i++)
                _slots.Add(BuildSlot(frame, top, i));

            _title = PixelText.Create(frame, "Title", "", Palette.Bone);
            UiFactory.Place((RectTransform)_title.transform, top, new Vector2(0f, -186f), Vector2.zero);

            _detail = PixelText.Create(frame, "Detail", "", Palette.Dim, 1, PxAlign.Center, 160);
            UiFactory.Place((RectTransform)_detail.transform, top, new Vector2(0f, -206f), Vector2.zero);

            _sellRow = UiFactory.CreateRect(frame, "SellRow");
            UiFactory.Place(_sellRow, top, new Vector2(0f, -258f), new Vector2(VirtualW, 16f));

            var sellIcon = UiFactory.CreateSpriteImage(_sellRow, "SellIcon", "Pledges/pledge_sell", new Vector2(16f, 16f));
            UiFactory.Place((RectTransform)sellIcon.transform, new Vector2(0.5f, 0.5f), new Vector2(-40f, 0f), new Vector2(16f, 16f));

            _sellButton = UiFactory.CreatePixelButton(_sellRow, "Sell", "SELL", false, 1, Sell, out _sellLabel);
            UiFactory.Place((RectTransform)_sellButton.transform, new Vector2(0.5f, 0.5f), new Vector2(-8f, 0f), new Vector2(48f, 16f));

            _sellRefund = PixelText.Create(_sellRow, "SellRefund", "", Palette.Gold, 1, PxAlign.Left);
            UiFactory.Place((RectTransform)_sellRefund.transform, new Vector2(0.5f, 0.5f), new Vector2(22f, 0f), Vector2.zero);

            _sellRow.gameObject.SetActive(false);

            var close = UiFactory.CreatePixelButton(frame, "Close", "BACK", true, 1, Close, out _);
            UiFactory.Place((RectTransform)close.transform, top, new Vector2(0f, -292f), new Vector2(64f, 20f));

            _panel.SetActive(false);
        }

        private Slot BuildSlot(RectTransform frame, Vector2 top, int index)
        {
            var slot = new Slot();

            slot.Frame = UiFactory.CreateSpriteImage(frame, $"Slot_{index}", "Pledges/pledge_slot_empty",
                new Vector2(44f, 60f));
            UiFactory.Place((RectTransform)slot.Frame.transform, top, SlotPositions[index], new Vector2(44f, 60f));

            slot.Card = UiFactory.CreateSpriteImage(slot.Frame.transform, "Card", null, new Vector2(40f, 56f));
            UiFactory.Place((RectTransform)slot.Card.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 56f));
            slot.Card.gameObject.SetActive(false);

            slot.Frame.raycastTarget = true; // CreateSpriteImage defaults it off
            slot.Button = slot.Frame.gameObject.AddComponent<Button>();
            slot.Button.targetGraphic = slot.Frame;
            int captured = index;
            slot.Button.onClick.AddListener(() => Select(captured));

            return slot;
        }

        public void Open()
        {
            _selected = _ctx.Pledges.Held.Count > 0 ? 0 : -1;
            Refresh();
            _panel.SetActive(true);
        }

        private void Select(int index)
        {
            _selected = index < _ctx.Pledges.Held.Count ? index : -1;
            Sfx.Tick();
            Refresh();
        }

        private void Refresh()
        {
            var held = _ctx.Pledges.Held;

            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                bool filled = i < held.Count;
                bool locked = i >= _ctx.Pledges.Slots;

                slot.Frame.sprite = ArtSprites.Get(locked ? "Pledges/pledge_slot_locked"
                    : (i == _selected ? "Pledges/pledge_slot_highlight" : "Pledges/pledge_slot_empty"));

                slot.Card.gameObject.SetActive(filled);
                if (filled)
                {
                    var cfg = _ctx.Pledges.Get(held[i]);
                    slot.Card.sprite = ArtSprites.Get(cfg != null ? cfg.CardSprite : null);
                }
                slot.Button.interactable = filled && !locked;
            }

            PledgeConfig selected = _selected >= 0 && _selected < held.Count
                ? _ctx.Pledges.Get(held[_selected]) : null;

            if (selected == null)
            {
                _title.Text = "NOTHING PLEDGED";
                _title.Color = Palette.Dim;
                _detail.Text = "THE FORGE OFFERS THEM. TAKE ONE AND THE HOUSE HOLDS IT.";
                _sellRow.gameObject.SetActive(false);
                return;
            }

            _title.Text = selected.name;
            _title.Color = RarityColor(selected.rarity);
            _detail.Text = selected.description;

            bool sellable = _ctx.Pledges.CanSell(selected);
            _sellRow.gameObject.SetActive(true);
            _sellButton.interactable = sellable;
            _sellLabel.Text = sellable ? "SELL" : "BOUND";
            _sellRefund.Text = sellable ? $"+{_ctx.Pledges.SellValue(selected)}" : "CURSED";
            _sellRefund.Color = sellable ? Palette.Gold : Palette.Blood;
        }

        private void Sell()
        {
            var held = _ctx.Pledges.Held;
            if (_selected < 0 || _selected >= held.Count) return;

            string id = held[_selected];
            if (!_ctx.Pledges.TrySell(id))
            {
                Sfx.Damage();
                return;
            }

            Sfx.Reward();
            _selected = _ctx.Pledges.Held.Count > 0 ? Mathf.Min(_selected, _ctx.Pledges.Held.Count - 1) : -1;
            Refresh();
        }

        private static Color RarityColor(string rarity)
        {
            switch (rarity)
            {
                case "uncommon": return Palette.Teal;
                case "rare": return Palette.Purple;
                case "cursed": return Palette.Blood;
                default: return Palette.Bone;
            }
        }

        private void Close()
        {
            _panel.SetActive(false);
            _onClosed?.Invoke();
        }
    }
}
