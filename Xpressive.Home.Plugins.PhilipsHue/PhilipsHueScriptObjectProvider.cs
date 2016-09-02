using System;
using System.Collections.Generic;
using System.Linq;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal sealed class PhilipsHueScriptObjectProvider : IScriptObjectProvider
    {
        private readonly IPhilipsHueGateway _gateway;

        public PhilipsHueScriptObjectProvider(IPhilipsHueGateway gateway)
        {
            _gateway = gateway;
        }

        public IEnumerable<Tuple<string, object>> GetObjects()
        {
            yield break;
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

        public class PhilipsHueScriptObject
        {
            private readonly IPhilipsHueGateway _gateway;
            private readonly PhilipsHueDevice _device;

            public PhilipsHueScriptObject(IPhilipsHueGateway gateway, PhilipsHueDevice device)
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
        }
    }
}
