using System;
using System.Globalization;
using LifxHttp;

namespace Xpressive.Home.Plugins.Lifx
{
    internal static class ColorExtensions
    {
        public static LifxColor.RGB ToRgb(this LifxColor.HSBK hsb)
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

            return new LifxColor.RGB((int)r, (int)g, (int)b);
        }

        public static LifxColor.RGB ParseRgb(this string hexColor)
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
            return new LifxColor.RGB(red, green, blue);
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
