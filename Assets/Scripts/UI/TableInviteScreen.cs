using System;
using UnityEngine;
using UnityEngine.UI;

namespace SinWheel
{
    /// <summary>
    /// The fork. Not "continue or play safe" — accept and the stakes multiply,
    /// decline and you are cashing out, which ends the run. It recurs six times
    /// a descent, and it is the shape the run was missing.
    /// </summary>
    public sealed class TableInviteScreen
    {
        private const int VirtualW = 180;
        private const int VirtualH = 320;

        private readonly GameContext _ctx;
        private readonly Action _onAccepted;

        private GameObject _panel;
        private Image _plaque;
        private PixelText _tableName;
        private PixelText _blurb;
        private PixelText _stakes;
        private PixelText _purse;

        public TableInviteScreen(GameContext ctx, RectTransform canvasRoot, Action onAccepted)
        {
            _ctx = ctx;
            _onAccepted = onAccepted;
            Build(canvasRoot);
        }

        private void Build(RectTransform canvasRoot)
        {
            var overlay = UiFactory.CreatePanel(canvasRoot, "TableInvitePanel", Palette.Night);
            UiFactory.Stretch(overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _panel = overlay.gameObject;

            var frame = UiFactory.CreateRect(overlay, "Frame");
            UiFactory.Place(frame, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(VirtualW, VirtualH));

            var banner = UiFactory.CreateSpriteImage(frame, "Invite", "Escalation/table_invite", new Vector2(112f, 44f));
            UiFactory.Place((RectTransform)banner.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 112f), new Vector2(112f, 44f));

            _plaque = UiFactory.CreateSpriteImage(frame, "Plaque", null, new Vector2(44f, 30f));
            UiFactory.Place((RectTransform)_plaque.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 62f), new Vector2(44f, 30f));

            _tableName = PixelText.Create(frame, "TableName", "", Palette.Gold);
            UiFactory.Place((RectTransform)_tableName.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), Vector2.zero);

            _blurb = PixelText.Create(frame, "Blurb", "", Palette.Blood, 1, PxAlign.Center, 160);
            UiFactory.Place((RectTransform)_blurb.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 24f), Vector2.zero);

            _stakes = PixelText.Create(frame, "Stakes", "", Palette.Bone);
            UiFactory.Place((RectTransform)_stakes.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), Vector2.zero);

            _purse = PixelText.Create(frame, "Purse", "", Palette.Dim);
            UiFactory.Place((RectTransform)_purse.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), Vector2.zero);

            var deeper = UiFactory.CreatePixelButton(frame, "Deeper", "GO DEEPER", true, 2, Accept, out _);
            UiFactory.Place((RectTransform)deeper.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -44f), new Vector2(96f, 32f));

            var cashOut = UiFactory.CreatePixelButton(frame, "CashOut", "CASH OUT", false, 2, Decline, out _);
            UiFactory.Place((RectTransform)cashOut.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -84f), new Vector2(96f, 32f));

            var warning = PixelText.Create(frame, "Warning", "CASHING OUT ENDS THE RUN", Palette.Dim);
            UiFactory.Place((RectTransform)warning.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -108f), Vector2.zero);

            _panel.SetActive(false);
        }

        public void Open()
        {
            var next = _ctx.Tables.Config(_ctx.Tables.CurrentTable + 1);
            _plaque.sprite = ArtSprites.Get($"Escalation/table_plaque_{Mathf.Clamp(next.index, 1, 7)}");
            _tableName.Text = $"TABLE {Roman(next.index)} - {next.name}";
            _blurb.Text = next.blurb ?? "";
            _stakes.Text = $"REWARDS X{next.rewardMultiplier:0.0}";
            _purse.Text = $"PURSE {_ctx.Wallet.RunCoins}";

            _panel.SetActive(true);
            Sfx.Boss();
        }

        private void Accept()
        {
            _panel.SetActive(false);
            _ctx.Tables.Accept();
            _onAccepted?.Invoke();
        }

        private void Decline()
        {
            _panel.SetActive(false);
            _ctx.Tables.Decline();
        }

        public static string Roman(int value)
        {
            switch (value)
            {
                case 1: return "I";
                case 2: return "II";
                case 3: return "III";
                case 4: return "IV";
                case 5: return "V";
                case 6: return "VI";
                case 7: return "VII";
                default: return value.ToString();
            }
        }
    }
}
