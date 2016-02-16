using System;
using System.Collections.Generic;

namespace Xpressive.Home.ProofOfConcept
{
    internal class DevicePropertyStore : IDevicePropertyStore
    {
        private readonly List<PersistedDeviceProperty> _storage;
        private readonly object _lock = new object();

        public event EventHandler<DevicePropertyEventArgs> DevicePropertyChanged;

        public DevicePropertyStore()
        {
            _storage = new List<PersistedDeviceProperty>();
        }

        public IDictionary<string, string> Get(string gatewayName, string deviceId)
        {
            lock (_lock)
            {
                var result = new Dictionary<string, string>();

                foreach (var persistedDeviceProperty in _storage)
                {
                    if (persistedDeviceProperty.GatewayName.Equals(gatewayName, StringComparison.Ordinal) &&
                        persistedDeviceProperty.DeviceId.Equals(deviceId, StringComparison.Ordinal))
                    {
                        result[persistedDeviceProperty.Property] = persistedDeviceProperty.Value;
                    }
                }

                return result;
            }
        }

        public string Get(string gatewayName, string deviceId, string property)
        {
            lock (_lock)
            {
                foreach (var persistedDeviceProperty in _storage)
                {
                    if (persistedDeviceProperty.GatewayName.Equals(gatewayName, StringComparison.Ordinal) &&
                        persistedDeviceProperty.DeviceId.Equals(deviceId, StringComparison.Ordinal) &&
                        persistedDeviceProperty.Property.Equals(property, StringComparison.Ordinal))
                    {
                        return persistedDeviceProperty.Value;
                    }
                }
            }

            return null;
        }

        public void Save(string gatewayName, string deviceId, string property, string value)
        {
            lock (_lock)
            {
                foreach (var persistedDeviceProperty in _storage)
                {
                    if (persistedDeviceProperty.GatewayName.Equals(gatewayName, StringComparison.Ordinal) &&
                        persistedDeviceProperty.DeviceId.Equals(deviceId, StringComparison.Ordinal) &&
                        persistedDeviceProperty.Property.Equals(property, StringComparison.Ordinal))
                    {
                        if (!string.Equals(persistedDeviceProperty.Value, value, StringComparison.Ordinal))
                        {
                            persistedDeviceProperty.Value = value;
                            OnDevicePropertyChanged(persistedDeviceProperty);
                        }

                        return;
                    }
                }

                var newPersistedDeviceProperty = new PersistedDeviceProperty
                {
                    GatewayName = gatewayName,
                    DeviceId = deviceId,
                    Property = property,
                    Value = value
                };

                _storage.Add(newPersistedDeviceProperty);
                OnDevicePropertyChanged(newPersistedDeviceProperty);
            }
        }

        private void OnDevicePropertyChanged(PersistedDeviceProperty persistedDeviceProperty)
        {
            OnDevicePropertyChanged(
                persistedDeviceProperty.GatewayName,
                persistedDeviceProperty.DeviceId,
                persistedDeviceProperty.Property,
                persistedDeviceProperty.Value);
        }

        private void OnDevicePropertyChanged(string gatewayName, string deviceId, string property, string value)
        {
            Console.WriteLine("{0}.{1}.{2} = {3}", gatewayName, deviceId, property, value);

            DevicePropertyChanged?.Invoke(this, new DevicePropertyEventArgs(gatewayName, deviceId, property, value));
        }

        private class PersistedDeviceProperty
        {
            public string GatewayName { get; set; }
            public string DeviceId { get; set; }
            public string Property { get; set; }
            public string Value { get; set; }
        }
    }
}