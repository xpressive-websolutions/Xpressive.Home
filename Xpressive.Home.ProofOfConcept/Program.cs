using System;
using System.Linq;
using System.Threading;
using Xpressive.Home.ProofOfConcept.Contracts;
using Xpressive.Home.ProofOfConcept.Gateways.Daylight;
using Xpressive.Home.ProofOfConcept.Gateways.Denon;
using Xpressive.Home.ProofOfConcept.Gateways.GoogleCalendar;
using Xpressive.Home.ProofOfConcept.Gateways.MyStrom;
using Xpressive.Home.ProofOfConcept.Gateways.PhilipsHue;
using Xpressive.Home.ProofOfConcept.Gateways.Pushalot;
using Xpressive.Home.ProofOfConcept.Gateways.Sonos;

namespace Xpressive.Home.ProofOfConcept
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var random = new Random();

            var gatewayResolver = new GatewayResolver();
            var ipAddressService = new IpAddressService();
            var messageQueue = new MessageQueue();
            var variableRepository = new VariableRepository(messageQueue);
            var hueBridgeLocator = new HueBridgeLocator(ipAddressService);
            var hueAppKeyStore = new HueAppKeyStore();

            //gatewayResolver.Register(new Q42PhilipsHueGateway(hueBridgeLocator, hueAppKeyStore, variableRepository, messageQueue));
            //gatewayResolver.Register(new DaylightGateway(variableRepository, messageQueue));
            //gatewayResolver.Register(new PushalotGateway());
            //gatewayResolver.Register(new MyStromGateway(ipAddressService, messageQueue, variableRepository));
            //gatewayResolver.Register(new GoogleCalendarGateway());
            //gatewayResolver.Register(new DenonGateway(ipAddressService));
            gatewayResolver.Register(new SonosGateway());

            //var denon = gatewayResolver.Resolve("Denon");
            var sonos = gatewayResolver.Resolve("Sonos");

            //while (!denon.Devices.Any())
            //{
            //    Thread.Sleep(1000);
            //}
            while (!sonos.Devices.Any())
            {
                Thread.Sleep(1000);
            }

            //var denonDevice = denon.Devices.Single();
            //denon.Execute(new DeviceAction("Denon", denonDevice.Id, "Volume Up"));

            var sonosDevice = sonos.Devices.First(d => d.Name.Equals("Schlafzimmer"));
            sonos.Execute(new DeviceAction("Sonos", sonosDevice.Id, "Stop"));

            Console.ReadLine();
        }
    }
}