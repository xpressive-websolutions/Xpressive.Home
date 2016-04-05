using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using LifxHttp;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using Action = Xpressive.Home.Contracts.Gateway.Action;

namespace Xpressive.Home.Plugins.Lifx
{
    internal sealed class LifxGateway : GatewayBase
    {
        private readonly IMessageQueue _messageQueue;
        private readonly string _token;
        private readonly object _deviceLock = new object();

        public LifxGateway(IMessageQueue messageQueue) : base("Lifx")
        {
            _messageQueue = messageQueue;
            _canCreateDevices = false;
            _token = ConfigurationManager.AppSettings["lifx.token"];

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
        }

        public override IDevice CreateEmptyDevice()
        {
            throw new NotSupportedException();
        }

        public async Task FindBulbsAsync()
        {
            if (string.IsNullOrEmpty(_token))
            {
                return;
            }

            while (true)
            {
                var client = new LifxClient(_token);
                var lights = await client.ListLights();

                foreach (var light in lights.Where(l => l.IsConnected))
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

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        protected override async Task ExecuteInternal(IDevice device, IAction action, IDictionary<string, string> values)
        {
            if (string.IsNullOrEmpty(_token))
            {
                return;
            }

            var client = new LifxClient(_token);
            var bulb = (LifxDevice)device;
            var lights = await client.ListLights();
            var light = lights.SingleOrDefault(l => l.Id.Equals(bulb.Id));
            int seconds;
            int brightness;

            if (light == null)
            {
                return;
            }

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
                !int.TryParse(b, out brightness))
            {
                brightness = (int) light.Color.Brightness*100;
            }

            var color = light.Color;

            switch (action.Name.ToLowerInvariant())
            {
                case "switch on":
                    await client.SetPower(light, true, seconds);
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "IsOn", true));
                    break;
                case "switch off":
                    await client.SetPower(light, false, seconds);
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "IsOn", false));
                    break;
                case "change color":
                    var rgb = values["Color"].ParseRgb();
                    await client.SetColor(light, rgb, seconds);
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "IsOn", true));
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Color", rgb.ToString()));
                    break;
                case "change brightness":
                    color = new LifxColor.HSBK(color.Hue, color.Saturation, brightness / 100f, color.Kelvin);
                    await client.SetColor(light, color, seconds);
                    var db = Math.Round((double)brightness, 0);
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Brightness", db));
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "IsOn", true));
                    break;
                default:
                    return;
            }
        }

        private void UpdateDeviceVariables(LifxDevice device, Light light)
        {
            var brightness = Math.Round(light.Brightness * 100d, 0);
            var groupName = light.GroupName;
            var name = light.Label;
            var isOn = light.PowerState == PowerState.On;
            var color = light.Color.ToRgb().ToString();

            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Brightness", brightness));
            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "IsOn", isOn));
            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Name", name));
            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "GroupName", groupName));
            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Color", color));
        }
    }
}