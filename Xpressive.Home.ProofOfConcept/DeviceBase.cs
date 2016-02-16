using System;

namespace Xpressive.Home.ProofOfConcept
{
    public abstract class DeviceBase : IDevice
    {
        private readonly Guid _guid;
        private readonly string _id;
        private readonly string _name;

        public DeviceBase(string id, string name)
        {
            _guid = Guid.NewGuid();
            _id = id;
            _name = name;

            BatteryStatus = DeviceBatteryStatus.Full;
            WriteStatus = DeviceWriteStatus.Ok;
            ReadStatus = DeviceReadStatus.Ok;
        }

        public string Id => _id;
        public string Name => _name;
        public DeviceBatteryStatus BatteryStatus { get; set; }
        public DeviceWriteStatus WriteStatus { get; set; }
        public DeviceReadStatus ReadStatus { get; set; }

        protected bool Equals(DeviceBase other)
        {
            return _guid.Equals(other._guid);
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
            return _guid.GetHashCode();
        }
    }
}