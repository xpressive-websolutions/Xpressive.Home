using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xpressive.Home.ProofOfConcept.Gateways.DateTime;
using Xpressive.Home.ProofOfConcept.Gateways.Daylight;
using Xpressive.Home.ProofOfConcept.Gateways.GoogleCalendar;
using Xpressive.Home.ProofOfConcept.Gateways.MyStrom;
using Xpressive.Home.ProofOfConcept.Gateways.PhilipsHue;
using Xpressive.Home.ProofOfConcept.Gateways.Pushalot;
using Xpressive.Home.ProofOfConcept.Gateways.Sonos;
using Xpressive.Home.ProofOfConcept.Gateways.TextToSpeech;

namespace Xpressive.Home.ProofOfConcept
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var random = new Random();

            var subscriptionService = new SubscriptionService();
            var devicePropertyStore = new DevicePropertyStore();
            var gatewayResolver = new GatewayResolver();
            new SubscriptionPropertyObserver(subscriptionService, devicePropertyStore, gatewayResolver);

            gatewayResolver.Register(new Q42PhilipsHueGateway(new HueBridgeLocator(new IpAddressService()), new HueAppKeyStore()));
            gatewayResolver.Register(new DateTimeGateway());
            gatewayResolver.Register(new DaylightGateway());
            gatewayResolver.Register(new PushalotGateway());
            gatewayResolver.Register(new MyStromGateway(new IpAddressService()));
            gatewayResolver.Register(new TextToSpeechGateway());
            gatewayResolver.Register(new GoogleCalendarGateway());
            gatewayResolver.Register(new SonosGateway());
            gatewayResolver.GetAll().ToList().ForEach(g => g.DevicePropertyChanged += (s, e) => devicePropertyStore.Save(e.GatewayName, e.DeviceId, e.Property, e.Value));

            int brightness = 60;
            devicePropertyStore.DevicePropertyChanged += async (s, e) =>
            {
                if (e.GatewayName.Equals("DateTime", StringComparison.Ordinal) &&
                    e.Property.Equals("Second") &&
                    int.Parse(e.Value) % 30 == 0)
                {
                    var hueGateway = gatewayResolver.Resolve("Philips Hue");
                    var spot = hueGateway.Devices.Single(d => d.Name.StartsWith("Test", StringComparison.Ordinal));

                    //await hueGateway.Set(spot, "Color", $"000000");
                    //await hueGateway.Set(spot, "Color", $"{random.Next(256).ToString("X2")}0000");
                    //await hueGateway.Set(spot, "Brightness", brightness.ToString());
                    brightness--;

                    var deviceAction = new DeviceAction("Philips Hue", spot.Id, "Change Color");
                    deviceAction.ActionFieldValues.Add("Color", "FF0000");
                    await hueGateway.Execute(deviceAction);
                }
            };

            Thread.Sleep(10000);
            var sonos = gatewayResolver.Resolve("Sonos");
            var sonosDevice = sonos.Devices.Single(d => d.Name.Equals("Bad", StringComparison.Ordinal));
            sonos.Execute(new DeviceAction("Sonos", sonosDevice.Id, "Play"));

            //IDevice ttsDevice;
            //var ttsFactory = new TextToSpeechGatewayFactory();
            //ttsFactory.TryCreate(
            //    gatewayResolver.Resolve("Text to speech"),
            //    new Dictionary<string, string> { { "Api Key", "" } },
            //    out ttsDevice);

            //var sayTimeDeviceSubscriptions = new[]
            //{
            //    new DeviceSubscription("DateTime", "DateTimeDevice", new Dictionary<string, string>()
            //    {
            //        { "Second", "0" },
            //    }),
            //};
            //var sayTimeAction = new DeviceAction("Text to speech", "TTS", "Say Time");
            //sayTimeAction.ActionFieldValues["Language"] = "de-de";
            //var sayTimeSubscription = new Subscription("SayTime", sayTimeAction, sayTimeDeviceSubscriptions);
            //sayTimeSubscription.WaitTime = TimeSpan.FromSeconds(5);
            //subscriptionService.Add(sayTimeSubscription);

            Console.ReadLine();
        }
    }
}
