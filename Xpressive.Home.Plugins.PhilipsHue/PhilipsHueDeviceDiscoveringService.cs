using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Q42.HueApi;
using Q42.HueApi.Models;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Variables;

namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal class PhilipsHueDeviceDiscoveringService : IPhilipsHueDeviceDiscoveringService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PhilipsHueDeviceDiscoveringService));
        private readonly IVariableRepository _variableRepository;
        private readonly IPhilipsHueBridgeDiscoveringService _bridgeDiscoveringService;
        private readonly IList<PhilipsHueBridge> _bridges;

        public PhilipsHueDeviceDiscoveringService(IVariableRepository variableRepository, IPhilipsHueBridgeDiscoveringService bridgeDiscoveringService)
        {
            _variableRepository = variableRepository;
            _bridgeDiscoveringService = bridgeDiscoveringService;
            _bridges = new List<PhilipsHueBridge>();
        }

        public event EventHandler<PhilipsHueBulb> BulbFound;

        public event EventHandler<PhilipsHuePresenceSensor> PresenceSensorFound;

        public event EventHandler<PhilipsHueButtonSensor> ButtonSensorFound;

        public void Start(CancellationToken cancellationToken)
        {
            _bridgeDiscoveringService.BridgeFound += OnBridgeFound;
            _bridgeDiscoveringService.Start();

            Task.Factory.StartNew(async () => await FindDevices(cancellationToken));
        }

        private async void OnBridgeFound(object sender, PhilipsHueBridge bridge)
        {
            _bridges.Add(bridge);
            await FindDevices(bridge);
        }

        private async Task FindDevices(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(t => { });

            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var bridge in _bridges)
                {
                    await FindDevices(bridge);
                }

                await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken).ContinueWith(t => { });
            }
        }

        private async Task FindDevices(PhilipsHueBridge bridge)
        {
            try
            {
                var variableName = $"PhilipsHue.{bridge.Id}.ApiKey";
                var apiKey = _variableRepository.Get<StringVariable>(variableName).Value;

                var client = new LocalHueClient(bridge.IpAddress, apiKey);

                await FindBulbsAsync(client, bridge);
                await FindSensorsAsync(client, bridge);
            }
            catch (Exception e)
            {
                _log.Error($"Unable to get devices ({e.GetType().Name}) {e.Message}");
            }
        }

        private async Task FindBulbsAsync(LocalHueClient client, PhilipsHueBridge bridge)
        {
            var bulbs = await client.GetLightsAsync();

            if (bulbs != null)
            {
                foreach (var light in bulbs)
                {
                    var id = light.UniqueId.RemoveMacAddressDelimiters();
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
            var id = GetUniqueId(sensor);
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
            var id = GetUniqueId(sensor);
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
            var id = GetUniqueId(sensor);
            var button = new PhilipsHueButtonSensor(sensor.Id, id, sensor.Name, bridge)
            {
                Model = sensor.ModelId,
                Type = sensor.Type,
                Icon = "PhilipsHueIcon PhilipsHueIcon_ZllSwitch",
                Battery = sensor.Config.Battery ?? 100
            };

            OnButtonSensorFound(button);
        }

        private string GetUniqueId(Sensor sensor)
        {
            var uniqueId = sensor.UniqueId.Substring(0, 23).RemoveMacAddressDelimiters();
            return uniqueId;
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
