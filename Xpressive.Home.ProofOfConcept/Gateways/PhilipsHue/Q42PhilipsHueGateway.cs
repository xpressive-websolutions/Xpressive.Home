using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Q42.HueApi;

namespace Xpressive.Home.ProofOfConcept.Gateways.PhilipsHue
{
    internal class Q42PhilipsHueGateway : GatewayBase
    {
        private readonly IHueBridgeLocator _hueBridgeLocator;
        private readonly IHueAppKeyStore _hueAppKeyStore;

        public Q42PhilipsHueGateway(IHueBridgeLocator hueBridgeLocator, IHueAppKeyStore hueAppKeyStore) : base("Philips Hue")
        {
            _hueBridgeLocator = hueBridgeLocator;
            _hueAppKeyStore = hueAppKeyStore;

            _actions.Add(new Action("Switch On"));
            _actions.Add(new Action("Switch Off"));
            _actions.Add(new Action("Change Color")
            {
                Fields = { "Color" }
            });

            FindBridges().ContinueWith(_ =>
            {
                Console.WriteLine("Found {0} bulbs.", _devices.Count);

                foreach (var bulb in _devices)
                {
                    Console.WriteLine(" - {0}", bulb.Name);
                }
            });
        }

        protected override Task<string> GetInternal(IDevice device, string property)
        {
            return Task.FromResult<string>(null);
        }

        protected override async Task ExecuteInternal(IDevice device, IAction action, IDictionary<string, string> values)
        {
            var bulb = (HueBulb)device;
            var command = new LightCommand();

            switch (action.Name.ToLowerInvariant())
            {
                case "switch on":
                    command.On = true;
                    break;
                case "switch off":
                    command.On = false;
                    break;
                case "change color":
                    command.SetColor(values["Color"]);
                    break;
                default:
                    return;
            }

            string appKey;
            _hueAppKeyStore.TryGetAppKey(bulb.Bridge.MacAddress, out appKey);
            var client = new LocalHueClient(bulb.Bridge.InternalIpAddress);
            client.Initialize(appKey);
            await client.SendCommandAsync(command, new[] { bulb.Id });
        }

        private async Task FindBridges()
        {
            var bridges = await _hueBridgeLocator.GetBridgesAsync();

            foreach (var bridge in bridges)
            {
                if (IsKnownBridge(bridge))
                {
                    await FindBulbs(bridge);
                }
                else
                {
                    // TODO: ask user to push the button
                    await ConnectToBridge(bridge);
                }
            }
        }

        private bool IsKnownBridge(HueBridge bridge)
        {
            string appKey;
            return _hueAppKeyStore.TryGetAppKey(bridge.MacAddress, out appKey);
        }

        private async Task ConnectToBridge(HueBridge bridge)
        {
            var client = new LocalHueClient(bridge.InternalIpAddress);
            var computerName = Environment.MachineName;
            var appKey = await client.RegisterAsync("Xpressive.Home", computerName);

            _hueAppKeyStore.AddAppKey(bridge.MacAddress, appKey);

            await FindBulbs(bridge);
        }

        private async Task FindBulbs(HueBridge bridge)
        {
            string appKey;
            _hueAppKeyStore.TryGetAppKey(bridge.MacAddress, out appKey);

            var client = new LocalHueClient(bridge.InternalIpAddress);
            client.Initialize(appKey);

            var bulbs = await client.GetLightsAsync();

            if (bulbs != null)
            {
                foreach (var light in bulbs)
                {
                    var bulb = new HueBulb(light.Id, bridge, light.Name);
                    _devices.Add(bulb);
                }
            }
        }
    }
}