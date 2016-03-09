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
        private readonly IVariableRepository _variableRepository;
        private readonly IMessageQueue _messageQueue;

        public Q42PhilipsHueGateway(IHueBridgeLocator hueBridgeLocator, IHueAppKeyStore hueAppKeyStore, IVariableRepository variableRepository, IMessageQueue messageQueue) : base("PhilipsHue")
        {
            _hueBridgeLocator = hueBridgeLocator;
            _hueAppKeyStore = hueAppKeyStore;
            _variableRepository = variableRepository;
            _messageQueue = messageQueue;

            _properties.Add(new NumericProperty("Brightness", 0, 255, isReadOnly: false));
            _properties.Add(new ColorProperty("Color", isReadOnly: false));

            _actions.Add(new Action("Switch On")
            {
                Fields = { "Transition time in seconds" }
            });
            _actions.Add(new Action("Switch Off")
            {
                Fields = { "Transition time in seconds" }
            });
            _actions.Add(new Action("Change Color")
            {
                Fields = { "Color", "Transition time in seconds" }
            });
            _actions.Add(new Action("Change Brightness")
            {
                Fields = { "Brightness", "Transition time in seconds" }
            });
            _actions.Add(new Action("Alarm Once"));
            _actions.Add(new Action("Alarm Multiple"));

            _messageQueue.Subscribe((CommandMessage message) => HandleMessage(message));

            FindBridges().ContinueWith(_ =>
            {
                Console.WriteLine("Found {0} bulbs.", _devices.Count);

                foreach (var bulb in _devices)
                {
                    Console.WriteLine(" - {0}", bulb.Name);
                }
            });
        }

        private void HandleMessage(CommandMessage message)
        {
            throw new NotImplementedException();
        }

        public override bool IsConfigurationValid()
        {
            return true;
        }

        public override DeviceBase AddDevice(DeviceBase device)
        {
            throw new NotSupportedException();
        }

        //protected override async Task<string> GetInternal(DeviceBase device, PropertyBase property)
        //{
        //    var bulb = (HueBulb)device;
        //    var client = GetHueClient(bulb.Bridge);
        //    var light = await client.GetLightAsync(bulb.Id);

        //    switch (property.Name.ToLowerInvariant())
        //    {
        //        case "brightness":
        //            return light.State.Brightness.ToString();
        //        case "color":
        //            return light.State.ToHex(light.ModelId);
        //        default:
        //            throw new NotSupportedException(property.Name);
        //    }
        //}

        //protected override async Task SetInternal(DeviceBase device, PropertyBase property, string value)
        //{
        //    var bulb = (HueBulb)device;
        //    var client = GetHueClient(bulb.Bridge);
        //    var command = new LightCommand();

        //    switch (property.Name.ToLowerInvariant())
        //    {
        //        case "brightness":
        //            command.Brightness = byte.Parse(value);
        //            break;
        //        case "color":
        //            command.SetColor(value);
        //            break;
        //        default:
        //            throw new NotSupportedException(property.Name);
        //    }

        //    await client.SendCommandAsync(command, new[] { bulb.Id });
        //}

        protected override async Task ExecuteInternal(DeviceBase device, IAction action, IDictionary<string, string> values)
        {
            var bulb = (HueBulb)device;
            var command = new LightCommand();

            string seconds;
            int s;
            if (action.Fields.Contains("Transition time in seconds") &&
                values.TryGetValue("Transition time in seconds", out seconds) &&
                int.TryParse(seconds, out s))
            {
                command.TransitionTime = TimeSpan.FromSeconds(s);
            }

            string brightness;
            byte b;
            if (action.Fields.Contains("Brightness") &&
                values.TryGetValue("Brightness", out brightness) &&
                byte.TryParse(brightness, out b))
            {
                command.Brightness = b;
            }

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
                case "change brightness":
                    break;
                case "alarm once":
                    command.Alert = Alert.Once;
                    break;
                case "alarm multiple":
                    command.Alert = Alert.Multiple;
                    break;
                default:
                    return;
            }

            var client = GetHueClient(bulb.Bridge);
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
                    _messageQueue.Publish(new UserNotificationMessage("Philips Hue: Please press Button"));
                    await ConnectToBridge(bridge);
                }
            }
        }

        private bool IsKnownBridge(HueBridge bridge)
        {
            string appKey;
            return _hueAppKeyStore.TryGetAppKey(bridge.Id, out appKey);
        }

        private async Task ConnectToBridge(HueBridge bridge)
        {
            try
            {
                var appKey = await GetApiKeyWithBridgeButtonClick(bridge);
                _hueAppKeyStore.AddAppKey(bridge.Id, appKey);

                await FindBulbs(bridge);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task<string> GetApiKeyWithBridgeButtonClick(HueBridge bridge)
        {
            var endTime = DateTime.UtcNow.AddSeconds(30);
            var client = new LocalHueClient(bridge.InternalIpAddress);
            var computerName = Environment.MachineName;

            while (DateTime.UtcNow < endTime)
            {
                try
                {
                    var appKey = await client.RegisterAsync("Xpressive.Home", computerName);
                    return appKey;
                }
                catch (Exception) { }
            }

            return null;
        }

        private async Task FindBulbs(HueBridge bridge)
        {
            var client = GetHueClient(bridge);
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

        private LocalHueClient GetHueClient(HueBridge bridge)
        {
            string appKey;
            _hueAppKeyStore.TryGetAppKey(bridge.Id, out appKey);

            var client = new LocalHueClient(bridge.InternalIpAddress);
            client.Initialize(appKey);
            return client;
        }
    }
}