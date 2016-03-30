using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Q42.HueApi;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Variables;
using Action = Xpressive.Home.Contracts.Gateway.Action;

namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal class PhilipsHueGateway : GatewayBase
    {
        private readonly IVariableRepository _variableRepository;
        private readonly object _devicesLock = new object();

        public PhilipsHueGateway(IVariableRepository variableRepository, IPhilipsHueDeviceDiscoveringService deviceDiscoveringService) : base("PhilipsHue")
        {
            _variableRepository = variableRepository;
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

        protected override async Task ExecuteInternal(IDevice device, IAction action, IDictionary<string, string> values)
        {
            var bulb = (PhilipsHueDevice)device;
            var command = new LightCommand();
            var variableName = $"PhilipsHue.{bulb.Bridge.Id}.ApiKey";
            var apiKey = _variableRepository.Get<StringVariable>(variableName).Value;

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

            var client = new LocalHueClient(bulb.Bridge.IpAddress, apiKey);
            await client.SendCommandAsync(command, new[] { bulb.Id });
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
    }
}