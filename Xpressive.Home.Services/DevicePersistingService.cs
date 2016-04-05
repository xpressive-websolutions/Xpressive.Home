using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NPoco;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Services
{
    internal class DevicePersistingService : IDevicePersistingService
    {
        public async Task SaveAsync(string gatewayName, DeviceBase device)
        {
            var properties = GetProperties(device);

            var dto = new DeviceDto
            {
                Gateway = gatewayName,
                Id = $"{gatewayName}.{device.Id}",
                Name = device.Name,
                Properties = JsonConvert.SerializeObject(properties)
            };

            using (var database = new Database("ConnectionString"))
            {
                var result = await database.UpdateAsync("Device", "Id", dto, dto.Id, new[] {"Gateway", "Name", "Properties"});

                if (result == 1)
                {
                    return;
                }

                await database.InsertAsync("Device", "Id", false, dto);
            }
        }

        public async Task<IEnumerable<DeviceBase>> GetAsync(string gatewayName, Func<string, string, DeviceBase> emptyDevice)
        {
            var devices = new List<DeviceBase>();

            using (var database = new Database("ConnectionString"))
            {
                var sql = "select * from Device where Gateway = @0";
                var dtos = await database.FetchAsync<DeviceDto>(sql, gatewayName);

                foreach (var dto in dtos)
                {
                    var device = emptyDevice(dto.Id, dto.Name);
                    var properties = GetProperties(dto.Properties);

                    foreach (var property in GetPropertyInfo(device))
                    {
                        object value;
                        if (properties.TryGetValue(property.Name, out value))
                        {
                            var converted = Convert.ChangeType(value, property.PropertyType);
                            property.SetValue(device, converted);
                        }
                    }

                    device.Id = device.Id.Substring(dto.Gateway.Length + 1);

                    devices.Add(device);
                }
            }

            return devices;
        }

        private Dictionary<string, object> GetProperties(string properties)
        {
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(properties);
        }

        private IEnumerable<PropertyInfo> GetPropertyInfo(IDevice device)
        {
            var properties = device.GetType().GetProperties();

            foreach (var property in properties)
            {
                var attribute = property.GetCustomAttribute<DevicePropertyAttribute>();
                if (attribute == null)
                {
                    continue;
                }

                yield return property;
            }
        }

        private Dictionary<string, object> GetProperties(IDevice device)
        {
            var result = new Dictionary<string, object>();

            foreach (var property in GetPropertyInfo(device))
            {
                result.Add(property.Name, property.GetValue(device));
            }

            result.Remove("Id");
            result.Remove("Name");

            return result;
        }

        public class DeviceDto
        {
            public string Gateway { get; set; }
            public string Id { get; set; }
            public string Name { get; set; }
            public string Properties { get; set; }
        }
    }
}