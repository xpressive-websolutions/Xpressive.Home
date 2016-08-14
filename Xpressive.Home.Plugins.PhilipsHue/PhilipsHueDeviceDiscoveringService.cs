using System;
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

        public event EventHandler<PhilipsHueDevice> BulbFound;

        private async void OnBridgeFound(object sender, PhilipsHueBridge bridge)
        {
            var variableName = $"PhilipsHue.{bridge.Id}.ApiKey";
            var apiKey = _variableRepository.Get<StringVariable>(variableName).Value;

            var client = new LocalHueClient(bridge.IpAddress, apiKey);
            var bulbs = await client.GetLightsAsync();

            if (bulbs != null)
            {
                foreach (var light in bulbs)
                {
                    var id = light.UniqueId.Replace(":", string.Empty).Replace("-", string.Empty);
                    var bulb = new PhilipsHueDevice(light.Id, id, light.Name, bridge);

                    OnBulbFound(bulb);
                }
            }
        }

        protected virtual void OnBulbFound(PhilipsHueDevice e)
        {
            BulbFound?.Invoke(this, e);
        }
    }
}
