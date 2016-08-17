using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using Action = Xpressive.Home.Contracts.Gateway.Action;

namespace Xpressive.Home.Plugins.Lifx
{
    internal sealed class LifxGateway : GatewayBase, ILifxGateway
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(LifxGateway));
        private readonly IMessageQueue _messageQueue;
        private readonly string _token;
        private readonly object _deviceLock = new object();
        private readonly LifxLocalClient _localClient = new LifxLocalClient();

        public LifxGateway(IMessageQueue messageQueue) : base("Lifx")
        {
            _messageQueue = messageQueue;
            _canCreateDevices = false;
            _token = ConfigurationManager.AppSettings["lifx.token"];

            _actions.Add(new Action("Switch On")
            {
                Fields = {"Transition time in seconds"}
            });
            _actions.Add(new Action("Switch Off")
            {
                Fields = {"Transition time in seconds"}
            });
            _actions.Add(new Action("Change Color")
            {
                Fields = {"Color", "Transition time in seconds"}
            });
            _actions.Add(new Action("Change Brightness")
            {
                Fields = {"Brightness", "Transition time in seconds"}
            });

            _localClient.DeviceDiscovered += async (s, e) =>
            {
                lock (_deviceLock)
                {
                    var device = _devices.Cast<LifxDevice>().SingleOrDefault(d => d.Id.Equals(e.Id));

                    if (device == null)
                    {
                        device = new LifxDevice(e);
                        _devices.Add(device);
                    }
                }

                await _localClient.GetLightStateAsync(e);
            };

            _localClient.VariableChanged += (s, e) =>
            {
                var device = _devices.Cast<LifxDevice>().SingleOrDefault(d => d.Id.Equals(e.Item1.Id));

                if (device != null)
                {
                    device.Name = e.Item1.Name;
                }

                var variable = $"{Name}.{e.Item1.Id}.{e.Item2}";
                _messageQueue.Publish(new UpdateVariableMessage(variable, e.Item3));
            };
        }

        public override IDevice CreateEmptyDevice()
        {
            throw new NotSupportedException();
        }

        public IEnumerable<LifxDevice> GetDevices()
        {
            return Devices.OfType<LifxDevice>();
        }

        public async void SwitchOn(LifxDevice device, int transitionTimeInSeconds)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Transition time in seconds", transitionTimeInSeconds.ToString()}
            };

            await ExecuteInternal(device, new Action("Switch On"), parameters);
        }

        public async void SwitchOff(LifxDevice device, int transitionTimeInSeconds)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Transition time in seconds", transitionTimeInSeconds.ToString()}
            };

            await ExecuteInternal(device, new Action("Switch Off"), parameters);
        }

        public async void ChangeColor(LifxDevice device, string hexColor, int transitionTimeInSeconds)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Color", hexColor},
                {"Transition time in seconds", transitionTimeInSeconds.ToString()}
            };

            await ExecuteInternal(device, new Action("Change Color"), parameters);
        }

        public async void ChangeBrightness(LifxDevice device, double brightness, int transitionTimeInSeconds)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Brightness", brightness.ToString("F2")},
                {"Transition time in seconds", transitionTimeInSeconds.ToString()}
            };

            await ExecuteInternal(device, new Action("Change Brightness"), parameters);
        }

        public async Task FindBulbsAsync()
        {
            FindLocalBulbs();

            await Task.Delay(TimeSpan.FromSeconds(5));

            await FindCloudBulbs();
        }

        private async Task FindCloudBulbs()
        {
            while (true)
            {
                if (!string.IsNullOrEmpty(_token))
                {
                    try
                    {
                        await GetHttpLights();
                    }
                    catch (Exception e)
                    {
                        _log.Error(e.Message, e);
                    }
                }

                try
                {
                }
                catch (Exception e)
                {
                    _log.Error(e.Message, e);
                }

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        private void FindLocalBulbs()
        {
            try
            {
                _localClient.StartDeviceDiscovery();
            }
            catch (Exception e)
            {
                _log.Error(e.Message, e);
            }
        }

        protected override async Task ExecuteInternal(IDevice device, IAction action, IDictionary<string, string> values)
        {
            if (string.IsNullOrEmpty(_token))
            {
                return;
            }

            var bulb = (LifxDevice) device;
            int seconds;
            double brightness;

            string s;
            if (!action.Fields.Contains("Transition time in seconds") ||
                !values.TryGetValue("Transition time in seconds", out s) ||
                !int.TryParse(s, out seconds))
            {
                seconds = 0;
            }

            string b;
            if (!action.Fields.Contains("Brightness") ||
                !values.TryGetValue("Brightness", out b) ||
                !double.TryParse(b, out brightness))
            {
                brightness = 0;
            }

            string color;
            if (!values.TryGetValue("Color", out color))
            {
                color = string.Empty;
            }

            if (bulb.Source == LifxSource.Cloud)
            {
                await ExecuteCloudAction(bulb, action.Name, seconds, brightness, color);
            }
            else
            {
                await ExecuteLocalAction(bulb, action.Name, seconds, brightness, color);
            }
        }

        private async Task ExecuteLocalAction(LifxDevice device, string action, int seconds, double brightness, string color)
        {
            var light = _localClient.Lights.SingleOrDefault(l => l.Id.Equals(device.Id));
            var b = (ushort)(brightness * 65535);

            if (light == null)
            {
                return;
            }

            switch (action.ToLowerInvariant())
            {
                case "switch on":
                    if (seconds == 0)
                    {
                        await _localClient.SetDevicePowerStateAsync(light, true);
                    }
                    else if (seconds > 0)
                    {
                        await _localClient.SetLightPowerAsync(light, TimeSpan.FromSeconds(seconds), true);
                    }
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "IsOn", true));
                    break;
                case "switch off":
                    if (seconds == 0)
                    {
                        await _localClient.SetDevicePowerStateAsync(light, false);
                    }
                    else if (seconds > 0)
                    {
                        await _localClient.SetLightPowerAsync(light, TimeSpan.FromSeconds(seconds), false);
                    }
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "IsOn", false));
                    break;
                case "change color":
                    var rgb = color.ParseRgb();
                    var hsb = rgb.ToHsbk();
                    var hue = (ushort)hsb.Hue;
                    var saturation = (ushort) (hsb.Saturation*65535);
                    b = (ushort) (hsb.Brightness*65535);
                    ushort kelvin = 4500;
                    await _localClient.SetColorAsync(light, hue, saturation, b, kelvin, TimeSpan.FromSeconds(seconds));
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "IsOn", true));
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Color", rgb.ToString()));
                    break;
                case "change brightness":
                    await _localClient.SetBrightnessAsync(light, b, TimeSpan.FromSeconds(seconds));
                    var db = Math.Round(brightness, 2);
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Brightness", db));
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "IsOn", true));
                    break;
                default:
                    return;
            }
        }

        private async Task ExecuteCloudAction(LifxDevice device, string action, int seconds, double brightness, string color)
        {
            var client = new LifxHttpClient(_token);
            var lights = await client.GetLights();
            var light = lights.SingleOrDefault(l => l.Id.Equals(device.Id));

            if (light == null)
            {
                return;
            }

            switch (action.ToLowerInvariant())
            {
                case "switch on":
                    await client.SwitchOn(light, seconds);
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "IsOn", true));
                    break;
                case "switch off":
                    await client.SwitchOff(light, seconds);
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "IsOn", false));
                    break;
                case "change color":
                    var rgb = color.ParseRgb();
                    await client.ChangeColor(light, rgb.ToString(), seconds);
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "IsOn", true));
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Color", rgb.ToString()));
                    break;
                case "change brightness":
                    await client.ChangeBrightness(light, brightness, seconds);
                    var db = Math.Round(brightness, 2);
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Brightness", db));
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "IsOn", true));
                    break;
                default:
                    return;
            }
        }

        private async Task GetHttpLights()
        {
            var client = new LifxHttpClient(_token);
            var lights = await client.GetLights();

            foreach (var light in lights)
            {
                LifxDevice device;

                lock (_deviceLock)
                {
                    device = _devices.Cast<LifxDevice>().SingleOrDefault(d => d.Id.Equals(light.Id));

                    if (device == null)
                    {
                        device = new LifxDevice(light);
                        _devices.Add(device);
                    }
                }

                UpdateDeviceVariables(device, light);
            }
        }

        private void UpdateDeviceVariables(LifxDevice device, LifxHttpLight light)
        {
            var brightness = Math.Round(light.Brightness, 2);
            var groupName = light.Group.Name;
            var name = light.Label;
            var isOn = light.Power == LifxHttpLight.PowerState.On;
            var isConnected = light.IsConnected;
            var color = light.GetHexColor();

            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Brightness", brightness));
            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "IsOn", isOn));
            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "IsConnected", isConnected));
            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Name", name));
            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "GroupName", groupName));
            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Color", color));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _localClient.Dispose();
            }
        }
    }
}
