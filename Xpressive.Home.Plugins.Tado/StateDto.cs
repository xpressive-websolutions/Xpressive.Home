namespace Xpressive.Home.Plugins.Tado
{
    internal class StateDto
    {
        public TadoMode TadoMode { get; set; }
        public SensorDataDto SensorDataPoints { get; set; }
        public SettingDto Setting { get; set; }
    }
}
