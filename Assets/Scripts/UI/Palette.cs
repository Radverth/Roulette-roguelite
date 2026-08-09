using UnityEngine;

namespace SinWheel
{
    /// <summary>Gothic-carnival palette: bold flat shapes, strong silhouettes.</summary>
    public static class Palette
    {
        public static readonly Color Night = Hex("#14101A");
        public static readonly Color Panel = Hex("#221A2C");
        public static readonly Color PanelLight = Hex("#332644");
        public static readonly Color Gold = Hex("#D4AF37");
        public static readonly Color Blood = Hex("#A32633");
        public static readonly Color BloodDark = Hex("#5E1420");
        public static readonly Color Purple = Hex("#7B4FA6");
        public static readonly Color Bone = Hex("#E8DCC0");
        public static readonly Color Teal = Hex("#4FA6A0");
        public static readonly Color Sickly = Hex("#7A8C3F");
        public static readonly Color Dim = Hex("#8A8095");

        public static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
        }
    }
}
