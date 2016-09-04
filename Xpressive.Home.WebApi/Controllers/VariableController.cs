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

        [HttpGet, Route("{gatewayName}")]
        public IEnumerable<VariableDto> Get(string gatewayName, [FromUri] string deviceId)
        {
            IGateway gateway;
            if (!_gateways.TryGetValue(gatewayName, out gateway) ||
                !gateway.Devices.Any(d => d.Id.Equals(deviceId, StringComparison.Ordinal)))
            {
                return Enumerable.Empty<VariableDto>();
            }

            var prefix = $"{gatewayName}.{deviceId}.";
            var variables = _variableRepository.Get().Where(v => v.Name.StartsWith(prefix, StringComparison.Ordinal));

            return variables.Select(v => new VariableDto
            {
                Name = v.Name.Substring(prefix.Length),
                Value = v.Value
            });
        }

        [HttpGet, Route("{variable}")]
        public IHttpActionResult Get(string variable)
        {
            var result = _variableRepository.Get().SingleOrDefault(v => v.Name.Equals(variable, StringComparison.OrdinalIgnoreCase));
            if (result != null)
            {
                return Ok(new VariableDto
                {
                    Name = result.Name,
                    Value = result.Value
                });
            }

            return NotFound();
        }

        public class VariableDto
        {
            public string Name { get; set; }
            public object Value { get; set; }
        }
    }
}
