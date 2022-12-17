namespace Xpressive.Home.Contracts.Gateway
{
    public abstract class DeviceBase : IDevice
    {
        protected DeviceBase()
        {
            BatteryStatus = DeviceBatteryStatus.Full;
        }

        [DeviceProperty(1)]
        public string Id { get; set; }

        [DeviceProperty(2)]
        public string Name { get; set; }

        public string Icon { get; set; }
        public DeviceBatteryStatus BatteryStatus { get; set; }

        public virtual bool IsConfigurationValid()
        {
            return !string.IsNullOrEmpty(Id) && !string.IsNullOrEmpty(Name);
        }

        protected bool Equals(DeviceBase other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((DeviceBase)obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
