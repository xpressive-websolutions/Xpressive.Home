using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Rooms;

namespace Xpressive.Home.Plugins.Denon
{
    internal sealed class DenonScriptObjectProvider : IScriptObjectProvider
    {
        private readonly IDenonGateway _gateway;
        private readonly IRoomRepository _roomRepository;
        private readonly IRoomDeviceService _roomDeviceService;

        public DenonScriptObjectProvider(IDenonGateway gateway, IRoomRepository roomRepository, IRoomDeviceService roomDeviceService)
        {
            _gateway = gateway;
            _roomRepository = roomRepository;
            _roomDeviceService = roomDeviceService;
        }

        public IEnumerable<Tuple<string, object>> GetObjects()
        {
            yield return new Tuple<string, object>("denon_list", new DenonScriptObjectFactory(_gateway, _roomRepository, _roomDeviceService));
        }

        public IEnumerable<Tuple<string, Delegate>> GetDelegates()
        {
            // denon("id")
            // denon("id").on();

            var deviceResolver = new Func<string, DenonScriptObject>(id =>
            {
                var device = _gateway.GetDevices().SingleOrDefault(d => d.Id.Equals(id));
                return new DenonScriptObject(_gateway, device);
            });

            yield return new Tuple<string, Delegate>("denon", deviceResolver);
        }

        public class DenonScriptObjectFactory
        {
            private readonly IDenonGateway _gateway;
            private readonly IRoomRepository _roomRepository;
            private readonly IRoomDeviceService _roomDeviceService;

            public DenonScriptObjectFactory(IDenonGateway gateway, IRoomRepository roomRepository, IRoomDeviceService roomDeviceService)
            {
                _gateway = gateway;
                _roomRepository = roomRepository;
                _roomDeviceService = roomDeviceService;
            }

            public DenonScriptObject[] all()
            {
                var devices = _gateway.GetDevices();

                return devices
                    .Select(d => new DenonScriptObject(_gateway, d))
                    .ToArray();
            }

            public DenonScriptObject[] byRoom(string roomName)
            {
                var devices = _gateway.GetDevices();
                var roomTask = _roomRepository.GetAsync();
                var deviceTask = _roomDeviceService.GetRoomDevicesAsync(_gateway.Name);

                Task.WaitAll(roomTask, deviceTask);

                var room = roomTask.Result.SingleOrDefault(r => r.Name.Equals(roomName, StringComparison.OrdinalIgnoreCase));

                if (room == null)
                {
                    return new DenonScriptObject[0];
                }

                var roomDevices = deviceTask.Result
                    .Where(r => r.RoomId.Equals(room.Id))
                    .Select(r => r.Id)
                    .ToList();

                return devices
                    .Where(d => roomDevices.Contains(d.Id, StringComparer.Ordinal))
                    .Select(d => new DenonScriptObject(_gateway, d))
                    .ToArray();
            }
        }

        public class DenonScriptObject
        {
            private readonly IDenonGateway _gateway;
            private readonly DenonDevice _device;

            public DenonScriptObject(IDenonGateway gateway, DenonDevice device)
            {
                _gateway = gateway;
                _device = device;
            }

            public void on()
            {
                _gateway.PowerOn(_device);
            }

            public void off()
            {
                _gateway.PowerOff(_device);
            }

            public object mute()
            {
                if (_device == null)
                {
                    Log.Warning("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.IsMute;
            }

            public void mute(bool isMute)
            {
                if (isMute)
                {
                    _gateway.Mute(_device);
                }
                else
                {
                    _gateway.Unmute(_device);
                }
            }

            public string source()
            {
                if (_device == null)
                {
                    Log.Warning("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.Source;
            }

            public void source(string s)
            {
                _gateway.ChangeInput(_device, s);
            }

            public object volume()
            {
                if (_device == null)
                {
                    Log.Warning("Unable to get variable value because the device was not found.");
                    return null;
                }

                return (int)(_device.Volume * 100);
            }

            public void volume(int v)
            {
                _gateway.ChangeVolumne(_device, v);
            }
        }
    }
}
