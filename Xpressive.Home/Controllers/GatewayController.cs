using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Controllers
{
    [Route("api/v1/gateway")]
    public class GatewayController : Controller
    {
        private readonly IMessageQueue _messageQueue;
        private readonly IDictionary<string, IGateway> _gateways;

        public GatewayController(IMessageQueue messageQueue, IEnumerable<IGateway> gateways)
        {
            _messageQueue = messageQueue;
            _gateways = gateways.ToDictionary(g => g.Name);
        }

        [HttpGet, Route("")]
        public IEnumerable<GatewayDto> GetGateways()
        {
            return _gateways.Select(g => new GatewayDto
            {
                Name = g.Key,
                CanCreateDevices = g.Value.CanCreateDevices
            });
        }

        [HttpGet, Route("{gatewayName}")]
        public IEnumerable<IDevice> GetDevices(string gatewayName)
        {
            if (_gateways.TryGetValue(gatewayName, out var gateway))
            {
                return gateway.Devices;
            }

            return null;
        }

        [HttpGet, Route("{gatewayName}/empty")]
        public Dictionary<string, object> CreateEmptyDevice(string gatewayName)
        {
            var result = new Dictionary<string, object>();

            if (_gateways.TryGetValue(gatewayName, out var gateway) && gateway.CanCreateDevices)
            {
                var device = gateway.CreateEmptyDevice();
                var properties = GetDeviceProperties(device);

                foreach (var property in properties)
                {
                    var type = property.PropertyType;
                    var value = type.IsValueType ? Activator.CreateInstance(type) : null;
                    result.Add(property.Name, value);
                }
            }

            return result;
        }

        [HttpPost, Route("{gatewayName}")]
        public async Task<IActionResult> CreateDevice(string gatewayName, [FromBody]Dictionary<string, object> dto)
        {
            if (_gateways.TryGetValue(gatewayName, out var gateway) && gateway.CanCreateDevices)
            {
                var device = gateway.CreateEmptyDevice();
                var properties = GetDeviceProperties(device);
                dto = dto.ToDictionary(k => k.Key, k => k.Value, StringComparer.OrdinalIgnoreCase);

                foreach (var property in properties)
                {
                    if (dto.TryGetValue(property.Name, out var value))
                    {
                        var converted = Convert.ChangeType(value, property.PropertyType);
                        property.SetValue(device, converted);
                    }
                }

                var success = await gateway.AddDevice(device);

                if (success)
                {
                    return Ok();
                }
            }

            return BadRequest();
        }

        [HttpDelete, Route("{gatewayName}")]
        public async Task<IActionResult> DeleteDevice(string gatewayName, string deviceId)
        {
            if (_gateways.TryGetValue(gatewayName, out var gateway) && gateway.CanCreateDevices)
            {
                var device = gateway.Devices.SingleOrDefault(d => d.Id.Equals(deviceId, StringComparison.Ordinal));

                if (device == null)
                {
                    return NotFound();
                }

                await gateway.RemoveDevice(device);
                return Ok();
            }

            return BadRequest();
        }

        [HttpPut, Route("{gatewayName}/{deviceId}/{actionName}")]
        public IActionResult ExecuteAction(string gatewayName, string deviceId, string actionName, [FromBody]Dictionary<string, string> parameters)
        {
            if (!_gateways.TryGetValue(gatewayName, out var gateway))
            {
                return NotFound();
            }

            var device = gateway.Devices.SingleOrDefault(d => d.Id.Equals(deviceId, StringComparison.Ordinal));

            if (device == null)
            {
                return NotFound();
            }

            if (!gateway.GetActions(device).Any(a => a.Name.Equals(actionName, StringComparison.Ordinal)))
            {
                return NotFound();
            }

            _messageQueue.Publish(new CommandMessage(gatewayName, deviceId, actionName, parameters));
            return NoContent();
        }

        [HttpGet, Route("{gatewayName}/{deviceId}/actions")]
        public IEnumerable<ActionDto> GetActions(string gatewayName, string deviceId)
        {
            if (!_gateways.TryGetValue(gatewayName, out var gateway))
            {
                return Enumerable.Empty<ActionDto>();
            }

            var device = gateway.Devices.SingleOrDefault(d => d.Id.Equals(deviceId, StringComparison.Ordinal));

            if (device == null)
            {
                return Enumerable.Empty<ActionDto>();
            }

            return gateway.GetActions(device).Select(a => new ActionDto
            {
                Name = a.Name,
                Fields = a.Fields.ToArray()
            });
        }

        private IList<PropertyInfo> GetDeviceProperties(IDevice device)
        {
            var result = new List<PropertyInfo>();

            if (device != null)
            {
                var pairs = new List<Tuple<int, PropertyInfo>>();
                var properties = device.GetType().GetProperties();

                foreach (var property in properties)
                {
                    var attribute = property.GetCustomAttribute<DevicePropertyAttribute>(inherit: true);
                    if (attribute != null)
                    {
                        pairs.Add(Tuple.Create(attribute.SortOrder, property));
                    }
                }

                result.AddRange(pairs.OrderBy(p => p.Item1).Select(p => p.Item2));
            }

            return result;
        }

        public class GatewayDto
        {
            public string Name { get; set; }
            public bool CanCreateDevices { get; set; }
        }

        public class ActionDto
        {
            public string Name { get; set; }
            public string[] Fields { get; set; }
        }
    }
}
