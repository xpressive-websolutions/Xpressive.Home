using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Xpressive.Home.Contracts;
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
        private readonly AutoResetEvent _taskWaitHandle = new AutoResetEvent(false);
        private ZWaveController _controller;
        private bool _isRunning;

        public ZwaveGateway(IMessageQueue messageQueue, IList<ICommandClassHandler> commandClassHandlers)
            : base("zwave")
        {
            _messageQueue = messageQueue;
            _commandClassHandlers = commandClassHandlers;
            _canCreateDevices = false;
            _isRunning = true;

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

        public override async Task StartAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(1));

            if (string.IsNullOrEmpty(_comPortName))
            {
                _messageQueue.Publish(new NotifyUserMessage("Add z-wave configuration (port) to config file."));
                _taskWaitHandle.Set();
                return;
            }

            var validComPorts = new HashSet<string>(SerialPort.GetPortNames(), StringComparer.Ordinal);
            if (!validComPorts.Contains(_comPortName))
            {
                _messageQueue.Publish(new NotifyUserMessage("COM Port for z-wave configuration is invalid."));
                _taskWaitHandle.Set();
                return;
            }

            try
            {
                await _library.Load();

                _controller = new ZWaveController(_comPortName);
                _controller.Open();
                _controller.Channel.NodeEventReceived += (s, e) => ContinueNodeQueueWorker(e.NodeID);

                while (_isRunning)
                {
                    _log.Debug("Discover Nodes");
                    await DiscoverNodes();

                    await TaskHelper.DelayAsync(TimeSpan.FromHours(1), () => _isRunning);
                }
            }
            catch (Exception e)
            {
                _log.Error(e.Message, e);
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

        private async Task DiscoverNodes()
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
                    queue.Add("GetSupportedCommandClasses", () => GetSupportedCommandClasses(device, node));

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

            device.ManufacturerId = specific.ManufacturerID.ToString("x4");
            device.ProductType = specific.ProductType.ToString("x4");
            device.ProductId = specific.ProductID.ToString("x4");

            UpdateDeviceWithLibrary(device);
        }

        private void UpdateDeviceWithLibrary(ZwaveDevice device)
        {
            if (device.Application == null ||
                device.Library == null ||
                device.Protocol == null ||
                device.ManufacturerId == null ||
                device.ProductType == null ||
                device.ProductId == null)
            {
                return;
            }

            ZwaveDeviceLibraryResolver.Resolve(_library, device);

            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Manufacturer", device.Manufacturer));
            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "ProductName", device.ProductName));
            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "Description", device.ProductDescription));
        }

        private async Task GetSupportedCommandClasses(ZwaveDevice device, Node node)
        {
            var commandClasses = await node.GetSupportedCommandClasses();
            var handlers = _commandClassHandlers.ToDictionary(h => h.CommandClass);

            _log.Debug($"Node {node.NodeID} supports {commandClasses.Length} command classes.");

            foreach (var commandClassReport in commandClasses)
            {
                ICommandClassHandler handler;
                if (handlers.TryGetValue(commandClassReport.Class, out handler))
                {
                    handler.Handle(device, node, _nodeCommandQueues[node.NodeID]);
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
            _isRunning = false;

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
                _taskWaitHandle.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
