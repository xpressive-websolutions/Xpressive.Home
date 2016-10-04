using System;
using System.Linq;
using Q42.HueApi;
using Xpressive.Home.Contracts.Variables;

namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal class PhilipsHueDeviceDiscoveringService : IPhilipsHueDeviceDiscoveringService
    {
        private readonly IVariableRepository _variableRepository;

        public PhilipsHueDeviceDiscoveringService(IVariableRepository variableRepository, IPhilipsHueBridgeDiscoveringService bridgeDiscoveringService)
        {
            _variableRepository = variableRepository;

            bridgeDiscoveringService.BridgeFound += OnBridgeFound;
        }

        public event EventHandler<PhilipsHueBulb> BulbFound;

        public event EventHandler<PhilipsHuePresenceSensor> PresenceSensorFound;

        private async void OnBridgeFound(object sender, PhilipsHueBridge bridge)
        {
            var variableName = $"PhilipsHue.{bridge.Id}.ApiKey";
            var apiKey = _variableRepository.Get<StringVariable>(variableName).Value;

            var client = new LocalHueClient(bridge.IpAddress, apiKey);
            var bulbs = await client.GetLightsAsync();
            var sensors = await client.GetSensorsAsync();

            if (bulbs != null)
            {
                foreach (var light in bulbs)
                {
                    var id = light.UniqueId.Replace(":", string.Empty).Replace("-", string.Empty);
                    var bulb = new PhilipsHueBulb(light.Id, id, light.Name, bridge)
                    {
                        Icon = "PhilipsHueIcon PhilipsHueIcon_" + light.ModelId,
                        Model = light.ModelId
                    };

                    OnBulbFound(bulb);
                }
            }

            if (sensors != null)
            {
                var presenceSensors = sensors
                    .Where(s => s.ModelId == "SML001" && s.Type == "ZLLPresence")
                    .ToList();

                foreach (var presenceSensor in presenceSensors)
                {
                    var id = presenceSensor.UniqueId.Replace(":", string.Empty).Replace("-", string.Empty);
                    var sensor = new PhilipsHuePresenceSensor(presenceSensor.Id, id, presenceSensor.Name, bridge)
                    {
                        Model = presenceSensor.ModelId,
                        Icon = "PhilipsHueIcon PhilipsHueIcon_PresenceSensor"
                    };

                    OnPresenceSensorFound(sensor);
                }
            }
        }

        protected virtual void OnBulbFound(PhilipsHueBulb e)
        {
            BulbFound?.Invoke(this, e);
        }

        protected virtual void OnPresenceSensorFound(PhilipsHuePresenceSensor e)
        {
            PresenceSensorFound?.Invoke(this, e);
        }
    }
}
