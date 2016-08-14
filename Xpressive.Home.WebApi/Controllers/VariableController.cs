using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Variables;

namespace Xpressive.Home.WebApi.Controllers
{
    [RoutePrefix("api/v1/variable")]
    public class VariableController : ApiController
    {
        private readonly IVariableRepository _variableRepository;
        private readonly IDictionary<string, IGateway> _gateways;

        public VariableController(IVariableRepository variableRepository, IEnumerable<IGateway> gateways)
        {
            _variableRepository = variableRepository;
            _gateways = gateways.ToDictionary(g => g.Name);
        }

        [HttpGet, Route("{gatewayName}/{deviceId}")]
        public IEnumerable<VariableDto> Get(string gatewayName, string deviceId)
        {
            IGateway gateway;
            if (!_gateways.TryGetValue(gatewayName, out gateway) ||
                !gateway.Devices.Any(d => d.Id.Equals(deviceId, StringComparison.Ordinal)))
            {
                return Enumerable.Empty<VariableDto>();
            }

            var prefix = $"{gatewayName}.{deviceId}";
            var variables = _variableRepository.Get().Where(v => v.Name.StartsWith(prefix, StringComparison.Ordinal));

            return variables.Select(v => new VariableDto
            {
                Name = v.Name.Substring(prefix.Length + 1),
                Value = v.Value
            });
        }

        public class VariableDto
        {
            public string Name { get; set; }
            public object Value { get; set; }
        }
    }
}