using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.NissanLeaf
{
    internal sealed class NissanLeafScriptObjectProvider : IScriptObjectProvider
    {
        private readonly INissanLeafGateway _gateway;

        public NissanLeafScriptObjectProvider(INissanLeafGateway gateway)
        {
            _gateway = gateway;
        }

        public IEnumerable<Tuple<string, object>> GetObjects()
        {
            yield break;
        }

        public IEnumerable<Tuple<string, Delegate>> GetDelegates()
        {
            // nissanleaf("id")
            // nissanleaf("id").power()

            var deviceResolver = new Func<string, NissanLeafScriptObject>(id =>
            {
                var device = _gateway.GetDevices().SingleOrDefault(d => d.Id.Equals(id));
                return new NissanLeafScriptObject(device, _gateway);
            });

            yield return new Tuple<string, Delegate>("nissanleaf", deviceResolver);
        }

        public class NissanLeafScriptObject
        {
            private static readonly ILog _log = LogManager.GetLogger(typeof(NissanLeafScriptObject));
            private readonly NissanLeafDevice _device;
            private readonly INissanLeafGateway _gateway;

            public NissanLeafScriptObject(NissanLeafDevice device, INissanLeafGateway gateway)
            {
                _device = device;
                _gateway = gateway;
            }

            public object cruisingRangeAcOff()
            {
                if (_device == null)
                {
                    _log.Warn("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.CruisingRangeAcOff;
            }

            public object cruisingRangeAcOn()
            {
                if (_device == null)
                {
                    _log.Warn("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.CruisingRangeAcOn;
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

            public object chargingState()
            {
                if (_device == null)
                {
                    _log.Warn("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.ChargingState;
            }

            public object pluginState()
            {
                if (_device == null)
                {
                    _log.Warn("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _device.PluginState;
            }

            public void startCharging()
            {
                _gateway.StartCharging(_device);
            }

            public void startClimateControl()
            {
                _gateway.StartClimateControl(_device);
            }

            public void stopClimateControl()
            {
                _gateway.StopClimateControl(_device);
            }
        }
    }
}
