using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel.Protocol;
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
        private readonly Dictionary<byte, BlockingCollection<NodeCommand>> _nodeCommandQueues;
        private readonly Dictionary<byte, SemaphoreSlim> _nodeCommandSemaphores;
        private readonly Dictionary<byte, DateTime> _lastSemaphoreRelease;
        private readonly object _lastSemaphoreReleaseLock = new object();
        private readonly List<Task> _nodeCommandTasks;
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
            _nodeCommandQueues = new Dictionary<byte, BlockingCollection<NodeCommand>>();
            _nodeCommandSemaphores = new Dictionary<byte, SemaphoreSlim>();
            _lastSemaphoreRelease = new Dictionary<byte, DateTime>();
            _nodeCommandTasks = new List<Task>();
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
            _controller.Channel.NodeEventReceived += (s, e) => ReleaseSemaphore(e.NodeID);

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
                    var queue = new BlockingCollection<NodeCommand>();
                    _nodeCommandQueues.Add(node.NodeID, queue);
                    _nodeCommandSemaphores.Add(node.NodeID, new SemaphoreSlim(1, int.MaxValue));
                    _lastSemaphoreRelease.Add(node.NodeID, DateTime.UtcNow);

                    var device = new ZwaveDevice(node.NodeID);
                    queue.Add("UpdateDeviceProtocolInfo", () => UpdateDeviceProtocolInfo(device, node));
                    queue.Add("InitializeDevice", () => InitializeDevice(device, node));
                    queue.Add("GetSupportedCommandClasses", () => GetSupportedCommandClasses(device, node));

                    var task = Task.Run(() => ProcessQueue(device, node));
                    _nodeCommandTasks.Add(task);
                    _devices.Add(device);
                }
            }
        }

        private async Task ProcessQueue(ZwaveDevice device, Node node)
        {
            var queue = _nodeCommandQueues[node.NodeID];
            var semaphore = _nodeCommandSemaphores[node.NodeID];

            while (!queue.IsAddingCompleted)
            {
                await semaphore.WaitAsync();

                _log.Debug("Start processing queue for node " + node.NodeID);
                var isException = false;
                var nodeCommands = new List<NodeCommand>();
                NodeCommand nodeCommand;

                while (queue.TryTake(out nodeCommand))
                {
                    nodeCommands.Add(nodeCommand);
                }

                foreach (var command in nodeCommands)
                {
                    _log.Debug($"Execute task {command.Description} for node {node.NodeID}");

                    if (!await TryExecuteQueueTask(command.Function))
                    {
                        _log.Debug($"Executing task {command.Description} for node {node.NodeID} failed.");
                        queue.Add(command);
                        isException = true;
                    }
                }

                if (!isException && device.IsSupportingWakeUp)
                {
                    _log.Debug($"Execute task NoMoreInformation for node {node.NodeID}");
                    await TryExecuteQueueTask(() => node.GetCommandClass<WakeUp>().NoMoreInformation());
                }

                _log.Debug("Finished processing queue for node " + node.NodeID);
            }
        }

        private async Task<bool> TryExecuteQueueTask(Func<Task> task)
        {
            try
            {
                await task();
            }
            catch (TransmissionException)
            {
                return false;
            }
            catch (TimeoutException)
            {
                return false;
            }
            catch (Exception e)
            {
                // todo: je nach exception...
                _log.Error(e.Message, e);
            }

            return true;
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
            var libraryDevices = _library.Devices
                .Where(d => string.Equals(d.ManufacturerId, device.ManufacturerId, StringComparison.OrdinalIgnoreCase))
                .Where(d => string.Equals(d.BasicClass, device.BasicType.ToString("x2"), StringComparison.OrdinalIgnoreCase))
                .Where(d => string.Equals(d.GenericClass, device.GenericType.ToString("x2"), StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (libraryDevices.Count > 1)
            {
                var moreSpecific = libraryDevices
                    .Where(d => string.Equals(d.SpecificClass, device.SpecificType.ToString("x2"), StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (moreSpecific.Count > 0)
                {
                    libraryDevices = moreSpecific;
                }
            }

            if (libraryDevices.Count > 1)
            {
                var moreSpecific = libraryDevices
                    .Where(d => string.Equals(d.ProductType, device.ProductType, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (moreSpecific.Count > 0)
                {
                    libraryDevices = moreSpecific;
                }
            }

            if (libraryDevices.Count > 1)
            {
                var moreSpecific = libraryDevices
                    .Where(d => string.Equals(d.RfFrequency, "EU", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (moreSpecific.Count > 0)
                {
                    libraryDevices = moreSpecific;
                }
            }

            var libraryDevice = libraryDevices.FirstOrDefault();
            if (libraryDevice != null)
            {
                device.Manufacturer = libraryDevice.BrandName;
                device.ProductDescription = libraryDevice.Description.FirstOrDefault()?.Description;
                device.ImagePath = libraryDevice.DeviceImage;

                _log.Debug($"Node {device.Id} Manufacturer: {device.Manufacturer}");
                _log.Debug($"Node {device.Id} Description: {device.ProductDescription}");
            }
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

        private void ReleaseSemaphore(byte nodeId)
        {
            lock (_lastSemaphoreReleaseLock)
            {
                DateTime release;
                if (!_lastSemaphoreRelease.TryGetValue(nodeId, out release))
                {
                    return;
                }

                if ((DateTime.UtcNow - release).TotalSeconds < 1)
                {
                    return;
                }

                _lastSemaphoreRelease[nodeId] = DateTime.UtcNow;
            }

            SemaphoreSlim semaphore;
            if (_nodeCommandSemaphores.TryGetValue(nodeId, out semaphore))
            {
                _log.Debug($"Release semaphore for node {nodeId}");
                semaphore.Release();
            }
        }

        protected override void Dispose(bool disposing)
        {
            _isRunning = false;

            foreach (var commandQueue in _nodeCommandQueues)
            {
                commandQueue.Value.CompleteAdding();
            }

            _controller.Close();
            base.Dispose(disposing);
        }
    }
}
