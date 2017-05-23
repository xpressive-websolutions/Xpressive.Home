using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Rooms;

namespace Xpressive.Home.Plugins.Lifx
{
    internal sealed class LifxScriptObjectProvider : IScriptObjectProvider
    {
        private readonly ILifxGateway _gateway;
        private readonly IRoomRepository _roomRepository;
        private readonly IRoomDeviceService _roomDeviceService;

        public LifxScriptObjectProvider(ILifxGateway gateway, IRoomRepository roomRepository, IRoomDeviceService roomDeviceService)
        {
            _gateway = gateway;
            _roomRepository = roomRepository;
            _roomDeviceService = roomDeviceService;
        }

        public IEnumerable<Tuple<string, object>> GetObjects()
        {
            yield return new Tuple<string, object>("lifx_list", new LifxScriptObjectFactory(_gateway, _roomRepository, _roomDeviceService));
        }

        public IEnumerable<Tuple<string, Delegate>> GetDelegates()
        {
            // lifx("id")
            // lifx("id").on();

            var deviceResolver = new Func<string, LifxScriptObject>(id =>
            {
                var device = _gateway.GetDevices().SingleOrDefault(d => d.Id.Equals(id));
                return new LifxScriptObject(_gateway, device);
            });

            yield return new Tuple<string, Delegate>("lifx", deviceResolver);
        }

        public class LifxScriptObjectFactory
        {
            private readonly ILifxGateway _gateway;
            private readonly IRoomRepository _roomRepository;
            private readonly IRoomDeviceService _roomDeviceService;

            public LifxScriptObjectFactory(ILifxGateway gateway, IRoomRepository roomRepository, IRoomDeviceService roomDeviceService)
            {
                _gateway = gateway;
                _roomRepository = roomRepository;
                _roomDeviceService = roomDeviceService;
            }

            public LifxScriptObject[] all()
            {
                var devices = _gateway.GetDevices();

                return devices
                    .Select(d => new LifxScriptObject(_gateway, d))
                    .ToArray();
            }

            public LifxScriptObject[] byRoom(string roomName)
            {
                var devices = _gateway.GetDevices();
                var roomTask = _roomRepository.GetAsync();
                var deviceTask = _roomDeviceService.GetRoomDevicesAsync(_gateway.Name);

                Task.WaitAll(roomTask, deviceTask);

                var room = roomTask.Result.SingleOrDefault(r => r.Name.Equals(roomName, StringComparison.OrdinalIgnoreCase));

                if (room == null)
                {
                    return new LifxScriptObject[0];
                }

                var roomDevices = deviceTask.Result
                    .Where(r => r.RoomId.Equals(room.Id))
                    .Select(r => r.Id)
                    .ToList();

                return devices
                    .Where(d => roomDevices.Contains(d.Id, StringComparer.Ordinal))
                    .Select(d => new LifxScriptObject(_gateway, d))
                    .ToArray();
            }
        }

        public class LifxScriptObject
        {
            private readonly ILifxGateway _gateway;
            private readonly LifxDevice _device;

            public LifxScriptObject(ILifxGateway gateway, LifxDevice device)
            {
                _gateway = gateway;
                _device = device;
            }

            public void on()
            {
                on(0);
            }

            public void on(int transitionTimeInSeconds)
            {
                if (_device != null)
                {
                    _gateway.SwitchOn(_device, transitionTimeInSeconds);
                }
            }

            public void off()
            {
                off(0);
            }

            public void off(int transitionTimeInSeconds)
            {
                if (_device != null)
                {
                    _gateway.SwitchOff(_device, transitionTimeInSeconds);
                }
            }

            public void color(string hexColor)
            {
                color(hexColor, 0);
            }

            public void color(string hexColor, int transitionTimeInSeconds)
            {
                if (_device != null)
                {
                    _gateway.ChangeColor(_device, hexColor, transitionTimeInSeconds);
                }
            }

            public void brightness(double b)
            {
                brightness(b, 0);
            }

            public void brightness(double b, int transitionTimeInSeconds)
            {
                if (_device != null)
                {
                    _gateway.ChangeBrightness(_device, b, transitionTimeInSeconds);
                }
            }
        }
    }
}
