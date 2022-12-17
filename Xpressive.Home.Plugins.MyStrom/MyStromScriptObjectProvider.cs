using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Rooms;

namespace Xpressive.Home.Plugins.MyStrom
{
    internal sealed class MyStromScriptObjectProvider : IScriptObjectProvider
    {
        private readonly IMyStromGateway _gateway;
        private readonly IRoomRepository _roomRepository;
        private readonly IRoomDeviceService _roomDeviceService;

        public MyStromScriptObjectProvider(IMyStromGateway gateway, IRoomRepository roomRepository, IRoomDeviceService roomDeviceService)
        {
            _gateway = gateway;
            _roomRepository = roomRepository;
            _roomDeviceService = roomDeviceService;
        }

        public IEnumerable<Tuple<string, object>> GetObjects()
        {
            yield return new Tuple<string, object>("mystrom_list", new MyStromScriptObjectFactory(_gateway, _roomRepository, _roomDeviceService));
        }

        public IEnumerable<Tuple<string, Delegate>> GetDelegates()
        {
            // mystrom("id")
            // mystrom("id").on();

            var deviceResolver = new Func<string, MyStromScriptObject>(id =>
            {
                var device = _gateway.GetDevices().SingleOrDefault(d => d.Id.Equals(id));
                return new MyStromScriptObject(device, _gateway);
            });

            yield return new Tuple<string, Delegate>("mystrom", deviceResolver);
        }

        public class MyStromScriptObjectFactory
        {
            private readonly IMyStromGateway _gateway;
            private readonly IRoomRepository _roomRepository;
            private readonly IRoomDeviceService _roomDeviceService;

            public MyStromScriptObjectFactory(IMyStromGateway gateway, IRoomRepository roomRepository, IRoomDeviceService roomDeviceService)
            {
                _gateway = gateway;
                _roomRepository = roomRepository;
                _roomDeviceService = roomDeviceService;
            }

            public MyStromScriptObject[] all()
            {
                var devices = _gateway.GetDevices();

                return devices
                    .Select(d => new MyStromScriptObject(d, _gateway))
                    .ToArray();
            }

            public MyStromScriptObject[] byRoom(string roomName)
            {
                var devices = _gateway.GetDevices();
                var roomTask = _roomRepository.GetAsync();
                var deviceTask = _roomDeviceService.GetRoomDevicesAsync(_gateway.Name);

                Task.WaitAll(roomTask, deviceTask);

                var room = roomTask.Result.SingleOrDefault(r => r.Name.Equals(roomName, StringComparison.OrdinalIgnoreCase));

                if (room == null)
                {
                    return new MyStromScriptObject[0];
                }

                var roomDevices = deviceTask.Result
                    .Where(r => r.RoomId.Equals(room.Id))
                    .Select(r => r.Id)
                    .ToList();

                return devices
                    .Where(d => roomDevices.Contains(d.Id, StringComparer.Ordinal))
                    .Select(d => new MyStromScriptObject(d, _gateway))
                    .ToArray();
            }
        }

        public class MyStromScriptObject
        {
            private readonly IMyStromGateway _gateway;
            private readonly MyStromDevice _device;

            public MyStromScriptObject(MyStromDevice device, IMyStromGateway gateway)
            {
                _device = device;
                _gateway = gateway;
            }

            public void on()
            {
                _gateway.SwitchOn(_device);
            }

            public void off()
            {
                _gateway.SwitchOff(_device);
            }

            public object power()
            {
                if (_device == null)
                {
                    Log.Warning("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.Power;
            }

            public void relay(bool relay)
            {
                if (relay)
                {
                    on();
                }
                else
                {
                    off();
                }
            }

            public object relay()
            {
                if (_device == null)
                {
                    Log.Warning("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.Relay;
            }
        }
    }
}
