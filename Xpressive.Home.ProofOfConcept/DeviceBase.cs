using Xpressive.Home.ProofOfConcept.Contracts;

namespace Xpressive.Home.ProofOfConcept
{
    public abstract class DeviceBase : IDevice
    {
        public DeviceBase()
        {
            BatteryStatus = DeviceBatteryStatus.Full;
            WriteStatus = DeviceWriteStatus.Ok;
            ReadStatus = DeviceReadStatus.Ok;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public DeviceBatteryStatus BatteryStatus { get; set; }
        public DeviceWriteStatus WriteStatus { get; set; }
        public DeviceReadStatus ReadStatus { get; set; }

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