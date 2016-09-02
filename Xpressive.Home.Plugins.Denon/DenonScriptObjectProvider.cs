using System;
using System.Collections.Generic;
using System.Linq;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.Denon
{
    internal sealed class DenonScriptObjectProvider : IScriptObjectProvider
    {
        private readonly IDenonGateway _gateway;

        public DenonScriptObjectProvider(IDenonGateway gateway)
        {
            _gateway = gateway;
        }

        public IEnumerable<Tuple<string, object>> GetObjects()
        {
            yield break;
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

            public void mute()
            {
                _gateway.Mute(_device);
            }

            public void unmute()
            {
                _gateway.Unmute(_device);
            }

            public void source(string s)
            {
                _gateway.ChangeInput(_device, s);
            }

            public void volume(int v)
            {
                _gateway.ChangeVolumne(_device, v);
            }
        }
    }
}
