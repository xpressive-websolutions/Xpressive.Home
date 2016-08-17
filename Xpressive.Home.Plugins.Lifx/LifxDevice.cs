using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Lifx
{
    public class LifxDevice : DeviceBase
    {
        public LifxDevice() { }

        internal LifxDevice(LifxHttpLight light)
        {
            Id = light.Id;
            Name = light.Label;
            Source = LifxSource.Cloud;
        }

        internal LifxDevice(LifxLocalLight light)
        {
            Id = light.Id;
            Source = LifxSource.Lan;
        }

        public LifxSource Source { get; set; }
    }

    public enum LifxSource
    {
        Lan,
        Cloud
    }
}
