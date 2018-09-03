using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Tado
{
    internal class TadoDevice : DeviceBase
    {
        public TadoDevice(int homeId, int zoneId)
        {
            Id = homeId + "_" + zoneId;
            HomeId = homeId;
            ZoneId = zoneId;
        }

        public int HomeId { get; }
        public int ZoneId { get; }
    }
}
