using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.DatabaseModel;

namespace Xpressive.Home.Services
{
    internal class DevicePersistingService : IDevicePersistingService
    {
        private readonly IContextFactory _contextFactory;

        public DevicePersistingService(IContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

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

            await _contextFactory.InScope(async context =>
            {
                var existing = await context.Device.FindAsync(dto.Id);

                if (existing != null)
                {
                    existing.Properties = dto.Properties;
                    existing.Name = dto.Name;
                }
                else
                {
                    context.Device.Add(dto);
                }

                await context.SaveChangesAsync();
            });
        }

        public async Task DeleteAsync(string gatewayName, DeviceBase device)
        {
            await _contextFactory.InScope(async context =>
            {
                var result = await context.Device.Where(d => d.Gateway == gatewayName && d.Id == device.Id).ToListAsync();
                context.Device.RemoveRange(result);
                await context.SaveChangesAsync();
            });
        }

        public async Task<IEnumerable<DeviceBase>> GetAsync(string gatewayName, Func<string, string, DeviceBase> emptyDevice)
        {
            var devices = new List<DeviceBase>();

            var dtos = await _contextFactory.InScope(async context => await context.Device.Where(d => d.Gateway == gatewayName).ToListAsync());

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
    }
}
