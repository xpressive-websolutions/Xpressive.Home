using System;
using System.Collections.Generic;

namespace Xpressive.Home.ProofOfConcept
{
    public interface IDevicePropertyStore
    {
        event EventHandler<DevicePropertyEventArgs> DevicePropertyChanged;

        void Save(string gatewayName, string deviceId, string property, string value);

        string Get(string gatewayName, string deviceId, string property);

        IDictionary<string, string> Get(string gatewayName, string deviceId);
    }
}