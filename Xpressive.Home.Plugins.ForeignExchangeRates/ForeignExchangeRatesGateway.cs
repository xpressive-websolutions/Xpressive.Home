using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Plugins.ForeignExchangeRates
{
    internal sealed class ForeignExchangeRatesGateway : GatewayBase, IForeignExchangeRatesGateway
    {
        private readonly IMessageQueue _messageQueue;
        private readonly IHttpClientProvider _httpClientProvider;
        private readonly string _baseUrl;
        private readonly bool _isValidConfiguration = true;

        public ForeignExchangeRatesGateway(IMessageQueue messageQueue, IHttpClientProvider httpClientProvider, IConfiguration configuration, IDevicePersistingService persistingService)
            : base("Forex", true, persistingService)
        {
            _messageQueue = messageQueue;
            _httpClientProvider = httpClientProvider;

            var apiKey = configuration["forex:apikey"];
            _baseUrl = "http://data.fixer.io/api/latest?access_key=" + apiKey;

            if (string.IsNullOrEmpty(apiKey))
            {
                messageQueue.Publish(new NotifyUserMessage("Add Forex configuration (apikey) to config file."));
                _isValidConfiguration = false;
            }
        }

        public override IDevice CreateEmptyDevice()
        {
            return new ForeignExchangeRatesDevice();
        }

        public IEnumerable<ForeignExchangeRatesDevice> GetDevices()
        {
            return Devices.OfType<ForeignExchangeRatesDevice>();
        }

        public override IEnumerable<IAction> GetActions(IDevice device)
        {
            yield break;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { });

            if (!_isValidConfiguration)
            {
                return;
            }

            await LoadDevicesAsync((id, name) => new ForeignExchangeRatesDevice { Id = id, Name = name });

            while (!cancellationToken.IsCancellationRequested)
            {
                var devices = GetDevices().ToList();
                foreach (var device in devices)
                {
                    await UpdateVariables(device);
                }
                var waitTime = 24d / (1000d / 31 / devices.Count) + 0.5;
                await Task.Delay(TimeSpan.FromHours(waitTime), cancellationToken).ContinueWith(_ => { });
            }
        }

        protected override Task ExecuteInternalAsync(IDevice device, IAction action, IDictionary<string, string> values)
        {
            throw new NotSupportedException();
        }

        private async Task UpdateVariables(ForeignExchangeRatesDevice device)
        {
            try
            {
                var url = $"{_baseUrl}&base={device.IsoCode.ToUpperInvariant()}";
                var client = _httpClientProvider.Get();
                var json = await client.GetStringAsync(url);

                var dto = JsonConvert.DeserializeObject<FixerDto>(json);

                if (dto.Rates == null)
                {
                    Log.Error("Unable to get exchange rates for currency " + device.IsoCode + ": " + json);
                    return;
                }

                device.Rates.Clear();
                device.LastUpdate = dto.Date;
                _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "LastUpdate", dto.Date));

                foreach (var rate in dto.Rates)
                {
                    device.Rates[rate.Key] = rate.Value;
                    _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, rate.Key, rate.Value));
                }
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
            }
        }
    }
}
