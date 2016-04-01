using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.WebApi.Controllers
{
    [RoutePrefix("api/v1/gateway")]
    public class GatewayController : ApiController
    {
        private readonly IDictionary<string, IGateway> _gateways;

        public GatewayController(IEnumerable<IGateway> gateways)
        {
            _gateways = gateways.ToDictionary(g => g.Name);
        }

        [HttpGet, Route("")]
        public IEnumerable<string> GetGatewayNames()
        {
            return _gateways.Keys;
        }

        [HttpGet, Route("{gatewayName}")]
        public IEnumerable<IDevice> GetDevices(string gatewayName)
        {
            IGateway gateway;
            if (_gateways.TryGetValue(gatewayName, out gateway))
            {
                return gateway.Devices;
            }

            return null;
        }

        [HttpGet, Route("{gatewayName}/empty")]
        public Dictionary<string, object> CreateEmptyDevice(string gatewayName)
        {
            var result = new Dictionary<string, object>();

            IGateway gateway;
            if (_gateways.TryGetValue(gatewayName, out gateway) && gateway.CanCreateDevices)
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
        public IHttpActionResult CreateDevice(string gatewayName, [FromBody]Dictionary<string, object> dto)
        {
            IGateway gateway;
            if (_gateways.TryGetValue(gatewayName, out gateway) && gateway.CanCreateDevices)
            {
                var device = gateway.CreateEmptyDevice();
                var properties = GetDeviceProperties(device);

                foreach (var property in properties)
                {
                    object value;
                    if (dto.TryGetValue(property.Name, out value))
                    {
                        var converted = Convert.ChangeType(value, property.PropertyType);
                        property.SetValue(device, converted);
                    }
                }

                var success = gateway.AddDevice(device);

                if (success)
                {
                    return Ok();
                }
            }

            return InternalServerError();
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
    }
}