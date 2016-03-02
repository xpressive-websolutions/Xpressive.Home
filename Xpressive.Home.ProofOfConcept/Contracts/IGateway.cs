using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xpressive.Home.ProofOfConcept
{
    public interface IGateway
    {
        event EventHandler<DevicePropertyEventArgs> DevicePropertyChanged;

        string Name { get; }
        IEnumerable<IDevice> Devices { get; }
        IEnumerable<IAction> Actions { get; }
        IEnumerable<IProperty> Properties { get; }

        Task<string> Get(IDevice device, string property);
        Task Set(IDevice device, string property, string value);
        Task Execute(IDeviceAction action);
    }
}