using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Configuration;
using Polly;
using Serilog;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Services;
using Action = Xpressive.Home.Contracts.Gateway.Action;

namespace Xpressive.Home.Plugins.Lifx
{
    internal sealed class LifxGateway : GatewayBase, ILifxGateway
    {
        private readonly IMessageQueue _messageQueue;
        private readonly IDeviceConfigurationBackupService _deviceConfigurationBackupService;
        private readonly string _token;
        private readonly object _deviceLock = new object();
        private readonly LifxLocalClient _localClient = new LifxLocalClient();

        public LifxGateway(IMessageQueue messageQueue, IDeviceConfigurationBackupService deviceConfigurationBackupService, IConfiguration configuration)
            : base("Lifx", false)
        {
            _messageQueue = messageQueue;
            _deviceConfigurationBackupService = deviceConfigurationBackupService;
            _token = configuration["lifx.token"];

            _messageQueue.Subscribe<CommandMessage>(Notify);

            _localClient.DeviceDiscovered += (s, e) =>
            {
                AddLifxDevice(e.Id, () => new LifxDevice(e));
            };

            _localClient.VariableChanged += (s, e) =>
            {
                if (DeviceDictionary.TryGetValue(e.Item1.Id, out var d) && d is LifxDevice device)
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

        public override IEnumerable<IAction> GetActions(IDevice device)
        {
            if (device is LifxDevice)
            {
                yield return new Action("Switch On") { Fields = { "Transition time in seconds" } };
                yield return new Action("Switch Off") { Fields = { "Transition time in seconds" } };
                yield return new Action("Change Color") { Fields = { "Color", "Transition time in seconds" } };
                yield return new Action("Change Brightness") { Fields = { "Brightness", "Transition time in seconds" } };
            }
        }

        public void SwitchOn(LifxDevice device, int transitionTimeInSeconds)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Transition time in seconds", transitionTimeInSeconds.ToString()}
            };

            var action = GetActions(device).Single(a => a.Name.Equals("Switch On", StringComparison.Ordinal));
            StartActionInNewTask(device, action, parameters);
        }

        public void SwitchOff(LifxDevice device, int transitionTimeInSeconds)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Transition time in seconds", transitionTimeInSeconds.ToString()}
            };

