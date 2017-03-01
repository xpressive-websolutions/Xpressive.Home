namespace Xpressive.Home.Plugins.Lifx
{
    public sealed class RgbColor
    {
        public byte Red { get; set; }
        public byte Green { get; set; }
        public byte Blue { get; set; }

        public override string ToString()
        {
            return $"#{Red:x2}{Green:x2}{Blue:x2}";
        }
    }
}
