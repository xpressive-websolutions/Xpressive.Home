namespace Xpressive.Home.Contracts.Messaging
{
    public sealed class RenameDeviceMessage : IMessageQueueMessage
    {
        public RenameDeviceMessage(string gateway, string device, string name)
        {
            Gateway = gateway;
            Device = device;
            Name = name;
        }

        public string Gateway { get; }
        public string Device { get; }
        public string Name { get; }
    }
}
