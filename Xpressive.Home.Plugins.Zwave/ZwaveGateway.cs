using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using OpenZWave;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using Action = Xpressive.Home.Contracts.Gateway.Action;

namespace Xpressive.Home.Plugins.Zwave
{
    public class ZwaveGateway : GatewayBase
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ZwaveGateway));
        private static readonly object _lock = new object();
        private readonly IMessageQueue _messageQueue;
        private readonly string _comPortName;
        private readonly string _networkKey;
        private readonly ZWManager _manager = ZWManager.Instance;

        public ZwaveGateway(IMessageQueue messageQueue)
            : base("zwave")
        {
            _messageQueue = messageQueue;
            _canCreateDevices = false;
            _comPortName = ConfigurationManager.AppSettings["zwave.port"];
            _networkKey = ConfigurationManager.AppSettings["zwave.networkkey"];
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
            var valueId = new ZWValueID(d.HomeId, d.NodeId, ZWValueGenre.Basic, 0x20, 1, 0, ZWValueType.Byte, 0);

            if (d.IsSwitchBinary && action.Name.Equals("Switch On", StringComparison.Ordinal))
            {
                _manager.SetValue(valueId, (byte)1);
            }
            else if (d.IsSwitchBinary && action.Name.Equals("Switch Off", StringComparison.Ordinal))
            {
                _manager.SetValue(valueId, (byte)0);
            }

            await Task.Delay(1);
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
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "config");
                var userPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConfigurationBackup", Name);
                var options = ZWOptions.Instance;
                options.Initialize(configPath, userPath, string.Empty);
                options.AddOptionInt("SaveLogLevel", (int)ZWLogLevel.Detail);
                options.AddOptionInt("QueueLogLevel", (int)ZWLogLevel.Debug);
                options.AddOptionInt("DumpTriggerLevel", (int)ZWLogLevel.Error);

                if (!string.IsNullOrEmpty(_networkKey))
                {
                    options.AddOptionString("NetworkKey", _networkKey, false);
                }

                options.AddOptionBool("Associate", true);
                options.Lock();

                _manager.Initialize();
                _manager.OnNotification += OnZwaveNotification;
                _manager.AddDriver(_comPortName);
            }
            catch (Exception e)
            {
                _log.Error(e.Message, e);
            }
        }

        private void OnZwaveNotification(ZWNotification notification)
        {
            try
            {
                if (notification.Code != NotificationCode.MsgComplete)
                {
                    return;
                }

                switch (notification.Type)
                {
                    case NotificationType.ValueAdded:
                    case NotificationType.ValueChanged:
                    case NotificationType.ValueRefreshed:
                    {
                        var device = GetDevice(notification);
                        HandleNodeValueNotification(device, notification.ValueID);
                        break;
                    }
                    case NotificationType.NodeNew:
                    case NotificationType.NodeAdded:
                        GetDevice(notification);
                        break;
                    case NotificationType.NodeProtocolInfo:
                    {
                        var device = GetDevice(notification);
                        var label = _manager.GetNodeType(notification.HomeId, notification.NodeId);
                        if (!string.IsNullOrEmpty(label) && string.IsNullOrEmpty(device.Name))
                        {
                            device.Name = label;
                        }
                        break;
                    }
                    case NotificationType.NodeNaming:
                    {
                        var device = GetDevice(notification);
                        var manufacturer = _manager.GetNodeManufacturerName(notification.HomeId, notification.NodeId);
                        var product = _manager.GetNodeProductName(notification.HomeId, notification.NodeId);
                        var name = _manager.GetNodeName(notification.HomeId, notification.NodeId);

                        if (!string.IsNullOrEmpty(name))
                        {
                            device.Name = name;
                        }
                        else if (!string.IsNullOrEmpty(manufacturer) && !string.IsNullOrEmpty(product))
                        {
                            device.Name = $"{manufacturer} {product}";
                        }

                        break;
                    }
                    case NotificationType.Group:
                    case NotificationType.NodeRemoved:
                    case NotificationType.ValueRemoved:
                    case NotificationType.NodeEvent:
                    case NotificationType.PollingDisabled:
                    case NotificationType.PollingEnabled:
                    case NotificationType.SceneEvent:
                    case NotificationType.CreateButton:
                    case NotificationType.DeleteButton:
                    case NotificationType.ButtonOn:
                    case NotificationType.ButtonOff:
                    case NotificationType.DriverReady:
                    case NotificationType.DriverFailed:
                    case NotificationType.DriverReset:
                    case NotificationType.EssentialNodeQueriesComplete:
                    case NotificationType.NodeQueriesComplete:
                    case NotificationType.AwakeNodesQueried:
                    case NotificationType.AllNodesQueriedSomeDead:
                    case NotificationType.AllNodesQueried:
                    case NotificationType.Notification:
                    case NotificationType.DriverRemoved:
                    case NotificationType.ControllerCommand:
                    case NotificationType.NodeReset:
                    case NotificationType.UserAlerts:
                    case NotificationType.ManufacturerSpecificDBReady:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception e)
            {
                _log.Error(e.Message, e);
            }
        }

        private ZwaveDevice GetDevice(ZWNotification notification)
        {
            lock (_lock)
            {
                var devices = _devices.Cast<ZwaveDevice>().ToDictionary(d => d.NodeId);
                ZwaveDevice device;

                if (devices.TryGetValue(notification.NodeId, out device))
                {
                    return device;
                }

                device = new ZwaveDevice(notification.NodeId, notification.HomeId);
                _devices.Add(device);
                return device;
            }
        }

        private void OnVariableChanged(ZwaveDevice device, string name, object value, string unit)
        {
            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, name, value, unit));
        }

        private object GetValue(ZWValueID valueId)
        {
            string valueString;
            bool valueBool;
            byte valueByte;
            double valueDouble;
            int valueInt;
            short valueShort;

            if (valueId.Type == ZWValueType.Bool && _manager.GetValueAsBool(valueId, out valueBool))
            {
                return valueBool;
            }

            if (valueId.Type == ZWValueType.List && _manager.GetValueListSelection(valueId, out valueString))
            {
                return valueString;
            }

            if (valueId.Type == ZWValueType.Byte && _manager.GetValueAsByte(valueId, out valueByte))
            {
                return (double)valueByte;
            }

            if (valueId.Type == ZWValueType.Decimal && _manager.GetValueAsString(valueId, out valueString) && double.TryParse(valueString, out valueDouble))
            {
                return valueDouble;
            }

            if (valueId.Type == ZWValueType.Int && _manager.GetValueAsInt(valueId, out valueInt))
            {
                return (double)valueInt;
            }

            if (valueId.Type == ZWValueType.Short && _manager.GetValueAsShort(valueId, out valueShort))
            {
                return (double)valueShort;
            }

            if (_manager.GetValueAsString(valueId, out valueString))
            {
                if (!string.IsNullOrEmpty(valueString) && double.TryParse(valueString, out valueDouble))
                {
                    return valueDouble;
                }

                if (!string.IsNullOrEmpty(valueString) && bool.TryParse(valueString, out valueBool))
                {
                    return valueBool;
                }

                return valueString;
            }

            return null;
        }

        private void HandleNodeValueNotification(ZwaveDevice device, ZWValueID valueId)
        {
            if (valueId.CommandClassId == 112)
            {
                // command class configuration
                return;
            }

            if (valueId.CommandClassId == 134)
            {
                // command class version
                return;
            }

            if (valueId.CommandClassId == 37)
            {
                // command class switch binary
                device.IsSwitchBinary = true;
            }

            var valueUnit = _manager.GetValueUnits(valueId) ?? string.Empty;
            var valueLabel = _manager.GetValueLabel(valueId);
            var valueValue = GetValue(valueId);

            if (valueId.CommandClassId == 128 && valueValue is double)
            {
                // command class battery
                var battery = (double)valueValue;
                if (battery > 90)
                {
                    device.BatteryStatus = DeviceBatteryStatus.Full;
                }
                else if (battery > 25)
                {
                    device.BatteryStatus = DeviceBatteryStatus.Good;
                }
                else
                {
                    device.BatteryStatus = DeviceBatteryStatus.Low;
                }
                return;
            }

            if (valueValue != null)
            {
                valueLabel = valueLabel.Replace(' ', '_');
                OnVariableChanged(device, valueLabel, valueValue, valueUnit);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _manager.Destroy();
                ZWOptions.Instance.Destroy();
            }

            base.Dispose(disposing);
        }
    }
}
