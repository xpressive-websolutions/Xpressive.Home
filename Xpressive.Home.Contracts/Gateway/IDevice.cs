namespace Xpressive.Home.Contracts.Gateway
{
    public interface IDevice
    {
        string Id { get; }
        string Name { get; }

        DeviceBatteryStatus BatteryStatus { get; }
        DeviceWriteStatus WriteStatus { get; }
        DeviceReadStatus ReadStatus { get; }
    }
}