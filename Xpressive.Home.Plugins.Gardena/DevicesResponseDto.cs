using System;
using System.Collections.Generic;

namespace Xpressive.Home.Plugins.Gardena
{
    public class DevicesResponseDto
    {
        public List<DevicesResponseDtoDevice> Devices { get; set; }
    }

    public class DevicesResponseDtoDevice
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public List<DevicesResponseDtoDeviceAbility> Abilities { get; set; }
        public string DeviceState { get; set; }
    }

    public class DevicesResponseDtoDeviceAbility
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<DevicesResponseDtoDeviceAbilityProperty> Properties { get; set; }
        public string Type { get; set; }
    }

    public class DevicesResponseDtoDeviceAbilityProperty
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }
        public bool Writeable { get; set; }
        public List<string> SupportedValues { get; set; }
        public DateTime Timestamp { get; set; }
        public string Unit { get; set; }
    }
}
