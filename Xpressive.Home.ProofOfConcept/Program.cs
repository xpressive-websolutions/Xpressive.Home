using System;
using System.Collections.Generic;
using System.Linq;
using Xpressive.Home.ProofOfConcept.Gateways.DateTime;
using Xpressive.Home.ProofOfConcept.Gateways.Daylight;
using Xpressive.Home.ProofOfConcept.Gateways.GoogleCalendar;
using Xpressive.Home.ProofOfConcept.Gateways.MyStrom;
using Xpressive.Home.ProofOfConcept.Gateways.PhilipsHue;
using Xpressive.Home.ProofOfConcept.Gateways.Pushalot;
using Xpressive.Home.ProofOfConcept.Gateways.TextToSpeech;

namespace Xpressive.Home.ProofOfConcept
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var subscriptionService = new SubscriptionService();
            var devicePropertyStore = new DevicePropertyStore();
            var gatewayResolver = new GatewayResolver();
            new SubscriptionPropertyObserver(subscriptionService, devicePropertyStore, gatewayResolver);

            gatewayResolver.Register(new Q42PhilipsHueGateway(new HueBridgeLocator(), new HueAppKeyStore()));
            gatewayResolver.Register(new DateTimeGateway());
            gatewayResolver.Register(new DaylightGateway());
            gatewayResolver.Register(new PushalotGateway());
            gatewayResolver.Register(new MyStromGateway());
            gatewayResolver.Register(new TextToSpeechGateway());
            gatewayResolver.Register(new GoogleCalendarGateway());
            gatewayResolver.GetAll().ToList().ForEach(g => g.DevicePropertyChanged += (s, e) => devicePropertyStore.Save(e.GatewayName, e.DeviceId, e.Property, e.Value));

            var sayTimeDeviceSubscriptions = new[]
            {
                new DeviceSubscription("DateTime", "DateTimeDevice", new Dictionary<string, string>()
                {
                    { "Second", "0" },
                }),
            };
            var sayTimeAction = new DeviceAction("TextToSpeech", "TTS", "Say Time");
            sayTimeAction.ActionFieldValues["Language"] = "de-de";
            var sayTimeSubscription = new Subscription("SayTime", sayTimeAction, sayTimeDeviceSubscriptions);
            sayTimeSubscription.WaitTime = TimeSpan.FromSeconds(5);
            subscriptionService.Add(sayTimeSubscription);

            Console.ReadLine();
        }
    }
}
