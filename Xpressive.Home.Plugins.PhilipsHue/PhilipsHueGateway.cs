using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Polly;
using Polly.Retry;
using Q42.HueApi;
using Q42.HueApi.Models;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Variables;
using Xpressive.Home.Plugins.PhilipsHue.LightCommandStrategies;
using Action = Xpressive.Home.Contracts.Gateway.Action;

namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal sealed class PhilipsHueGateway : GatewayBase, IPhilipsHueGateway
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PhilipsHueGateway));
        private readonly IVariableRepository _variableRepository;
        private readonly IPhilipsHueDeviceDiscoveringService _deviceDiscoveringService;
        private readonly IMessageQueue _messageQueue;
        private readonly object _devicesLock = new object();
        private readonly RetryPolicy _executeCommandPolicy;
        private readonly ConcurrentQueue<Tuple<PhilipsHueBulb, LightCommand>> _commandQueue = new ConcurrentQueue<Tuple<PhilipsHueBulb, LightCommand>>();

        public PhilipsHueGateway(
            IVariableRepository variableRepository,
            IPhilipsHueDeviceDiscoveringService deviceDiscoveringService,
            IMessageQueue messageQueue) : base("PhilipsHue")
        {
            _variableRepository = variableRepository;
            _deviceDiscoveringService = deviceDiscoveringService;
            _messageQueue = messageQueue;
            _canCreateDevices = false;

            _deviceDiscoveringService.BulbFound += OnDeviceFound;
            _deviceDiscoveringService.PresenceSensorFound += OnDeviceFound;
            _deviceDiscoveringService.ButtonSensorFound += OnDeviceFound;

            _executeCommandPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5)
                });
        }

        public IEnumerable<PhilipsHueDevice> GetDevices()
        {
            return Devices.OfType<PhilipsHueDevice>();
        }

        public override IEnumerable<IAction> GetActions(IDevice device)
        {
            if (device is PhilipsHueBulb)
            {
                yield return new Action("Switch On") { Fields = { "Transition time in seconds" } };
                yield return new Action("Switch Off") { Fields = { "Transition time in seconds" } };
                yield return new Action("Change Color") { Fields = { "Color", "Transition time in seconds" } };
                yield return new Action("Change Brightness") { Fields = { "Brightness", "Transition time in seconds" } };
                yield return new Action("Change Temperature") { Fields = { "Temperature" } };
                yield return new Action("Alarm Once");
                yield return new Action("Alarm Multiple");
            }
        }

        public void SwitchOn(PhilipsHueDevice device, int transitionTimeInSeconds)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Transition time in seconds", transitionTimeInSeconds.ToString()}
            };

            var action = GetActions(device).Single(a => a.Name.Equals("Switch On", StringComparison.Ordinal));
            StartActionInNewTask(device, action, parameters);
        }

        public void SwitchOff(PhilipsHueDevice device, int transitionTimeInSeconds)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Transition time in seconds", transitionTimeInSeconds.ToString()}
            };

            var action = GetActions(device).Single(a => a.Name.Equals("Switch Off", StringComparison.Ordinal));
            StartActionInNewTask(device, action, parameters);
        }

        public void ChangeColor(PhilipsHueDevice device, string hexColor, int transitionTimeInSeconds)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Color", hexColor},
                {"Transition time in seconds", transitionTimeInSeconds.ToString()}
            };

            var action = GetActions(device).Single(a => a.Name.Equals("Change Color", StringComparison.Ordinal));
            StartActionInNewTask(device, action, parameters);
        }

        public void ChangeBrightness(PhilipsHueDevice device, double brightness, int transitionTimeInSeconds)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Brightness", brightness.ToString("F2")},
                {"Transition time in seconds", transitionTimeInSeconds.ToString()}
            };

            var action = GetActions(device).Single(a => a.Name.Equals("Change Brightness", StringComparison.Ordinal));
            StartActionInNewTask(device, action, parameters);
        }

        public void ChangeTemperature(PhilipsHueDevice device, int temperature)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Temperature", temperature.ToString("D")}
            };

            var action = GetActions(device).Single(a => a.Name.Equals("Change Temperature", StringComparison.Ordinal));
            StartActionInNewTask(device, action, parameters);
        }

        public override IDevice CreateEmptyDevice()
        {
            throw new NotSupportedException();
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(t => { });

            _deviceDiscoveringService.Start();

            var _ = Task.Factory.StartNew(() => StartCommandQueueWorker(cancellationToken), TaskCreationOptions.LongRunning);

            while (!cancellationToken.IsCancellationRequested)
            {
                List<PhilipsHueBulb> bulbs;
                List<PhilipsHuePresenceSensor> presenceSensors;
                List<PhilipsHueButtonSensor> buttonSensors;

                lock (_devicesLock)
                {
                    bulbs = _devices.OfType<PhilipsHueBulb>().ToList();
                    presenceSensors = _devices.OfType<PhilipsHuePresenceSensor>().ToList();
                    buttonSensors = _devices.OfType<PhilipsHueButtonSensor>().ToList();
                }

                var bridges = bulbs
                    .Select(d => d.Bridge)
                    .Union(presenceSensors.Select(s => s.Bridge))
                    .Distinct()
                    .ToList();

                foreach (var bridge in bridges)
                {
                    try
                    {
                        var client = GetClient(bridge);

                        await _executeCommandPolicy.ExecuteAsync(() => UpdateBulbVariablesAsync(client, bulbs));

                        for (var i = 0; i < 5 && !cancellationToken.IsCancellationRequested; i++)
                        {
                            var sensors = await _executeCommandPolicy.ExecuteAsync(() => client.GetSensorsAsync());
                            UpdateSensorVariables(sensors, presenceSensors);
                            UpdateSensorVariables(sensors, buttonSensors);
                            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken).ContinueWith(t => { });
                        }
                    }
                    catch (WebException)
                    {
                        continue;
                    }
                    catch (Exception e)
                    {
                        _log.Error(e.Message, e);
                    }
                }
            }
        }

        protected override Task ExecuteInternalAsync(IDevice device, IAction action, IDictionary<string, string> values)
        {
            if (device == null)
            {
                _log.Warn($"Unable to execute action {action.Name} because the device was not found.");
                return Task.CompletedTask;
            }

            var bulb = device as PhilipsHueBulb;

            if (bulb == null)
            {
                _log.Warn($"Unable to execute action {action.Name} because the device isn't a bulb.");
                return Task.CompletedTask;
            }

            var strategy = LightCommandStrategyBase.Get(action);
            var command = strategy.GetLightCommand(values, bulb);
            _commandQueue.Enqueue(Tuple.Create(bulb, command));

            return Task.CompletedTask;
        }

        private async Task UpdateBulbVariablesAsync(LocalHueClient client, List<PhilipsHueBulb> bulbs)
        {
            var lights = await client.GetLightsAsync();

            foreach (var light in lights)
            {
                var bulb = bulbs.SingleOrDefault(b => IsEqual(b, light));

                if (bulb == null)
                {
                    continue;
                }

                var state = light.State;
                var brightness = state.Brightness / 255d;
                var temperature = state.ColorTemperature ?? 0;

                if (temperature != 0)
                {
                    temperature = MirekToKelvin(temperature);
                }

                bulb.IsOn = state.On;

                UpdateVariable($"{Name}.{bulb.Id}.Brightness", Math.Round(brightness, 2));
                UpdateVariable($"{Name}.{bulb.Id}.IsOn", state.On);
                UpdateVariable($"{Name}.{bulb.Id}.IsReachable", state.IsReachable);
                UpdateVariable($"{Name}.{bulb.Id}.Name", light.Name);
                UpdateVariable($"{Name}.{bulb.Id}.ColorTemperature", (double)temperature);
            }
        }

        private void UpdateSensorVariables(IEnumerable<Sensor> sensors, List<PhilipsHuePresenceSensor> presenceSensors)
        {
            foreach (var sensor in sensors)
            {
                var presenceSensor = presenceSensors.SingleOrDefault(s => IsEqual(s, sensor));

                if (presenceSensor == null)
                {
                    continue;
                }

                var state = sensor.State;

                presenceSensor.HasPresence = state.Presence ?? false;
                presenceSensor.Battery = sensor.Config.Battery ?? 100;

                UpdateVariable($"{Name}.{presenceSensor.Id}.Presence", state.Presence);
            }
        }

        private void UpdateSensorVariables(IEnumerable<Sensor> sensors, List<PhilipsHueButtonSensor> buttonSensors)
        {
            foreach (var sensor in sensors)
            {
                var buttonSensor = buttonSensors.SingleOrDefault(s => IsEqual(s, sensor));

                if (buttonSensor == null)
                {
                    continue;
                }

                DateTime lastButtonOccurrence;
                if (!DateTime.TryParse(sensor.State.Lastupdated, out lastButtonOccurrence))
                {
                    lastButtonOccurrence = DateTime.MinValue;
                }

                buttonSensor.LastButton = sensor.State.ButtonEvent ?? 0;
                var isFirstTime = buttonSensor.LastButtonOccurrence == DateTime.MinValue;
                var somethingHappend = !isFirstTime && buttonSensor.LastButtonOccurrence < lastButtonOccurrence;
                UpdateButtonSensorVariables(buttonSensor, somethingHappend);
                buttonSensor.LastButtonOccurrence = lastButtonOccurrence;

                buttonSensor.Battery = sensor.Config.Battery ?? 100;
            }
        }

        private void UpdateButtonSensorVariables(PhilipsHueButtonSensor buttonSensor, bool somethingHappend)
        {
            if (buttonSensor.Type.Equals("ZGPSwitch", StringComparison.OrdinalIgnoreCase))
            {
                UpdateVariable($"{Name}.{buttonSensor.Id}.Button1", somethingHappend && buttonSensor.LastButton == 34);
                UpdateVariable($"{Name}.{buttonSensor.Id}.Button2", somethingHappend && buttonSensor.LastButton == 16);
                UpdateVariable($"{Name}.{buttonSensor.Id}.Button3", somethingHappend && buttonSensor.LastButton == 17);
                UpdateVariable($"{Name}.{buttonSensor.Id}.Button4", somethingHappend && buttonSensor.LastButton == 18);
            }
            else if (buttonSensor.Type.Equals("ZLLSwitch", StringComparison.OrdinalIgnoreCase))
            {
                UpdateVariable($"{Name}.{buttonSensor.Id}.ButtonOn", somethingHappend && buttonSensor.LastButton >= 1000 && buttonSensor.LastButton < 2000);
                UpdateVariable($"{Name}.{buttonSensor.Id}.ButtonUp", somethingHappend && buttonSensor.LastButton >= 2000 && buttonSensor.LastButton < 3000);
                UpdateVariable($"{Name}.{buttonSensor.Id}.ButtonDown", somethingHappend && buttonSensor.LastButton >= 3000 && buttonSensor.LastButton < 4000);
                UpdateVariable($"{Name}.{buttonSensor.Id}.ButtonOff", somethingHappend && buttonSensor.LastButton >= 4000 && buttonSensor.LastButton < 5000);
            }
        }

        private async void StartCommandQueueWorker(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Tuple<PhilipsHueBulb, LightCommand> tuple;
                if (_commandQueue.TryDequeue(out tuple))
                {
                    var bulb = tuple.Item1;
                    var command = tuple.Item2;

                    if (command.On.HasValue)
                    {
                        if (command.On.Value == bulb.IsOn)
                        {
                            command.On = null;
                        }
                    }

                    var waitTime = GetWaitTimeAfterCommandExecution(command);

                    if (waitTime == TimeSpan.Zero)
                    {
                        // in this case, the command has no effect
                        continue;
                    }

                    await ExecuteCommand(bulb, command);
                    await Task.Delay(waitTime, cancellationToken).ContinueWith(_ => { });
                }
                else
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(5), cancellationToken).ContinueWith(_ => { });
                }
            }
        }

        private TimeSpan GetWaitTimeAfterCommandExecution(LightCommand command)
        {
            // according to http://www.developers.meethue.com/documentation/hue-system-performance
            var numberOfCommands = 0;

            if (command.Alert.HasValue) { numberOfCommands++; }
            if (command.Brightness.HasValue) { numberOfCommands++; }
            if (command.BrightnessIncrement.HasValue) { numberOfCommands++; }
            if (command.Hue.HasValue) { numberOfCommands++; }
            if (command.Saturation.HasValue) { numberOfCommands++; }
            if (command.ColorCoordinates != null) { numberOfCommands++; }
            if (command.ColorTemperature.HasValue) { numberOfCommands++; }
            if (command.On.HasValue) { numberOfCommands++; }

            return TimeSpan.FromMilliseconds(numberOfCommands * 50);
        }

        private async Task ExecuteCommand(PhilipsHueBulb bulb, LightCommand command)
        {
            try
            {
                await _executeCommandPolicy.ExecuteAsync(async () =>
                {
                    var client = GetClient(bulb.Bridge);
                    await client.SendCommandAsync(command, new[] { bulb.Index });
                });

                if (command.On.HasValue)
                {
                    bulb.IsOn = command.On.Value;
                    UpdateVariable($"{Name}.{bulb.Id}.IsOn", command.On.Value);
                }

                if (command.Brightness.HasValue)
                {
                    var db = command.Brightness.Value / 255d;
                    UpdateVariable($"{Name}.{bulb.Id}.Brightness", Math.Round(db, 2));
                }
            }
            catch (Exception e)
            {
                _log.Error(e.Message, e);
            }
        }

        private void OnDeviceFound(object sender, PhilipsHueDevice device)
        {
            lock (_devicesLock)
            {
                if (_devices.Cast<PhilipsHueDevice>().Any(d => d.Id.Equals(device.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                _devices.Add(device);
            }
        }

        private void UpdateVariable(string name, object value)
        {
            _messageQueue.Publish(new UpdateVariableMessage(name, value));
        }

        private bool IsEqual(PhilipsHueBulb device, Light light)
        {
            var lightId = light.UniqueId.Replace(":", string.Empty).Replace("-", string.Empty);
            return device.Id.Equals(lightId, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsEqual(PhilipsHueDevice device, Sensor sensor)
        {
            if (sensor.UniqueId == null)
            {
                return false;
            }

            var sensorId = sensor.UniqueId.Replace(":", string.Empty).Replace("-", string.Empty);
            return device.Id.Equals(sensorId, StringComparison.OrdinalIgnoreCase);
        }

        private LocalHueClient GetClient(PhilipsHueBridge bridge)
        {
            var variableName = $"{Name}.{bridge.Id}.ApiKey";
            var apiKey = _variableRepository.Get<StringVariable>(variableName).Value;

            return new LocalHueClient(bridge.IpAddress, apiKey);
        }

        private int MirekToKelvin(int mirek)
        {
            return 1000000/mirek;
        }
    }
}
