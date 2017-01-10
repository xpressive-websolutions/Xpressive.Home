using System;
using System.Threading.Tasks;
using Xpressive.Home.Services;
using Xunit;

namespace Xpressive.Home.Plugins.PhilipsHue.Tests
{
    public class Given_a_philips_hue_bridge_discovering_service
    {
        [Fact]
        public void Then_the_bridge_is_found()
        {
            var upnpDeviceDiscoveringService = new UpnpDeviceDiscoveringService();
            var service = new PhilipsHueBridgeDiscoveringService(null, null, upnpDeviceDiscoveringService, null);

            var task1 = upnpDeviceDiscoveringService.StartDiscoveringAsync();
            var task2 = Task.Delay(TimeSpan.FromHours(1));
            Task.WaitAny(task1, task2);
        }
    }
}
