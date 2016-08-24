using System;
using System.Collections.Generic;
using System.Linq;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.Sonos
{
    internal sealed class SonosScriptObjectProvider : IScriptObjectProvider
    {
        private readonly ISonosGateway _gateway;

        public SonosScriptObjectProvider(ISonosGateway gateway)
        {
            _gateway = gateway;
        }

        public IEnumerable<Tuple<string, object>> GetObjects()
        {
            yield break;
        }

        public IEnumerable<Tuple<string, Delegate>> GetDelegates()
        {
            // sonos("id")
            // sonos("id").play();

            var deviceResolver = new Func<string, SonosScriptObject>(id =>
            {
                var device = _gateway.GetDevices().SingleOrDefault(d => d.Id.Equals(id));
                return device != null ? new SonosScriptObject(_gateway, device) : null;
            });

            yield return new Tuple<string, Delegate>("sonos", deviceResolver);
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

            public void volume(double v)
            {
                _gateway.ChangeVolume(_device, v);
            }
        }
    }
}
