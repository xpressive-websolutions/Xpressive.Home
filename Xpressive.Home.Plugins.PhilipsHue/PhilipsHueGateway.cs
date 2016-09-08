using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Polly;
using Q42.HueApi;
using Q42.HueApi.ColorConverters;
using Q42.HueApi.ColorConverters.HSB;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Variables;
using Action = Xpressive.Home.Contracts.Gateway.Action;

namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal sealed class PhilipsHueGateway : GatewayBase, IPhilipsHueGateway
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PhilipsHueGateway));
        private readonly IVariableRepository _variableRepository;
        private readonly IMessageQueue _messageQueue;
        private readonly object _devicesLock = new object();
        private readonly AutoResetEvent _taskWaitHandle = new AutoResetEvent(false);
        private bool _isRunning = true;

        public PhilipsHueGateway(
            IVariableRepository variableRepository,
            IPhilipsHueDeviceDiscoveringService deviceDiscoveringService,
            IMessageQueue messageQueue) : base("PhilipsHue")
        {
            _variableRepository = variableRepository;
            _messageQueue = messageQueue;
            _canCreateDevices = false;

            _actions.Add(new Action("Switch On")
            {
                Fields = { "Transition time in seconds" }
            });
            _actions.Add(new Action("Switch Off")
            {
                Fields = { "Transition time in seconds" }
            });
            _actions.Add(new Action("Change Color")
            {
                Fields = { "Color", "Transition time in seconds" }
            });
            _actions.Add(new Action("Change Brightness")
            {
                Fields = { "Brightness", "Transition time in seconds" }
            });
            _actions.Add(new Action("Change Temperature")
            {
                Fields = { "Temperature" }
            });
            _actions.Add(new Action("Alarm Once"));
            _actions.Add(new Action("Alarm Multiple"));

            deviceDiscoveringService.BulbFound += OnBulbFound;
        }

        public IEnumerable<PhilipsHueDevice> GetDevices()
        {
            return Devices.OfType<PhilipsHueDevice>();
        }

        public void SwitchOn(PhilipsHueDevice device, int transitionTimeInSeconds)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Transition time in seconds", transitionTimeInSeconds.ToString()}
            };

            var action = _actions.Single(a => a.Name.Equals("Switch On", StringComparison.Ordinal));
            StartActionInNewTask(device, action, parameters);
        }

        public void SwitchOff(PhilipsHueDevice device, int transitionTimeInSeconds)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Transition time in seconds", transitionTimeInSeconds.ToString()}
            };

            var action = _actions.Single(a => a.Name.Equals("Switch Off", StringComparison.Ordinal));
            StartActionInNewTask(device, action, parameters);
        }

        public void ChangeColor(PhilipsHueDevice device, string hexColor, int transitionTimeInSeconds)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Color", hexColor},
                {"Transition time in seconds", transitionTimeInSeconds.ToString()}
            };

            var action = _actions.Single(a => a.Name.Equals("Change Color", StringComparison.Ordinal));
            StartActionInNewTask(device, action, parameters);
        }

        public void ChangeBrightness(PhilipsHueDevice device, double brightness, int transitionTimeInSeconds)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Brightness", brightness.ToString("F2")},
                {"Transition time in seconds", transitionTimeInSeconds.ToString()}
            };

            var action = _actions.Single(a => a.Name.Equals("Change Brightness", StringComparison.Ordinal));
            StartActionInNewTask(device, action, parameters);
        }

        public void ChangeTemperature(PhilipsHueDevice device, int temperature)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Temperature", temperature.ToString("D")}
            };

            var action = _actions.Single(a => a.Name.Equals("Change Temperature", StringComparison.Ordinal));
            StartActionInNewTask(device, action, parameters);
        }

        public override IDevice CreateEmptyDevice()
        {
            throw new NotSupportedException();
        }

        public override async Task StartAsync()
        {
            await TaskHelper.DelayAsync(TimeSpan.FromSeconds(1), () => _isRunning);

            while (_isRunning)
            {
                List<PhilipsHueDevice> bulbs;

                lock (_devicesLock)
                {
                    bulbs = _devices.Cast<PhilipsHueDevice>().ToList();
                }

                var bridges = bulbs.Select(d => d.Bridge).Distinct().ToList();

                foreach (var bridge in bridges)
                {
                    try
                    {
                        var client = GetClient(bridge);
                        var lights = await client.GetLightsAsync();
                        var tuples = lights.Select(l => Tuple.Create(bulbs.SingleOrDefault(b => IsEqual(b, l)), l));

                        foreach (var tuple in tuples)
                        {
                            var bulb = tuple.Item1;
                            var light = tuple.Item2;
                            var state = light.State;
                            var brightness = state.Brightness / 255d;
                            var temperature = state.ColorTemperature ?? 0;

                            if (temperature != 0)
                            {
                                temperature = MirekToKelvin(temperature);
                            }

                            UpdateVariable($"{Name}.{bulb.Id}.Brightness", Math.Round(brightness, 2));
                            UpdateVariable($"{Name}.{bulb.Id}.IsOn", state.On);
                            UpdateVariable($"{Name}.{bulb.Id}.IsReachable", state.IsReachable);
                            UpdateVariable($"{Name}.{bulb.Id}.Name", light.Name);
                            UpdateVariable($"{Name}.{bulb.Id}.ColorTemperature", (double)temperature);
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

                await TaskHelper.DelayAsync(TimeSpan.FromSeconds(30), () => _isRunning);
            }

            _taskWaitHandle.Set();
        }

        public override void Stop()
        {
            _isRunning = false;
            if (!_taskWaitHandle.WaitOne(TimeSpan.FromSeconds(5)))
            {
                _log.Error("Unable to shutdown.");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _taskWaitHandle.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override async Task ExecuteInternalAsync(IDevice device, IAction action, IDictionary<string, string> values)
        {
            if (device == null)
            {
                _log.Warn($"Unable to execute action {action.Name} because the device was not found.");
                return;
            }

            var bulb = (PhilipsHueDevice)device;
            var command = new LightCommand();

            string seconds;
            int s;
            if (action.Fields.Contains("Transition time in seconds") &&
                values.TryGetValue("Transition time in seconds", out seconds) &&
                int.TryParse(seconds, out s) &&
                s > 0)
            {
                command.TransitionTime = TimeSpan.FromSeconds(s);
            }

            string brightness;
            double bd;
            if (action.Fields.Contains("Brightness") &&
                values.TryGetValue("Brightness", out brightness) &&
                double.TryParse(brightness, out bd) &&
                bd >= 0d && bd <= 1d)
            {
                command.Brightness = Convert.ToByte(bd * 255);
            }

            string temperature;
            int t;
            if (action.Fields.Contains("Temperature") &&
                values.TryGetValue("Temperature", out temperature) &&
                int.TryParse(temperature, out t) &&
                t >= 2000 && t <= 6500)
            {
                command.ColorTemperature = KelvinToMirek(t);
            }

            switch (action.Name.ToLowerInvariant())
            {
                case "switch on":
                    command.On = true;
                    break;
                case "switch off":
                    command.On = false;
                    break;
                case "change color":
                    command.On = true;
                    command.SetColor(new RGBColor(values["Color"]));
                    break;
                case "change brightness":
                    command.On = true;
                    break;
                case "change temperature":
                    break;
                case "alarm once":
                    command.Alert = Alert.Once;
                    break;
                case "alarm multiple":
                    command.Alert = Alert.Multiple;
                    break;
                default:
                    return;
            }

            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(new []
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5)
                });

            try
            {
                await policy.ExecuteAsync(async () =>
                {
                    var client = GetClient(bulb.Bridge);
                    await client.SendCommandAsync(command, new[] { bulb.Index });
                });

                if (command.On.HasValue)
                {
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

                UpdateVariable($"{Name}.{e.Id}.Name", e.Name);

                _devices.Add(e);
            }
        }

        private void UpdateVariable(string name, object value)
        {
            _messageQueue.Publish(new UpdateVariableMessage(name, value));
        }

        private bool IsEqual(PhilipsHueDevice device, Light light)
        {
            var lightId = light.UniqueId.Replace(":", string.Empty).Replace("-", string.Empty);
            return device.Id.Equals(lightId, StringComparison.OrdinalIgnoreCase);
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

        private int KelvinToMirek(int kelvin)
        {
            return 1000000/kelvin;
        }
    }
}
