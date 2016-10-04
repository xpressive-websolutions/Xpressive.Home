using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Polly;
using Polly.Retry;
using Q42.HueApi;
using Q42.HueApi.Models;
using Xpressive.Home.Contracts;
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
        private readonly IMessageQueue _messageQueue;
        private readonly object _devicesLock = new object();
        private readonly AutoResetEvent _queryWaitHandle = new AutoResetEvent(false);
        private readonly AutoResetEvent _commandWaitHandle = new AutoResetEvent(false);
        private readonly RetryPolicy _executeCommandPolicy;
        private readonly ConcurrentQueue<Tuple<PhilipsHueBulb, LightCommand>> _commandQueue = new ConcurrentQueue<Tuple<PhilipsHueBulb, LightCommand>>();
        private bool _isRunning = true;

        public PhilipsHueGateway(
            IVariableRepository variableRepository,
            IPhilipsHueDeviceDiscoveringService deviceDiscoveringService,
            IMessageQueue messageQueue) : base("PhilipsHue")
        {
            _variableRepository = variableRepository;
            _messageQueue = messageQueue;
            _canCreateDevices = false;

            deviceDiscoveringService.BulbFound += OnBulbFound;
            deviceDiscoveringService.PresenceSensorFound += OnPresenceSensorFound;

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

        public override async Task StartAsync()
        {
            await TaskHelper.DelayAsync(TimeSpan.FromSeconds(1), () => _isRunning);

            var _ = Task.Factory.StartNew(StartCommandQueueWorker, TaskCreationOptions.LongRunning);

            while (_isRunning)
            {
                List<PhilipsHueBulb> bulbs;
                List<PhilipsHuePresenceSensor> presenceSensors;

                lock (_devicesLock)
                {
                    bulbs = _devices.OfType<PhilipsHueBulb>().ToList();
                    presenceSensors = _devices.OfType<PhilipsHuePresenceSensor>().ToList();
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

                        for (var i = 0; i < 5 && _isRunning; i++)
                        {
                            await _executeCommandPolicy.ExecuteAsync(() => UpdateSensorVariablesAsync(client, presenceSensors));
                            await TaskHelper.DelayAsync(TimeSpan.FromSeconds(5), () => _isRunning);
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

            _queryWaitHandle.Set();
        }

        public override void Stop()
        {
            _isRunning = false;

            if (!_queryWaitHandle.WaitOne(TimeSpan.FromSeconds(5)))
            {
                _log.Error("Unable to shutdown query loop.");
            }

            if (!_commandWaitHandle.WaitOne(TimeSpan.FromSeconds(5)))
            {
                _log.Error("Unable to shutdown command loop.");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _queryWaitHandle.Dispose();
                _commandWaitHandle.Dispose();
            }
            base.Dispose(disposing);
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

        private async Task UpdateSensorVariablesAsync(LocalHueClient client, List<PhilipsHuePresenceSensor> presenceSensors)
        {
            var sensors = await client.GetSensorsAsync();

            foreach (var sensor in sensors)
            {
                var presenceSensor = presenceSensors.SingleOrDefault(s => IsEqual(s, sensor));

                if (presenceSensor == null)
                {
                    continue;
                }

                var state = sensor.State;

                presenceSensor.HasPresence = state.Presence;

                UpdateVariable($"{Name}.{presenceSensor.Id}.Presence", state.Presence);
            }
        }

        private async void StartCommandQueueWorker()
        {
            while (_isRunning)
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
                    await TaskHelper.DelayAsync(waitTime, () => _isRunning);
                }
                else
                {
                    await TaskHelper.DelayAsync(TimeSpan.FromMilliseconds(5), () => _isRunning);
                }
            }

            _commandWaitHandle.Set();
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

        private void OnBulbFound(object sender, PhilipsHueDevice e)
        {
            lock (_devicesLock)
            {
                if (_devices.Cast<PhilipsHueDevice>().Any(d => d.Id.Equals(e.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                _devices.Add(e);
            }
        }

        private void OnPresenceSensorFound(object sender, PhilipsHuePresenceSensor e)
        {
            lock (_devicesLock)
            {
                if (_devices.Cast<PhilipsHueDevice>().Any(d => d.Id.Equals(e.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                _devices.Add(e);
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

        private bool IsEqual(PhilipsHuePresenceSensor device, Sensor sensor)
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
