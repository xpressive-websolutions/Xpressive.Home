using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Q42.HueApi;
using Q42.HueApi.Models;
using Xpressive.Home.Contracts.Variables;

namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal class PhilipsHueDeviceDiscoveringService : IPhilipsHueDeviceDiscoveringService
    {
        private readonly IVariableRepository _variableRepository;
        private readonly IPhilipsHueBridgeDiscoveringService _bridgeDiscoveringService;

        public PhilipsHueDeviceDiscoveringService(IVariableRepository variableRepository, IPhilipsHueBridgeDiscoveringService bridgeDiscoveringService)
        {
            _variableRepository = variableRepository;
            _bridgeDiscoveringService = bridgeDiscoveringService;
        }

        public event EventHandler<PhilipsHueBulb> BulbFound;

        public event EventHandler<PhilipsHuePresenceSensor> PresenceSensorFound;

        public event EventHandler<PhilipsHueButtonSensor> ButtonSensorFound;

        public void Start()
        {
            _bridgeDiscoveringService.BridgeFound += OnBridgeFound;
            _bridgeDiscoveringService.Start();
        }

        private async void OnBridgeFound(object sender, PhilipsHueBridge bridge)
        {
            var variableName = $"PhilipsHue.{bridge.Id}.ApiKey";
            var apiKey = _variableRepository.Get<StringVariable>(variableName).Value;

            var client = new LocalHueClient(bridge.IpAddress, apiKey);

            await FindBulbsAsync(client, bridge);
            await FindSensorsAsync(client, bridge);
        }

        private async Task FindBulbsAsync(LocalHueClient client, PhilipsHueBridge bridge)
        {
            var bulbs = await client.GetLightsAsync();

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
        }

        private async Task FindSensorsAsync(LocalHueClient client, PhilipsHueBridge bridge)
        {
            var sensors = await client.GetSensorsAsync();

            if (sensors != null)
            {
                foreach (var sensor in sensors)
                {
                    switch (sensor.Type.ToLowerInvariant())
                    {
                        case "zllpresence":
                            HandleZllPresenceSensor(sensor, bridge);
                            break;
                        case "zgpswitch":
                            HandleZgpSwitch(sensor, bridge);
                            break;
                        case "zllswitch":
                            HandleZllSwitch(sensor, bridge);
                            break;
                        default:
                            Debug.WriteLine(sensor.Type);
                            break;
                    }
                }
            }
        }

        private void HandleZllPresenceSensor(Sensor sensor, PhilipsHueBridge bridge)
        {
            var id = sensor.UniqueId.Replace(":", string.Empty).Replace("-", string.Empty);
            var sensorDevice = new PhilipsHuePresenceSensor(sensor.Id, id, sensor.Name, bridge)
            {
                Model = sensor.ModelId,
                Type = sensor.Type,
                Icon = "PhilipsHueIcon PhilipsHueIcon_PresenceSensor",
                Battery = sensor.Config.Battery ?? 100
            };

            OnPresenceSensorFound(sensorDevice);
        }

        private void HandleZgpSwitch(Sensor sensor, PhilipsHueBridge bridge)
        {
            var id = sensor.UniqueId.Replace(":", string.Empty).Replace("-", string.Empty);
            var button = new PhilipsHueButtonSensor(sensor.Id, id, sensor.Name, bridge)
            {
                Model = sensor.ModelId,
                Type = sensor.Type,
                Icon = "PhilipsHueIcon PhilipsHueIcon_ZgpSwitch",
                Battery = sensor.Config.Battery ?? 100
            };

            OnButtonSensorFound(button);
        }

        private void HandleZllSwitch(Sensor sensor, PhilipsHueBridge bridge)
        {
            var id = sensor.UniqueId.Replace(":", string.Empty).Replace("-", string.Empty);
            var button = new PhilipsHueButtonSensor(sensor.Id, id, sensor.Name, bridge)
            {
                Model = sensor.ModelId,
                Type = sensor.Type,
                Icon = "PhilipsHueIcon PhilipsHueIcon_ZllSwitch",
                Battery = sensor.Config.Battery ?? 100
            };

            OnButtonSensorFound(button);
        }

        protected virtual void OnBulbFound(PhilipsHueBulb e)
        {
            BulbFound?.Invoke(this, e);
        }

        protected virtual void OnPresenceSensorFound(PhilipsHuePresenceSensor e)
        {
            PresenceSensorFound?.Invoke(this, e);
        }

        protected virtual void OnButtonSensorFound(PhilipsHueButtonSensor e)
        {
            ButtonSensorFound?.Invoke(this, e);
        }
    }
}
