using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
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
        private readonly string _comPortName;
        private readonly ZwaveDeviceLibrary _library;
        private ZWaveController _controller;
        private bool _isRunning;

        public ZwaveGateway(IMessageQueue messageQueue)
            : base("zwave")
        {
            _messageQueue = messageQueue;
            _canCreateDevices = false;
            _isRunning = true;

            _comPortName = ConfigurationManager.AppSettings["zwave.port"];
            _library = new ZwaveDeviceLibrary();
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

            var controllerNodeId = await _controller.GetNodeID();
            var nodesEnumerable = await _controller.DiscoverNodes();
            var nodes = nodesEnumerable.Where(n => n.NodeID != controllerNodeId).ToList();

            _log.Debug($"Controller found {nodes.Count} nodes.");

            _controller.Channel.NodeEventReceived += async (s, e) =>
            {
                var node = nodes.Single(n => n.NodeID == e.NodeID);
                await UpdateNode(node);
            };

            foreach (var node in nodes)
            {
                try
                {
                    var device = new ZwaveDevice(node.NodeID);
                    await UpdateDeviceProtocolInfo(device, node);
                    _devices.Add(device);

                    node.GetCommandClass<Basic>().Changed += (s, e) =>
                    {
                        var nodeId = e.Report.Node.NodeID.ToString("D");
                        var value = (int)e.Report.Value;
                        // TODO: reset
                    };

                    node.GetCommandClass<SensorMultiLevel>().Changed += (s, e) =>
                    {
                        if (e.Report.Type == SensorType.Undefined)
                        {
                            return;
                        }

                        var nodeId = e.Report.Node.NodeID.ToString("D");
                        var variable = e.Report.Type.ToString();
                        var value = e.Report.Value;
                        _messageQueue.Publish(new UpdateVariableMessage(Name, nodeId, variable, value));
                        // TODO: reset
                    };

                    node.GetCommandClass<Alarm>().Changed += (s, e) =>
                    {
                        var nodeId = e.Report.Node.NodeID.ToString("D");
                        var value = e.Report.Level == 0 ? string.Empty : e.Report.Type.ToString();
                        _messageQueue.Publish(new UpdateVariableMessage(Name, nodeId, "Alarm", value));
                        // TODO: reset
                    };

                    node.GetCommandClass<SensorAlarm>().Changed += (s, e) =>
                    {
                        var nodeId = e.Report.Node.NodeID.ToString("D");
                        var value = e.Report.Level == 0 ? string.Empty : e.Report.Type.ToString();
                        _messageQueue.Publish(new UpdateVariableMessage(Name, nodeId, "SensorAlarm", value));
                        // TODO: reset
                    };
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            while (_isRunning)
            {
                await Task.Delay(10);
            }
        }

        private async Task UpdateDeviceProtocolInfo(ZwaveDevice device, Node node)
        {
            var protocolInfo = await node.GetProtocolInfo();
            device.BasicType = (byte)protocolInfo.BasicType;
            device.GenericType = (byte)protocolInfo.GenericType;
            device.SpecificType = protocolInfo.SpecificType;
        }

        private async Task UpdateNode(Node node)
        {
            var device = _devices.OfType<ZwaveDevice>().SingleOrDefault(d => d.NodeId == node.NodeID);

            if (device == null || device.IsUpdating)
            {
                return;
            }

            if ((DateTime.UtcNow - device.LastUpdate).TotalMinutes < 30)
            {
                return;
            }

            device.IsUpdating = true;

            try
            {
                await InitializeDevice(device, node);
                await UpdateDeviceBattery(device, node);

                device.LastUpdate = DateTime.UtcNow;
            }
            catch(TransmissionException) { }
            catch (Exception e)
            {
                _log.Error(e.Message, e);
            }

            device.IsUpdating = false;
        }

        private async Task UpdateDeviceBattery(ZwaveDevice device, Node node)
        {
            var battery = await node.GetCommandClass<Battery>().Get();

            if (battery.Value > 85)
            {
                device.BatteryStatus = DeviceBatteryStatus.Full;
            }
            else if (battery.Value > 25)
            {
                device.BatteryStatus = DeviceBatteryStatus.Good;
            }
            else
            {
                device.BatteryStatus = DeviceBatteryStatus.Low;
            }
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
            }
        }

        protected override void Dispose(bool disposing)
        {
            _isRunning = false;
            _controller.Close();
            base.Dispose(disposing);
        }
    }
}
