using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Polly;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.CommandClasses;
using Action = Xpressive.Home.Contracts.Gateway.Action;
using Version = ZWave.CommandClasses.Version;

namespace Xpressive.Home.Plugins.Zwave
{
    public class ZwaveGateway : GatewayBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ZwaveGateway));
        private readonly IMessageQueue _messageQueue;
        private readonly IList<ICommandClassHandler> _commandClassHandlers;
        private readonly string _comPortName;
        private readonly ZwaveDeviceLibrary _library;
        private readonly Dictionary<byte, ZwaveCommandQueue> _nodeCommandQueues;
        private ZWaveController _controller;

        public ZwaveGateway(IMessageQueue messageQueue, IList<ICommandClassHandler> commandClassHandlers)
            : base("zwave")
        {
            _messageQueue = messageQueue;
            _commandClassHandlers = commandClassHandlers;
            _canCreateDevices = false;

            _comPortName = ConfigurationManager.AppSettings["zwave.port"];
            _library = new ZwaveDeviceLibrary();
            _nodeCommandQueues = new Dictionary<byte, ZwaveCommandQueue>();
        }

        public override IDevice CreateEmptyDevice()
        {
            throw new NotSupportedException();
        }

        protected override async Task ExecuteInternalAsync(IDevice device, IAction action, IDictionary<string, string> values)
        {
            if (device == null)
            {
                _log.Warn($"Unable to execute action {action.Name} because the device was not found.");
                return;
            }

            var d = (ZwaveDevice)device;
            var nodes = await _controller.GetNodes();
            var node = nodes[d.NodeId];

            if (d.IsSwitchBinary && action.Name.Equals("Switch On", StringComparison.Ordinal))
            {
                await node.GetCommandClass<SwitchBinary>().Set(true);
            }
            else if (d.IsSwitchBinary && action.Name.Equals("Switch Off", StringComparison.Ordinal))
            {
                await node.GetCommandClass<SwitchBinary>().Set(false);
            }
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

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { });

            if (string.IsNullOrEmpty(_comPortName))
            {
                _messageQueue.Publish(new NotifyUserMessage("Add z-wave configuration (port) to config file."));
                return;
            }

            var validComPorts = new HashSet<string>(SerialPort.GetPortNames(), StringComparer.Ordinal);
            if (!validComPorts.Contains(_comPortName))
            {
                _messageQueue.Publish(new NotifyUserMessage("COM Port for z-wave configuration is invalid."));
                return;
            }

            try
            {
                await Policy
                    .Handle<WebException>()
                    .Or<Exception>()
                    .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(1))
                    .ExecuteAsync(async () =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                        await _library.Load(cancellationToken);
                    });

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                _controller = new ZWaveController(_comPortName);
                _controller.Open();
                _controller.Channel.NodeEventReceived += (s, e) => ContinueNodeQueueWorker(e.NodeID);

                while (!cancellationToken.IsCancellationRequested)
                {
                    _log.Debug("Discover Nodes");
                    await DiscoverNodes(cancellationToken);
                    await Task.Delay(TimeSpan.FromHours(1), cancellationToken).ContinueWith(_ => { });
                }
            }
            catch (Exception e)
            {
                _log.Error(e.Message, e);
            }
        }

        private async Task DiscoverNodes(CancellationToken cancellationToken)
        {
            var controllerNodeId = await _controller.GetNodeID();
            var nodes = await _controller.DiscoverNodes();
            var slaveNodes = nodes.Where(n => n.NodeID != controllerNodeId).ToList();

            foreach (var node in slaveNodes)
            {
                if (!_nodeCommandQueues.ContainsKey(node.NodeID))
                {
                    var device = new ZwaveDevice(node.NodeID);
                    var queue = new ZwaveCommandQueue(node);
                    _nodeCommandQueues.Add(node.NodeID, queue);
                    _devices.Add(device);

                    queue.Add("UpdateDeviceProtocolInfo", () => UpdateDeviceProtocolInfo(device, node));
                    queue.Add("GetNodeVersion", () => GetNodeVersion(device, node));
                    queue.Add("GetNodeProductInformation", () => GetNodeProductInformation(device, node));
                    queue.Add("GetSupportedCommandClasses", () => GetSupportedCommandClasses(device, node, cancellationToken));

                    queue.StartOrContinueWorker();
                }
            }
        }

        private async Task UpdateDeviceProtocolInfo(ZwaveDevice device, Node node)
        {
            var protocolInfo = await node.GetProtocolInfo();
            device.BasicType = (byte)protocolInfo.BasicType;
            device.GenericType = (byte)protocolInfo.GenericType;
            device.SpecificType = protocolInfo.SpecificType;
        }

        private async Task GetNodeVersion(ZwaveDevice device, Node node)
        {
            var version = await node.GetCommandClass<Version>().Get();

            device.Application = version.Application;
            device.Library = version.Library;
            device.Protocol = version.Protocol;

            UpdateDeviceWithLibrary(device);
        }

        private async Task GetNodeProductInformation(ZwaveDevice device, Node node)
        {
            var specific = await node.GetCommandClass<ManufacturerSpecific>().Get();

            device.ManufacturerId = specific.ManufacturerID;
            device.ProductType = specific.ProductType;
            device.ProductId = specific.ProductID;

            UpdateDeviceWithLibrary(device);
        }

        private void UpdateDeviceWithLibrary(ZwaveDevice device)
        {
            if (device.Application == null ||
                device.Library == null ||
                device.Protocol == null ||
                device.ManufacturerId == 0 ||
                device.ProductType == 0 ||
                device.ProductId == 0)
            {
                return;
            }

            ZwaveDeviceLibraryResolver.Resolve(_library, device);

            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Manufacturer", device.Manufacturer));
            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "ProductName", device.ProductName));
            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Description", device.ProductDescription));

            if (string.IsNullOrEmpty(device.Name))
            {
                device.Name = device.ProductName;
            }
        }

        private async Task GetSupportedCommandClasses(ZwaveDevice device, Node node, CancellationToken cancellationToken)
        {
            var commandClasses = await node.GetSupportedCommandClasses();
            var handlers = _commandClassHandlers.ToDictionary(h => h.CommandClass);

            _log.Debug($"Node {node.NodeID} supports {commandClasses.Length} command classes.");

            foreach (var commandClassReport in commandClasses)
            {
                ICommandClassHandler handler;
                if (handlers.TryGetValue(commandClassReport.Class, out handler))
                {
                    handler.Handle(device, node, _nodeCommandQueues[node.NodeID], cancellationToken);
                }
                else
                {
                    _log.Warn($"No CommandClassHandler for CommandClass {commandClassReport.Class} found.");
                }
            }
        }

        private void ContinueNodeQueueWorker(byte nodeId)
        {
            ZwaveCommandQueue queue;
            if (_nodeCommandQueues.TryGetValue(nodeId, out queue))
            {
                queue.StartOrContinueWorker();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var commandQueue in _nodeCommandQueues)
                {
                    commandQueue.Value.Dispose();
                }

                foreach (var commandClassHandler in _commandClassHandlers)
                {
                    commandClassHandler.Dispose();
                }

                _controller?.Close();
            }

            base.Dispose(disposing);
        }
    }
}
