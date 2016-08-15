using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Lifx
{
    public class LifxDevice : DeviceBase
    {
        public LifxDevice() { }

        internal LifxDevice(Light light)
        {
            Id = light.Id;
            Name = light.Label;
        }
    }
}
