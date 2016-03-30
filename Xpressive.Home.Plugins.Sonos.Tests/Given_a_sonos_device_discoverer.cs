using System;
using System.Threading.Tasks;
using Xpressive.Home.Services;
using Xunit;
using Xunit.Abstractions;

namespace Xpressive.Home.Plugins.Sonos.Tests
{
    public class Given_a_sonos_device_discoverer
    {
        private readonly ITestOutputHelper _output;

        public Given_a_sonos_device_discoverer(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Then_the_devices_are_discovered()
        {
            var client = new SonosSoapClient();
            var upnpDeviceDiscoveringService = new UpnpDeviceDiscoveringService();
            var service = new SonosDeviceDiscoverer(upnpDeviceDiscoveringService, client);
            service.DeviceFound += (s, e) => _output.WriteLine($"Found sonos device {e.Zone} @ {e.IpAddress}");

            var task1 = Task.Run(async () => await upnpDeviceDiscoveringService.StartDiscoveringAsync());
            var task2 = Task.Delay(TimeSpan.FromSeconds(5));

            Task.WaitAny(task1, task2);
        }
    }
}
