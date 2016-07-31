using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.CommandClasses;
using Version = ZWave.CommandClasses.Version;

namespace Xpressive.Home.Plugins.Zwave
{
    public class ZwaveGateway : GatewayBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof (ZwaveGateway));
        private readonly IMessageQueue _messageQueue;
        private readonly IList<ICommandClassHandler> _commandClassHandlers;
        private readonly string _comPortName;
        private readonly ZwaveDeviceLibrary _library;
        private readonly Dictionary<byte, ZwaveCommandQueue> _nodeCommandQueues;
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

        protected override Task ExecuteInternal(IDevice device, IAction action, IDictionary<string, string> values)
        {
            throw new NotImplementedException();
        }

        internal async Task Start()
        {
            if (string.IsNullOrEmpty(_comPortName))
            {
                _messageQueue.Publish(new NotifyUserMessage("Add z-wave configuration (port) to config file."));
                return;
            }
            
            await _library.Load();

            _controller = new ZWaveController(_comPortName);
            _controller.Open();
            _controller.Channel.NodeEventReceived += (s, e) => ContinueNodeQueueWorker(e.NodeID);

            var lastNodeDiscovery = DateTime.MinValue;

            while (_isRunning)
            {
                await Task.Delay(10);

                if ((DateTime.UtcNow - lastNodeDiscovery).TotalHours > 1)
                {
                    lastNodeDiscovery = DateTime.UtcNow;
                    _log.Debug("Discover Nodes");
                    await DiscoverNodes();
                }
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
                    var queue = new ZwaveCommandQueue(device, node);
                    _nodeCommandQueues.Add(node.NodeID, queue);
                    _devices.Add(device);

                    queue.Add("UpdateDeviceProtocolInfo", () => UpdateDeviceProtocolInfo(device, node));
                    queue.Add("InitializeDevice", () => InitializeDevice(device, node));
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

        private async Task InitializeDevice(ZwaveDevice device, Node node)
        {
            if (device.IsInitialized)
            {
                return;
            }
            
            var version = await node.GetCommandClass<Version>().Get();
            var specific = await node.GetCommandClass<ManufacturerSpecific>().Get();

            device.Application = version.Application;
            device.Library = version.Library;
            device.Protocol = version.Protocol;
            device.ManufacturerId = specific.ManufacturerID.ToString("x4");
            device.ProductType = specific.ProductType.ToString("x4");
            device.ProductId = specific.ProductID.ToString("x4");

            UpdateDeviceWithLibrary(device);
            device.IsInitialized = true;
        }

        private void UpdateDeviceWithLibrary(ZwaveDevice device)
        {
            ZwaveDeviceLibraryResolver.Resolve(_library, device);

            _log.Debug($"Node {device.Id} Manufacturer: {device.Manufacturer}");
            _log.Debug($"Node {device.Id} Product Name: {device.ProductName}");
            _log.Debug($"Node {device.Id} Description: {device.ProductDescription}");
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
                _log.Debug($"Continue worker for node {nodeId}");
                queue.StartOrContinueWorker();
            }
        }

        protected override void Dispose(bool disposing)
        {
            _isRunning = false;

            foreach (var commandQueue in _nodeCommandQueues)
            {
                commandQueue.Value.Dispose();
            }

            foreach (var commandClassHandler in _commandClassHandlers)
            {
                commandClassHandler.Dispose();
            }

            _controller.Close();
            base.Dispose(disposing);
        }
    }
}
