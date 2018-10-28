using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Variables;

namespace Xpressive.Home.Controllers
{
    [Route("api/v1/variable")]
    public class VariableController : Controller
    {
        private readonly IVariableRepository _variableRepository;
        private readonly IVariableHistoryService _variableHistoryService;
        private readonly IDictionary<string, IGateway> _gateways;

        public VariableController(IVariableRepository variableRepository, IVariableHistoryService variableHistoryService, IEnumerable<IGateway> gateways)
        {
            _variableRepository = variableRepository;
            _variableHistoryService = variableHistoryService;
            _gateways = gateways.ToDictionary(g => g.Name);
        }

        [HttpGet, Route("{variable}/value")]
        public IActionResult Get(string variable)
        {
            var result = _variableRepository.Get<IVariable>(variable);
            if (result != null)
            {
                return Ok(new VariableDto
                {
                    Name = result.Name,
                    Value = result.Value,
                    Type = result.Value?.GetType().Name,
                    Unit = result.Unit
                });
            }

            return NotFound();
        }

        [HttpGet, Route("{gatewayName}")]
        public IEnumerable<VariableDto> Get(string gatewayName, [FromQuery] string deviceId)
        {
            if (!_gateways.TryGetValue(gatewayName, out var gateway) ||
                !gateway.Devices.Any(d => d.Id.Equals(deviceId, StringComparison.Ordinal)))
            {
                return Enumerable.Empty<VariableDto>();
            }

            var prefix = $"{gatewayName}.{deviceId}.";
            var variables = _variableRepository.Get().Where(v => v.Name.StartsWith(prefix, StringComparison.Ordinal));

            var dtos = variables.Select(v => new VariableDto
            {
                Name = v.Name.Substring(prefix.Length),
                Value = v.Value,
                Type = v.Value?.GetType().Name,
                Unit = v.Unit
            })
            .Where(dto => !dto.Name.Equals("Name", StringComparison.OrdinalIgnoreCase))
            .ToList();

            return dtos;
        }

        [HttpGet, Route("history")]
        public IEnumerable<VariableHistoryDto> GetHistory([FromQuery]string variable)
        {
            var result = _variableRepository.Get<IVariable>(variable);

            if (result == null)
            {
                return new List<VariableHistoryDto>(0);
            }

            return _variableHistoryService
                .Get(result.Name)
                .Select(h => new VariableHistoryDto
                {
                    EffectiveDate = h.EffectiveDate,
                    Value = h.Value
                });
        }

        public class VariableDto
        {
            public string Name { get; set; }
            public object Value { get; set; }
            public string Type { get; set; }
            public string Unit { get; set; }
        }

        public class VariableHistoryDto
        {
            public DateTime EffectiveDate { get; set; }
            public object Value { get; set; }
        }
    }
}
