using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Polly;
using Serilog;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;
using Action = Xpressive.Home.Contracts.Gateway.Action;

namespace Xpressive.Home.Plugins.Zwave
{
    public class ZwaveGateway : GatewayBase
    {
        private readonly ZWaveController _controller;
        private readonly IMessageQueue _messageQueue;
        private readonly string _comPortName;
        private readonly IAsyncPolicy _policy;

        public ZwaveGateway(IMessageQueue messageQueue, IConfiguration configuration)
            : base("zwave", false)
        {
            _messageQueue = messageQueue;
            _comPortName = configuration["zwave.port"];

            _messageQueue.Subscribe<CommandMessage>(Notify);

            _policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(10, attempt => TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 10)));

            if (string.IsNullOrEmpty(_comPortName))
            {
                _messageQueue.Publish(new NotifyUserMessage("Add Z-Wave configuration to config file."));
            }
        }

        public override IDevice CreateEmptyDevice()
        {
            throw new NotSupportedException();
        }

        public override IEnumerable<IAction> GetActions(IDevice device)
        {
            var d = device as ZwaveDevice;
            if (d == null)
            {
                yield break;
            }

            if (d.IsSwitchBinary)
            {
                yield return new Action("Switch On");
                yield return new Action("Switch Off");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { });

            if (string.IsNullOrEmpty(_comPortName))
            {
                return;
            }

            try
            {
                _controller.Open();

                var homeId = await GetControllerHomeId(_controller);

                while (!cancellationToken.IsCancellationRequested)
                {
                    var nodes = await GetNodes(_controller);

                    foreach (var node in nodes)
                    {
                        if (!DeviceDictionary.TryGetValue(node.NodeID.ToString("D"), out var d) || !(d is ZwaveDevice device))
                        {
                            device = new ZwaveDevice(node.NodeID, homeId);
                            DeviceDictionary.TryRemove(device.Id, out _);
                            DeviceDictionary.TryAdd(device.Id, device);

                            node.MessageReceived += async (s, e) =>
                            {
                                try
                                {
                                    await GetSupportedClasses(node, device, cancellationToken);
                                }
                                catch
                                {
                                }
                            };
                        }
                    }

                    await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken);
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
            }
            finally
            {
                _controller.Close();
            }
        }

        private async Task GetSupportedClasses(Node node, ZwaveDevice device, CancellationToken cancellationToken)
        {
            if (device.CommandClasses != null)
            {
                return;
            }

            try
            {
                await device.Semaphore.WaitAsync(cancellationToken);

                if (device.CommandClasses != null)
                {
                    return;
                }

                var classReports = await node.GetSupportedCommandClasses(cancellationToken);
                device.CommandClasses = classReports.Select(c => c.Class).ToList();
            }
            catch
            {
                Log.Information("Unable to get command classes for node {nodeId}", device.NodeId);
            }
            finally
            {
                device.Semaphore.Release();
            }

            if (device.CommandClasses == null)
            {
                return;
            }

            foreach (var commandClass in device.CommandClasses)
            {
                switch (commandClass)
                {
                    case CommandClass.Alarm:
                        node.GetCommandClass<Alarm>().Changed += (s, e) => UpdateVariables(e.Report);
                        break;
                    case CommandClass.WakeUp:
                        node.GetCommandClass<WakeUp>().Changed += (s, e) => UpdateVariables(e.Report);
                        break;
                    case CommandClass.Basic:
                        node.GetCommandClass<Basic>().Changed += (s, e) => UpdateVariables(e.Report);
                        break;
                    case CommandClass.Battery:
                        node.GetCommandClass<Battery>().Changed += (s, e) => UpdateVariables(e.Report);
                        break;
                    case CommandClass.CentralScene:
                        node.GetCommandClass<CentralScene>().Changed += (s, e) => UpdateVariables(e.Report);
                        break;
                    case CommandClass.Meter:
                        node.GetCommandClass<Meter>().Changed += (s, e) => UpdateVariables(e.Report);
                        break;
                    case CommandClass.SwitchBinary:
                        var switchBinary = node.GetCommandClass<SwitchBinary>();
                        switchBinary.Changed += (s, e) => UpdateVariables(e.Report);
                        UpdateVariables(await switchBinary.Get(cancellationToken));
                        break;
                    case CommandClass.SwitchMultiLevel:
                        node.GetCommandClass<SwitchMultiLevel>().Changed += (s, e) => UpdateVariables(e.Report);
                        break;
                    case CommandClass.SceneActivation:
                        node.GetCommandClass<SceneActivation>().Changed += (s, e) => UpdateVariables(e.Report);
                        break;
                    case CommandClass.SensorBinary:
                        var sensorBinary = node.GetCommandClass<SensorBinary>();
                        sensorBinary.Changed += (s, e) => UpdateVariables(e.Report);
                        UpdateVariables(await sensorBinary.Get(cancellationToken));
                        break;
                    case CommandClass.SensorMultiLevel:
                        node.GetCommandClass<SensorMultiLevel>().Changed += (s, e) => UpdateVariables(e.Report);
                        break;
                    case CommandClass.ThermostatSetpoint:
                        node.GetCommandClass<ThermostatSetpoint>().Changed += (s, e) => UpdateVariables(e.Report);
                        break;
                    case CommandClass.SensorAlarm:
                        node.GetCommandClass<SensorAlarm>().Changed += (s, e) => UpdateVariables(e.Report);
                        break;
                }
            }
        }

        private void UpdateVariables(NodeReport report)
        {
            var type = report.GetType();

            var fields = type
                .GetFields()
                .Where(f => f.IsPublic)
                .Where(f => f.DeclaringType == type)
                .ToList();

            foreach (var field in fields)
            {
                var value = field.GetValue(report);
                var s = value.ToString();

                if (double.TryParse(s, out var d))
                {
                    _messageQueue.Publish(new UpdateVariableMessage(Name, report.Node.NodeID.ToString("D"), field.Name, Math.Round(d, 10000)));
                }
                else if (bool.TryParse(s, out var b))
                {
                    _messageQueue.Publish(new UpdateVariableMessage(Name, report.Node.NodeID.ToString("D"), field.Name, b));
                }
                else
                {
                    _messageQueue.Publish(new UpdateVariableMessage(Name, report.Node.NodeID.ToString("D"), field.Name, s));
                }
            }
        }

        private async Task<NodeCollection> GetNodes(ZWaveController controller)
        {
            return await _policy.ExecuteAsync(async () =>
            {
                var nodes = await controller.GetNodes();
                return nodes;
            });
        }

        private async Task<uint> GetControllerHomeId(ZWaveController controller)
        {
            return await _policy.ExecuteAsync(async () =>
            {
                var homeId = await controller.GetHomeID();
                Log.Information($"Version: {await controller.GetVersion()}");
                Log.Information($"HomeID: {homeId:X}");
                Log.Information($"ControllerID: {await controller.GetNodeID():D3}");
                return homeId;
            });
        }

        protected override async Task ExecuteInternalAsync(IDevice device, IAction action, IDictionary<string, string> values)
        {
            if (device == null || !(device is ZwaveDevice d))
            {
                Log.Warning("Unable to execute action {actionName} because the device was not found.", action.Name);
                return;
            }

            var nodes = await _controller.GetNodes();
            var node = nodes.SingleOrDefault(n => n.NodeID == d.NodeId);

            if (node == null)
            {
                Log.Warning("Unable to execute action {actionName} because the node {nodeId} was not found.", action.Name, d.NodeId);
                return;
            }

            switch (action.Name)
            {
                case "Switch On":
                    if (d.IsSwitchBinary)
                    {
                        await node.GetCommandClass<SwitchBinary>().Set(true);
                    }
                    break;
                case "Switch Off":
                    if (d.IsSwitchBinary)
                    {
                        await node.GetCommandClass<SwitchBinary>().Set(false);
                    }
                    break;
            }
        }
    }
}
