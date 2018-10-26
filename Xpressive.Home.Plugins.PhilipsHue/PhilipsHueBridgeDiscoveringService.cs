using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Q42.HueApi;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Services;
using Xpressive.Home.Contracts.Variables;

namespace Xpressive.Home.Plugins.PhilipsHue
{
    internal class PhilipsHueBridgeDiscoveringService : IPhilipsHueBridgeDiscoveringService, IDisposable
    {
        private readonly IMessageQueue _messageQueue;
        private readonly IVariableRepository _variableRepository;
        private readonly IDeviceConfigurationBackupService _deviceConfigurationBackupService;
        private readonly object _lock;
        private readonly Dictionary<string, PhilipsHueBridge> _bridges;

        public PhilipsHueBridgeDiscoveringService(
            IMessageQueue messageQueue,
            IVariableRepository variableRepository,
            IDeviceConfigurationBackupService deviceConfigurationBackupService)
        {
            _lock = new object();
            _bridges = new Dictionary<string, PhilipsHueBridge>(StringComparer.OrdinalIgnoreCase);
            _messageQueue = messageQueue;
            _variableRepository = variableRepository;
            _deviceConfigurationBackupService = deviceConfigurationBackupService;

            messageQueue.Subscribe<NetworkDeviceFoundMessage>(Notify);
        }

        public event EventHandler<PhilipsHueBridge> BridgeFound;

        public void Start()
        {
            LoadBridgesFromBackup();
        }

        public void Notify(NetworkDeviceFoundMessage message)
        {
            string bridgeId;
            if (!message.Values.TryGetValue("hue-bridgeid", out bridgeId))
            {
                return;
            }

            PhilipsHueBridge bridge;
            bridgeId = bridgeId.ToLowerInvariant();

            lock (_lock)
            {
                if (_bridges.ContainsKey(bridgeId))
                {
                    return;
                }
                bridge = new PhilipsHueBridge(bridgeId, message.IpAddress);
                _bridges[bridgeId] = bridge;
            }

            Task.Run(() => HandleHueBridge(bridge));
        }

        private async Task HandleHueBridge(PhilipsHueBridge bridge)
        {
            var variableName = $"PhilipsHue.{bridge.Id}.ApiKey";
            var apiKey = _variableRepository.Get<StringVariable>(variableName)?.Value;

            if (string.IsNullOrEmpty(apiKey))
            {
                _messageQueue.Publish(new NotifyUserMessage("Found new Philips Hue Bridge. Please press the Button to connect."));
                apiKey = await GetApiKeyWithBridgeButtonClick(bridge);

                if (string.IsNullOrEmpty(apiKey))
                {
                    lock (_lock)
                    {
                        _bridges.Remove(bridge.Id);
                    }

                    return;
                }

                _messageQueue.Publish(new UpdateVariableMessage(variableName, apiKey));
            }

            OnBridgeFound(bridge);
        }

        private async Task<string> GetApiKeyWithBridgeButtonClick(PhilipsHueBridge bridge)
        {
            var endTime = DateTime.UtcNow.AddSeconds(30);
            var client = new LocalHueClient(bridge.IpAddress);

            while (DateTime.UtcNow < endTime)
            {
                try
                {
                    var machineName = Environment.MachineName.Replace(' ', '_');

                    if (machineName.Length > 19)
                    {
                        machineName = machineName.Substring(0, 19);
                    }

                    var appKey = await client.RegisterAsync("Xpressive.Home", machineName);
                    return appKey;
                }
                catch { }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            return null;
        }

        private void LoadBridgesFromBackup()
        {
            var backup = _deviceConfigurationBackupService.Get<BridgeConfigurationBackupDto[]>("PhilipsHue");

            if (backup == null)
            {
                return;
            }

            foreach (var dto in backup)
            {
                OnBridgeFound(new PhilipsHueBridge(dto.Id, dto.IpAddress));
            }
        }

        private void OnBridgeFound(PhilipsHueBridge e)
        {
            BridgeFound?.Invoke(this, e);
        }

        public void Dispose()
        {
            var backup = _bridges.Select(p => new BridgeConfigurationBackupDto(p.Key, p.Value.IpAddress)).ToArray();
            _deviceConfigurationBackupService.Save("PhilipsHue", backup);
        }

        private class BridgeConfigurationBackupDto
        {
            public BridgeConfigurationBackupDto(string id, string ipAddress)
            {
                Id = id;
                IpAddress = ipAddress;
            }

            public string Id { get; }
            public string IpAddress { get; }
        }
    }
}
