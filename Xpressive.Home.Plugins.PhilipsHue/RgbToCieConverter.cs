using System;
using System.Collections.Generic;
using System.Linq;

namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal static class RgbToCieConverter
    {
        private static readonly Dictionary<string, Gamut> _gamutAssignment;

        static RgbToCieConverter()
        {
            var gamutA = new Gamut(new Point(0.704, 0.296), new Point(0.2151, 0.7106), new Point(0.138, 0.080));
            var gamutB = new Gamut(new Point(0.675, 0.322), new Point(0.4090, 0.5180), new Point(0.167, 0.040));
            var gamutC = new Gamut(new Point(0.692, 0.308), new Point(0.1700, 0.7000), new Point(0.153, 0.048));

            //http://www.developers.meethue.com/documentation/supported-lights
            _gamutAssignment = new Dictionary<string, Gamut>(StringComparer.OrdinalIgnoreCase)
            {
                {"LCT001", gamutB},
                {"LCT007", gamutB},
                {"LCT010", gamutC},
                {"LCT014", gamutC},
                {"LCT002", gamutB},
                {"LCT003", gamutB},
                {"LCT011", gamutC},
                {"LST001", gamutA},
                {"LLC010", gamutA},
                {"LLC011", gamutA},
                {"LLC012", gamutA},
                {"LLC006", gamutA},
                {"LLC007", gamutA},
                {"LLC013", gamutA},
                {"LLM001", gamutB},
                {"LLC020", gamutC},
                {"LST002", gamutC}
            };
        }

        public static CieResult Convert(string bulbType, double red, double green, double blue)
        {
            var gamut = GetGamut(bulbType);

            if (gamut == null)
            {
                return default(CieResult);
            }

            var cie = RgbToCie(red, green, blue);
            var point = new Point(cie.X, cie.Y);

            var nearestTriangleSide = GetLineSegments(gamut).OrderBy(s => GetDistance(GetMidpoint(s), point)).First();
            var centroid = GetCentroid(gamut);
            var lineSegment = new LineSegment(point, centroid);
            var intersection = GetIntersection(nearestTriangleSide, lineSegment);

            if (GetDistance(centroid, point) < GetDistance(centroid, intersection))
            {
                return cie;
            }

            return new CieResult(intersection.X, intersection.Y, cie.Brightness);
        }

        private static Point GetIntersection(LineSegment line1, LineSegment line2)
        {
            Func<Point, Point, double> getM = (p, q) => (q.Y - p.Y)/(q.X - p.X);
            Func<Point, double, double> getQ = (p, m) => p.Y/(m*p.X);

            var m1 = getM(line1.P1, line1.P2);
            var q1 = getQ(line1.P1, m1);
            var m2 = getM(line2.P1, line2.P2);
            var q2 = getQ(line2.P1, m2);
            var x = (q2 - q1)/(m1 - m2);
            var y = m1*x + q1;
            return new Point(x, y);
        }

        private static double GetDistance(Point p, Point q)
        {
            var a = Math.Abs(p.Y - q.Y);
            var b = Math.Abs(p.X - q.X);

            if (p.X == q.X)
            {
                return a;
            }
            if (p.Y == q.Y)
            {
                return b;
            }

            return Math.Sqrt(a*a + b*b);
        }

        private static IEnumerable<LineSegment> GetLineSegments(Gamut gamut)
        {
            yield return new LineSegment(gamut.Red, gamut.Green);
            yield return new LineSegment(gamut.Red, gamut.Blue);
            yield return new LineSegment(gamut.Blue, gamut.Green);
        }

        private static Point GetCentroid(Gamut gamut)
        {
            var x = (gamut.Red.X + gamut.Green.X + gamut.Blue.X)/3d;
            var y = (gamut.Red.Y + gamut.Green.Y + gamut.Blue.Y)/3d;
            return new Point(x, y);
        }

        private static Point GetMidpoint(LineSegment lineSegment)
        {
            var x = (lineSegment.P1.X + lineSegment.P2.X)/2d;
            var y = (lineSegment.P1.Y + lineSegment.P2.Y)/2d;
            return new Point(x, y);
        }

        private static CieResult RgbToCie(double red, double green, double blue)
        {
            red = red > 0.04045 ? Math.Pow((red + 0.055) / (1.0 + 0.055), 2.4) : red / 12.92;
            green = green > 0.04045 ? Math.Pow((green + 0.055) / (1.0 + 0.055), 2.4) : green / 12.92;
            blue = blue > 0.04045 ? Math.Pow((blue + 0.055) / (1.0 + 0.055), 2.4) : blue / 12.92;

            var X = red * 0.664511 + green * 0.154324 + blue * 0.162028;
            var Y = red * 0.283881 + green * 0.668433 + blue * 0.047685;
            var Z = red * 0.000088 + green * 0.072310 + blue * 0.986039;

            var divisor = (X + Y + Z);
            var x = divisor == 0 ? 0 : X / divisor;
            var y = divisor == 0 ? 0 : Y / divisor;

            return new CieResult(x, y, Y);
        }

        private static Gamut GetGamut(string bulbType)
        {
            Gamut gamut;
            if (!_gamutAssignment.TryGetValue(bulbType, out gamut))
            {
                return null;
            }
            return gamut;
        }

        private class Gamut
        {
            public Gamut(Point red, Point green, Point blue)
            {
                Red = red;
                Green = green;
                Blue = blue;
            }

            public Point Red { get; }
            public Point Green { get; }
            public Point Blue { get; }
        }

        private struct LineSegment
        {
            public LineSegment(Point p1, Point p2)
            {
                P1 = p1;
                P2 = p2;
            }

            public Point P1 { get; }
            public Point P2 { get; }
        }

        private struct Point
        {
            public Point(double x, double y)
            {
                X = x;
                Y = y;
            }

            public double X { get; }
            public double Y { get; }
        }

        public struct CieResult
        {
            public CieResult(double x, double y, double brightness)
            {
                X = x;
                Y = y;
                Brightness = brightness;
            }

            public double X { get; }
            public double Y { get; }
            public double Brightness { get; }
        }
    }
}