            var action = GetActions(device).Single(a => a.Name.Equals("Switch Off", StringComparison.Ordinal));
            StartActionInNewTask(device, action, parameters);
        }

        public void ChangeColor(LifxDevice device, string hexColor, int transitionTimeInSeconds)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Color", hexColor},
                {"Transition time in seconds", transitionTimeInSeconds.ToString()}
            };

            var action = GetActions(device).Single(a => a.Name.Equals("Change Color", StringComparison.Ordinal));
            StartActionInNewTask(device, action, parameters);
        }

        public void ChangeBrightness(LifxDevice device, double brightness, int transitionTimeInSeconds)
        {
            var parameters = new Dictionary<string, string>
            {
                {"Brightness", brightness.ToString("F2")},
                {"Transition time in seconds", transitionTimeInSeconds.ToString()}
            };

            var action = GetActions(device).Single(a => a.Name.Equals("Change Brightness", StringComparison.Ordinal));
            StartActionInNewTask(device, action, parameters);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { });

            FindLocalBulbs(cancellationToken);
            FindCloudBulbsAsync(cancellationToken);

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { });

            var backupDto = _deviceConfigurationBackupService.Get<LocalLifxLightConfigurationBackupDto>(Name);
            if (backupDto != null)
            {
                foreach (var ipAddress in backupDto.LocalIpAddresses)
                {
                    var temporaryDevice = new LifxLocalLight
                    {
                        Endpoint = new IPEndPoint(IPAddress.Parse(ipAddress), 56700)
                    };

                    await _localClient.GetLightStateAsync(temporaryDevice);
                }
            }
        }

        private async Task FindCloudBulbsAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_token))
            {
                _messageQueue.Publish(new NotifyUserMessage("Add LIFX cloud token to config file."));
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                await ExecuteWithRetriesAsync(GetHttpLights, "get cloud bulbs");

                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken).ContinueWith(_ => { });
            }
        }

        private void FindLocalBulbs(CancellationToken cancellationToken)
        {
            try
            {
                _localClient.StartLifxNetwork(cancellationToken);
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
            }
        }

        protected override async Task ExecuteInternalAsync(IDevice device, IAction action, IDictionary<string, string> values)
        {
            if (string.IsNullOrEmpty(_token))
            {
                return;
            }

            if (device == null)
            {
                Log.Warning("Unable to execute action {actionName} because the device was not found.", action.Name);
                return;
            }

            var bulb = (LifxDevice)device;
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
                var description = $"action {action.Name} for cloud bulb {bulb.Name}";
                await ExecuteWithRetriesAsync(() => ExecuteCloudAction(bulb, action.Name, seconds, brightness, color), description);
            }
            else
            {
                var description = $"action {action.Name} for local bulb {bulb.Name}";
                await ExecuteWithRetriesAsync(() => ExecuteLocalAction(bulb, action.Name, seconds, brightness, color), description);
            }
        }

        private async Task ExecuteWithRetriesAsync(Func<Task> func, string description)
        {
            try
            {
                var policy = Policy
                    .Handle<Exception>()
                    .WaitAndRetryAsync(new[]
                    {
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(5)
                    });

                await policy.ExecuteAsync(async () => await func());
            }
            catch (WebException e)
            {
                Log.Error(e, "Error while executing {description}.", description);
            }
            catch (XmlException e)
            {
                Log.Error(e, "Error while executing {description}.", description);
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
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

            var hsbk = light.Color;
            if (hsbk == null)
            {
                hsbk = new HsbkColor
                {
                    Kelvin = 4500
                };
            }

            switch (action.ToLowerInvariant())
            {
                case "switch on":
                    if (seconds == 0)
                    {
                        await _localClient.SetPowerAsync(light, true);
                    }
                    else if (seconds > 0)
                    {
                        await _localClient.SetPowerAsync(light, TimeSpan.FromSeconds(seconds), true);
                    }
                    //_messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "IsOn", true));
                    break;
                case "switch off":
                    if (seconds == 0)
                    {
                        await _localClient.SetPowerAsync(light, false);
                    }
                    else if (seconds > 0)
                    {
                        await _localClient.SetPowerAsync(light, TimeSpan.FromSeconds(seconds), false);
                    }
                    //_messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "IsOn", false));
                    break;
                case "change color":
                    var rgb = color.ParseRgb();
                    var hsb = rgb.ToHsbk();

                    hsbk.Hue = hsb.Hue;
                    hsbk.Saturation = hsb.Saturation;
                    hsbk.Brightness = hsb.Brightness;

                    await _localClient.SetColorAsync(light, hsbk, TimeSpan.FromSeconds(seconds));
                    //_messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "IsOn", true));
                    //_messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Color", rgb.ToString()));
                    break;
                case "change brightness":
                    hsbk.Brightness = brightness;

                    await _localClient.SetColorAsync(light, hsbk, TimeSpan.FromSeconds(seconds));
                    //var db = Math.Round(brightness, 2);
                    //_messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Brightness", db));
                    //_messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "IsOn", true));
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
                var device = AddLifxDevice(light?.Id, () => new LifxDevice(light));

                if (device != null)
                {
                    UpdateDeviceVariables(device, light);
                }
            }
        }

        private LifxDevice AddLifxDevice(string id, Func<LifxDevice> create)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            lock (_deviceLock)
            {
                if (!DeviceDictionary.TryGetValue(id, out var d) || !(d is LifxDevice device))
                {
                    device = create();
                    DeviceDictionary.TryAdd(device.Id, device);
                }
                return device;
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
                var ipAddresses = _localClient.Lights.Select(l => l.Endpoint.Address.ToString()).ToList();
                var backupDto = new LocalLifxLightConfigurationBackupDto(ipAddresses);
                _deviceConfigurationBackupService.Save(Name, backupDto);

                _localClient.Dispose();
            }
        }

        private class LocalLifxLightConfigurationBackupDto
        {
            public LocalLifxLightConfigurationBackupDto(IEnumerable<string> ipAddresses)
            {
                if (ipAddresses == null)
                {
                    LocalIpAddresses = new List<string>(0);
                }
                else
                {
                    LocalIpAddresses = new List<string>(ipAddresses);
                }
            }

            public List<string> LocalIpAddresses { get; }
        }
    }
}
