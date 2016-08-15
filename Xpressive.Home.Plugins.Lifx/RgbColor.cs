namespace Xpressive.Home.Plugins.Lifx
{
    public sealed class RgbColor
    {
        public int Red { get; set; }
        public int Green { get; set; }
        public int Blue { get; set; }

        public override string ToString()
        {
            return $"#{Red:x2}{Green:x2}{Blue:x2}";
        }
    }
}
