using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Rooms;

namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal sealed class PhilipsHueScriptObjectProvider : IScriptObjectProvider
    {
        private readonly IPhilipsHueGateway _gateway;
        private readonly IRoomRepository _roomRepository;
        private readonly IRoomDeviceService _roomDeviceService;

        public PhilipsHueScriptObjectProvider(IPhilipsHueGateway gateway, IRoomRepository roomRepository, IRoomDeviceService roomDeviceService)
        {
            _gateway = gateway;
            _roomRepository = roomRepository;
            _roomDeviceService = roomDeviceService;
        }

        public IEnumerable<Tuple<string, object>> GetObjects()
        {
            yield return new Tuple<string, object>("philipshue_list", new PhilipsHueScriptObjectFactory(_gateway, _roomRepository, _roomDeviceService));
        }

        public IEnumerable<Tuple<string, Delegate>> GetDelegates()
        {
            // philipshue("id")
            // philipshue("id").on();

            var deviceResolver = new Func<string, PhilipsHueScriptObject>(id =>
            {
                var device = _gateway.GetDevices().SingleOrDefault(d => d.Id.Equals(id));
                return new PhilipsHueScriptObject(_gateway, device);
            });

            yield return new Tuple<string, Delegate>("philipshue", deviceResolver);
        }

        public class PhilipsHueScriptObjectFactory
        {
            private readonly IPhilipsHueGateway _gateway;
            private readonly IRoomRepository _roomRepository;
            private readonly IRoomDeviceService _roomDeviceService;

            public PhilipsHueScriptObjectFactory(IPhilipsHueGateway gateway, IRoomRepository roomRepository, IRoomDeviceService roomDeviceService)
            {
                _gateway = gateway;
                _roomRepository = roomRepository;
                _roomDeviceService = roomDeviceService;
            }

            public PhilipsHueScriptObject[] all()
            {
                var devices = _gateway.GetDevices();

                return devices
                    .Select(d => new PhilipsHueScriptObject(_gateway, d))
                    .ToArray();
            }

            public PhilipsHueScriptObject[] byRoom(string roomName)
            {
                var devices = _gateway.GetDevices();
                var roomTask = _roomRepository.GetAsync();
                var deviceTask = _roomDeviceService.GetRoomDevicesAsync(_gateway.Name);

                Task.WaitAll(roomTask, deviceTask);

                var room = roomTask.Result.SingleOrDefault(r => r.Name.Equals(roomName, StringComparison.OrdinalIgnoreCase));

                if (room == null)
                {
                    return new PhilipsHueScriptObject[0];
                }

                var roomDevices = deviceTask.Result
                    .Where(r => r.RoomId.Equals(room.Id))
                    .Select(r => r.Id)
                    .ToList();

                return devices
                    .Where(d => roomDevices.Contains(d.Id, StringComparer.Ordinal))
                    .Select(d => new PhilipsHueScriptObject(_gateway, d))
                    .ToArray();
            }
        }

        public class PhilipsHueScriptObject
        {
            private static readonly ILog _log = LogManager.GetLogger(typeof(PhilipsHueScriptObject));
            private readonly IPhilipsHueGateway _gateway;
            private readonly PhilipsHueDevice _device;

            public PhilipsHueScriptObject(IPhilipsHueGateway gateway, PhilipsHueDevice device)
            {
                _gateway = gateway;
                _device = device;
            }

            public object isOn()
            {
                var bulb = _device as PhilipsHueBulb;

                if (bulb == null)
                {
                    _log.Warn("Unable to get variable value because the device was not found or it isn't a bulb.");
                    return null;
                }

                return bulb.IsOn;
            }

            public object hasPresence()
            {
                var sensor = _device as PhilipsHuePresenceSensor;

                if (sensor == null)
                {
                    _log.Warn("Unable to get variable value because the device was not found or it isn't a sensor.");
                    return null;
                }

                return sensor.HasPresence;
            }

            public void on()
            {
                on(0);
            }

            public void on(int transitionTimeInSeconds)
            {
                _gateway.SwitchOn(_device, transitionTimeInSeconds);
            }

            public void off()
            {
                off(0);
            }

            public void off(int transitionTimeInSeconds)
            {
                _gateway.SwitchOff(_device, transitionTimeInSeconds);
            }

            public void brightness(double b)
            {
                brightness(b, 0);
            }

            public void brightness(double b, int transitionTimeInSeconds)
            {
                _gateway.ChangeBrightness(_device, b, transitionTimeInSeconds);
            }

            public void color(string hexColor)
            {
                color(hexColor, 0);
            }

            public void color(string hexColor, int transitionTimeInSeconds)
            {
                _gateway.ChangeColor(_device, hexColor, transitionTimeInSeconds);
            }

            public void temperature(int temperature)
            {
                _gateway.ChangeTemperature(_device, temperature);
            }
        }
    }
}
