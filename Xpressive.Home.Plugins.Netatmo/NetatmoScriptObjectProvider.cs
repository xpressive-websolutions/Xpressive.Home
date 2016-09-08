using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Rooms;

namespace Xpressive.Home.Plugins.Netatmo
{
    internal class NetatmoScriptObjectProvider : IScriptObjectProvider
    {
        private readonly INetatmoGateway _gateway;
        private readonly IRoomRepository _roomRepository;
        private readonly IRoomDeviceService _roomDeviceService;

        public NetatmoScriptObjectProvider(INetatmoGateway gateway, IRoomRepository roomRepository, IRoomDeviceService roomDeviceService)
        {
            _gateway = gateway;
            _roomRepository = roomRepository;
            _roomDeviceService = roomDeviceService;
        }

        public IEnumerable<Tuple<string, object>> GetObjects()
        {
            yield return new Tuple<string, object>("netatmo_list", new NetatmoScriptObjectFactory(_gateway, _roomRepository, _roomDeviceService));
        }

        public IEnumerable<Tuple<string, Delegate>> GetDelegates()
        {
            // netatmo("id")
            // netatmo("id").co2();

            var deviceResolver = new Func<string, NetatmoScriptObject>(id =>
            {
                var device = _gateway.GetDevices().SingleOrDefault(d => d.Id.Equals(id));
                return new NetatmoScriptObject(device);
            });

            yield return new Tuple<string, Delegate>("netatmo", deviceResolver);
        }

        public class NetatmoScriptObjectFactory
        {
            private readonly INetatmoGateway _gateway;
            private readonly IRoomRepository _roomRepository;
            private readonly IRoomDeviceService _roomDeviceService;

            public NetatmoScriptObjectFactory(INetatmoGateway gateway, IRoomRepository roomRepository, IRoomDeviceService roomDeviceService)
            {
                _gateway = gateway;
                _roomRepository = roomRepository;
                _roomDeviceService = roomDeviceService;
            }

            public NetatmoScriptObject[] all()
            {
                var devices = _gateway.GetDevices();

                return devices
                    .Select(d => new NetatmoScriptObject(d))
                    .ToArray();
            }

            public NetatmoScriptObject[] byRoom(string roomName)
            {
                var devices = _gateway.GetDevices();
                var roomTask = _roomRepository.GetAsync();
                var deviceTask = _roomDeviceService.GetRoomDevicesAsync(_gateway.Name);

                Task.WaitAll(roomTask, deviceTask);

                var room = roomTask.Result.SingleOrDefault(r => r.Name.Equals(roomName, StringComparison.OrdinalIgnoreCase));

                if (room == null)
                {
                    return new NetatmoScriptObject[0];
                }

                var roomDevices = deviceTask.Result
                    .Where(r => r.RoomId.Equals(room.Id))
                    .Select(r => r.Id)
                    .ToList();

                return devices
                    .Where(d => roomDevices.Contains(d.Id, StringComparer.Ordinal))
                    .Select(d => new NetatmoScriptObject(d))
                    .ToArray();
            }
        }

        public class NetatmoScriptObject
        {
            private static readonly ILog _log = LogManager.GetLogger(typeof(NetatmoScriptObject));
            private readonly NetatmoDevice _device;

            public NetatmoScriptObject(NetatmoDevice device)
            {
                _device = device;
            }

            public object co2()
            {
                if (_device == null)
                {
                    _log.Warn("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.Co2.HasValue ? (object)_device.Co2.Value : null;
            }

            public object humidity()
            {
                if (_device == null)
                {
                    _log.Warn("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.Humidity.HasValue ? (object)_device.Humidity.Value : null;
            }

            public object noise()
            {
                if (_device == null)
                {
                    _log.Warn("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.Noise.HasValue ? (object)_device.Noise.Value : null;
            }

            public object pressure()
            {
                if (_device == null)
                {
                    _log.Warn("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.Pressure.HasValue ? (object)_device.Pressure.Value : null;
            }

            public object temperature()
            {
                if (_device == null)
                {
                    _log.Warn("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.Temperature.HasValue ? (object)_device.Temperature.Value : null;
            }
        }
    }
}
