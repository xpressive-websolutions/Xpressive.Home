using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        public ForeignExchangeRatesGateway(IMessageQueue messageQueue, IHttpClientProvider httpClientProvider) : base("Forex")
        {
            _messageQueue = messageQueue;
            _httpClientProvider = httpClientProvider;

            _canCreateDevices = true;
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

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { });

            await LoadDevicesAsync((id, name) => new ForeignExchangeRatesDevice { Id = id, Name = name });

            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var device in GetDevices())
                {
                    await UpdateVariables(device);
                }

                await Task.Delay(TimeSpan.FromHours(1), cancellationToken).ContinueWith(_ => { });
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
                var url = "http://api.fixer.io/latest?base=" + device.IsoCode.ToUpperInvariant();
                var client = _httpClientProvider.Get();
                var json = await client.GetStringAsync(url);

                var dto = JsonConvert.DeserializeObject<FixerDto>(json);

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
