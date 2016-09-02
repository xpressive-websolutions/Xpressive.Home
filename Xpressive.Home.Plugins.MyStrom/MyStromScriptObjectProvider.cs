using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.MyStrom
{
    internal sealed class MyStromScriptObjectProvider : IScriptObjectProvider
    {
        private readonly IMyStromGateway _gateway;

        public MyStromScriptObjectProvider(IMyStromGateway gateway)
        {
            _gateway = gateway;
        }

        public IEnumerable<Tuple<string, object>> GetObjects()
        {
            yield break;
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

        public class MyStromScriptObject
        {
            private static readonly ILog _log = LogManager.GetLogger(typeof(MyStromScriptObject));
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
                    _log.Warn("Unable to get variable value because the device was not found.");
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
                    _log.Warn("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.Relay;
            }
        }
    }
}
