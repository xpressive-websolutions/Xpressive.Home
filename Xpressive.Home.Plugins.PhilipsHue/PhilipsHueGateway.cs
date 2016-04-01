using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Q42.HueApi;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Variables;
using Action = Xpressive.Home.Contracts.Gateway.Action;

namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal class PhilipsHueGateway : GatewayBase
    {
        private readonly IVariableRepository _variableRepository;
        private readonly IMessageQueue _messageQueue;
        private readonly object _devicesLock = new object();

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
            _actions.Add(new Action("Alarm Once"));
            _actions.Add(new Action("Alarm Multiple"));

            deviceDiscoveringService.BulbFound += OnBulbFound;
        }

        public override IDevice CreateEmptyDevice()
        {
            throw new NotSupportedException();
        }

        public async Task ObserveBulbStatusAsync()
        {
            while (true)
            {
                List<PhilipsHueDevice> bulbs;

                lock (_devicesLock)
                {
                    bulbs = _devices.Cast<PhilipsHueDevice>().ToList();
                }

                var bridges = bulbs.Select(d => d.Bridge).Distinct().ToList();

                foreach (var bridge in bridges)
                {
                    var client = GetClient(bridge);
                    var lights = await client.GetLightsAsync();
                    var tuples = lights.Select(l => Tuple.Create(bulbs.SingleOrDefault(b => b.Id.Equals(l.Id)), l));

                    foreach (var tuple in tuples)
                    {
                        var bulb = tuple.Item1;
                        var light = tuple.Item2;
                        var state = light.State;

                        UpdateVariable($"{Name}.{bridge.Id}_{bulb.Id}.Brightness", (double)state.Brightness);
                        UpdateVariable($"{Name}.{bridge.Id}_{bulb.Id}.IsOn", state.On);
                        UpdateVariable($"{Name}.{bridge.Id}_{bulb.Id}.IsReachable", state.IsReachable);
                        UpdateVariable($"{Name}.{bridge.Id}_{bulb.Id}.Name", light.Name);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }

        protected override async Task ExecuteInternal(IDevice device, IAction action, IDictionary<string, string> values)
        {
            var bulb = (PhilipsHueDevice)device;
            var command = new LightCommand();

            string seconds;
            int s;
            if (action.Fields.Contains("Transition time in seconds") &&
                values.TryGetValue("Transition time in seconds", out seconds) &&
                int.TryParse(seconds, out s))
            {
                command.TransitionTime = TimeSpan.FromSeconds(s);
            }

            string brightness;
            byte b;
            if (action.Fields.Contains("Brightness") &&
                values.TryGetValue("Brightness", out brightness) &&
                byte.TryParse(brightness, out b))
            {
                command.Brightness = b;
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
                    command.SetColor(values["Color"]);
                    break;
                case "change brightness":
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

            var client = GetClient(bulb.Bridge);
            await client.SendCommandAsync(command, new[] { bulb.Id });

            if (command.On.HasValue)
            {
                UpdateVariable($"{Name}.{bulb.Bridge.Id}_{bulb.Id}.IsOn", command.On.Value);
            }

            if (command.Brightness.HasValue)
            {
                UpdateVariable($"{Name}.{bulb.Bridge.Id}_{bulb.Id}.Brightness", (double) command.Brightness.Value);
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

                UpdateVariable($"{Name}.{e.Bridge.Id}_{e.Id}.Name", e.Name);

                _devices.Add(e);
            }
        }

        private void UpdateVariable(string name, object value)
        {
            _messageQueue.Publish(new UpdateVariableMessage(name, value));
        }

        private LocalHueClient GetClient(PhilipsHueBridge bridge)
        {
            var variableName = $"{Name}.{bridge.Id}.ApiKey";
            var apiKey = _variableRepository.Get<StringVariable>(variableName).Value;

            return new LocalHueClient(bridge.IpAddress, apiKey);
        }
    }
}