using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Rooms;

namespace Xpressive.Home.Plugins.Sonos
{
    internal sealed class SonosScriptObjectProvider : IScriptObjectProvider
    {
        private readonly ISonosGateway _gateway;
        private readonly IRoomRepository _roomRepository;
        private readonly IRoomDeviceService _roomDeviceService;

        public SonosScriptObjectProvider(ISonosGateway gateway, IRoomRepository roomRepository, IRoomDeviceService roomDeviceService)
        {
            _gateway = gateway;
            _roomRepository = roomRepository;
            _roomDeviceService = roomDeviceService;
        }

        public IEnumerable<Tuple<string, object>> GetObjects()
        {
            yield return new Tuple<string, object>("sonos_list", new SonosScriptObjectFactory(_gateway, _roomRepository, _roomDeviceService));
        }

        public IEnumerable<Tuple<string, Delegate>> GetDelegates()
        {
            // sonos("id")
            // sonos("id").play();

            var deviceResolver = new Func<string, SonosScriptObject>(id =>
            {
                var device = _gateway.GetDevices().SingleOrDefault(d => d.Id.Equals(id));
                return new SonosScriptObject(_gateway, device);
            });

            yield return new Tuple<string, Delegate>("sonos", deviceResolver);
        }

        public class SonosScriptObjectFactory
        {
            private readonly ISonosGateway _gateway;
            private readonly IRoomRepository _roomRepository;
            private readonly IRoomDeviceService _roomDeviceService;

            public SonosScriptObjectFactory(ISonosGateway gateway, IRoomRepository roomRepository, IRoomDeviceService roomDeviceService)
            {
                _gateway = gateway;
                _roomRepository = roomRepository;
                _roomDeviceService = roomDeviceService;
            }

            public SonosScriptObject[] all()
            {
                var devices = _gateway.GetDevices();

                return devices
                    .Select(d => new SonosScriptObject(_gateway, d))
                    .ToArray();
            }

            public SonosScriptObject[] byRoom(string roomName)
            {
                var devices = _gateway.GetDevices();
                var roomTask = _roomRepository.GetAsync();
                var deviceTask = _roomDeviceService.GetRoomDevicesAsync(_gateway.Name);

                Task.WaitAll(roomTask, deviceTask);

                var room = roomTask.Result.SingleOrDefault(r => r.Name.Equals(roomName, StringComparison.OrdinalIgnoreCase));

                if (room == null)
                {
                    return new SonosScriptObject[0];
                }

                var roomDevices = deviceTask.Result
                    .Where(r => r.RoomId.Equals(room.Id))
                    .Select(r => r.Id)
                    .ToList();

                return devices
                    .Where(d => roomDevices.Contains(d.Id, StringComparer.Ordinal))
                    .Select(d => new SonosScriptObject(_gateway, d))
                    .ToArray();
            }
        }

        public class SonosScriptObject
        {
            private readonly ISonosGateway _gateway;
            private readonly SonosDevice _device;

            public SonosScriptObject(ISonosGateway gateway, SonosDevice device)
            {
                _gateway = gateway;
                _device = device;
            }

            public void play()
            {
                _gateway.Play(_device);
            }

            public void pause()
            {
                _gateway.Pause(_device);
            }

            public void stop()
            {
                _gateway.Stop(_device);
            }

            public void radio(string stream, string title)
            {
                _gateway.PlayRadio(_device, stream, title);
            }

            public void file(string file, string title, string album)
            {
                _gateway.PlayFile(_device, file, title, album);
            }

            public object volume()
            {
                if (_device == null)
                {
                    Log.Warning("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.Volume;
            }

            public void volume(double v)
            {
                _gateway.ChangeVolume(_device, v);
            }

            public object master()
            {
                if (_device == null)
                {
                    Log.Warning("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.IsMaster;
            }

            public string state()
            {
                if (_device == null)
                {
                    Log.Warning("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.TransportState;
            }
        }
    }
}
