using System;
using System.Globalization;

namespace Xpressive.Home.Plugins.Lifx
{
    internal static class ColorExtensions
    {
        public static RgbColor ToRgb(this HsbkColor hsb)
        {
            double r, g, b;
            var h = hsb.Hue / 360d;

            if (Math.Abs(hsb.Saturation) < 0.001)
            {
                r = g = b = hsb.Brightness * 255d; // achromatic
            }
            else
            {
                var q = hsb.Brightness < 0.5
                    ? hsb.Brightness * (1 + hsb.Saturation)
                    : hsb.Brightness + hsb.Saturation - hsb.Brightness * hsb.Saturation;
                var p = 2 * hsb.Brightness - q;
                r = HueToRgb(p, q, h + 1d / 3d) * 255d;
                g = HueToRgb(p, q, h) * 255d;
                b = HueToRgb(p, q, h - 1d / 3d) * 255d;
            }

            return new RgbColor
            {
                Red = (int)r,
                Green = (int)g,
                Blue = (int)b
            };
        }

        public static RgbColor ParseRgb(this string hexColor)
        {
            if (string.IsNullOrEmpty(hexColor))
            {
                throw new ArgumentNullException(nameof(hexColor));
            }

            hexColor = hexColor.Replace("#", string.Empty);

            if (hexColor.Length != 6)
            {
                throw new ArgumentException("Color should contains 6 characters");
            }

            var red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
            var green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
            var blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);

            return new RgbColor
            {
                Red = red,
                Green = green,
                Blue = blue
            };
        }

        public static HsbkColor ToHsbk(this RgbColor rgb)
        {
            var r = rgb.Red / 255d;
            var g = rgb.Green / 255d;
            var b = rgb.Blue / 255d;
            var mx = Math.Max(r, Math.Max(g, b));
            var mn = Math.Min(r, Math.Min(g, b));
            var c = mx - mn;
            var hs = 0d;

            if (Math.Abs(c) < 0.001)
            {
                hs = 0;
            }
            else if (Math.Abs(mx - r) < 0.001)
            {
                hs = (g - b) / c;
                if (hs < 0)
                {
                    hs += 6;
                }
            }
            else if (Math.Abs(mx - g) < 0.001)
            {
                hs = 2 + (b - r) / c;
            }
            else
            {
                hs = 4 + (r - g) / c;
            }

            var h = 60 * hs;
            var v = mx;
            var s = Math.Abs(c) < 0.001 ? 0 : c / v;

            return new HsbkColor
            {
                Hue = h,
                Saturation = s,
                Brightness = v,
                Kelvin = 4500
            };
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0d) { t += 1d; }
            if (t > 1d) { t -= 1d; }
            if (t < 1d / 6d) { return p + (q - p) * 6d * t; }
            if (t < 1d / 2d) { return q; }
            if (t < 2d / 3d) { return p + (q - p) * (2d / 3d - t) * 6d; }
            return p;
        }
    }
}
