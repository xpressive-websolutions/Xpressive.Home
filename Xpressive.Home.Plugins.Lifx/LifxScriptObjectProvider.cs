using System;
using System.Collections.Generic;
using System.Linq;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.Lifx
{
    internal sealed class LifxScriptObjectProvider : IScriptObjectProvider
    {
        private readonly ILifxGateway _gateway;

        public LifxScriptObjectProvider(ILifxGateway gateway)
        {
            _gateway = gateway;
        }

        public IEnumerable<Tuple<string, object>> GetObjects()
        {
            yield break;
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

            public void color(string hexColor)
            {
                color(hexColor, 0);
            }

            public void color(string hexColor, int transitionTimeInSeconds)
            {
                _gateway.ChangeColor(_device, hexColor, transitionTimeInSeconds);
            }

            public void brightness(double b)
            {
                brightness(b, 0);
            }

            public void brightness(double b, int transitionTimeInSeconds)
            {
                _gateway.ChangeBrightness(_device, b, transitionTimeInSeconds);
            }
        }
    }
}
