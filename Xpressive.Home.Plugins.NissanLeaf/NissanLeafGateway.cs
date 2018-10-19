using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using Action = Xpressive.Home.Contracts.Gateway.Action;

namespace Xpressive.Home.Plugins.NissanLeaf
{
    internal sealed class NissanLeafGateway : GatewayBase, INissanLeafGateway
    {
        private readonly INissanLeafClient _nissanLeafClient;
        private readonly IMessageQueue _messageQueue;
        private readonly string _username;
        private readonly string _password;

        public NissanLeafGateway(INissanLeafClient nissanLeafClient, IMessageQueue messageQueue, IConfiguration configuration)
            : base("NissanLeaf", false)
        {
            _nissanLeafClient = nissanLeafClient;
            _messageQueue = messageQueue;
            _username = configuration["nissanleaf.username"];
            _password = configuration["nissanleaf.password"];
        }

        public IEnumerable<NissanLeafDevice> GetDevices()
        {
            return Devices.Cast<NissanLeafDevice>();
        }

        public void StartCharging(NissanLeafDevice device)
        {
            var action = GetActions(device).Single(a => a.Name.Equals("Start charging", StringComparison.Ordinal));
            StartActionInNewTask(device, action, new Dictionary<string, string>());
        }

        public void StartClimateControl(NissanLeafDevice device)
        {
            var action = GetActions(device).Single(a => a.Name.Equals("Start climate contro", StringComparison.Ordinal));
            StartActionInNewTask(device, action, new Dictionary<string, string>());
        }

        public void StopClimateControl(NissanLeafDevice device)
        {
            var action = GetActions(device).Single(a => a.Name.Equals("Stop climate control", StringComparison.Ordinal));
            StartActionInNewTask(device, action, new Dictionary<string, string>());
        }

        public override IEnumerable<IAction> GetActions(IDevice device)
        {
            yield return new Action("Start charging");
            //yield return new Action("Stop charging");
            yield return new Action("Start climate control");
            yield return new Action("Stop climate control");
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { });

            if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password))
            {
                _messageQueue.Publish(new NotifyUserMessage("Add nissan leaf configuration to config file."));
                return;
            }

            var isInit = await _nissanLeafClient.InitAsync();

            if (!isInit)
            {
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                var foundDevices = await _nissanLeafClient.LoginAsync(_username, _password);

                foreach (var foundDevice in foundDevices)
                {
                    if (DeviceDictionary.TryGetValue(foundDevice.Id, out var d) && d is NissanLeafDevice existingDevice)
                    {
                        existingDevice.CustomSessionId = foundDevice.CustomSessionId;
                    }
                    else
                    {
                        DeviceDictionary.TryAdd(foundDevice.Id, foundDevice);
                    }
                }

                foreach (var device in Devices.Cast<NissanLeafDevice>())
                {
                    var batteryStatus = await _nissanLeafClient.GetBatteryStatusAsync(device, cancellationToken);

                    if (batteryStatus == null || cancellationToken.IsCancellationRequested)
                    {
                        continue;
                    }

                    device.ChargingState = batteryStatus.ChargingState;
                    device.PluginState = batteryStatus.PluginState;
                    device.Power = Math.Round(batteryStatus.Power, 2);
                    device.CruisingRangeAcOn = Math.Round(batteryStatus.CruisingRangeAcOn);
                    device.CruisingRangeAcOff = Math.Round(batteryStatus.CruisingRangeAcOff);

                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "ChargingState", device.ChargingState));
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "PluginState", device.PluginState));
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Power", device.Power, "Percent"));
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "CruisingRangeAcOff", device.CruisingRangeAcOff, "Meter"));
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "CruisingRangeAcOn", device.CruisingRangeAcOn, "Meter"));
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken).ContinueWith(_ => { });
        }

        public override IDevice CreateEmptyDevice()
        {
            throw new NotSupportedException();
        }

        protected override async Task ExecuteInternalAsync(IDevice device, IAction action, IDictionary<string, string> values)
        {
            var leaf = device as NissanLeafDevice;

            if (leaf == null)
            {
                return;
            }

            switch (action.Name.ToLowerInvariant())
            {
                case "start charging":
                    await _nissanLeafClient.StartCharging(leaf);
                    break;
                case "start climate control":
                    await _nissanLeafClient.ActivateClimateControl(leaf);
                    break;
                case "stop climate control":
                    await _nissanLeafClient.DeactivateClimateControl(leaf);
                    break;
            }
        }
    }
}
