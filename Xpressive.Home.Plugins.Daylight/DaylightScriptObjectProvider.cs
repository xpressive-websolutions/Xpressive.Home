using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.Daylight
{
    internal sealed class DaylightScriptObjectProvider : IScriptObjectProvider
    {
        private readonly IDaylightGateway _gateway;

        public DaylightScriptObjectProvider(IDaylightGateway gateway)
        {
            _gateway = gateway;
        }

        public IEnumerable<Tuple<string, object>> GetObjects()
        {
            yield break;
        }

        public IEnumerable<Tuple<string, Delegate>> GetDelegates()
        {
            // daylight("id").isDaylight()

            var deviceResolver = new Func<string, DaylightScriptObject>(id =>
            {
                var device = _gateway.GetDevices().SingleOrDefault(d => d.Id.Equals(id));
                return new DaylightScriptObject(device);
            });

            yield return new Tuple<string, Delegate>("daylight", deviceResolver);
        }

        public class DaylightScriptObject
        {
            private readonly DaylightDevice _device;

            public DaylightScriptObject(DaylightDevice device)
            {
                _device = device;
            }

            public object isDaylight()
            {
                if (_device == null)
                {
                    Log.Warning("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.IsDaylight;
            }
        }
    }
}
