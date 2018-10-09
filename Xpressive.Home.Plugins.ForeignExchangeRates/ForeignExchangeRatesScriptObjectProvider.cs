using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.ForeignExchangeRates
{
    internal sealed class ForeignExchangeRatesScriptObjectProvider : IScriptObjectProvider
    {
        private readonly IForeignExchangeRatesGateway _gateway;

        public ForeignExchangeRatesScriptObjectProvider(IForeignExchangeRatesGateway gateway)
        {
            _gateway = gateway;
        }

        public IEnumerable<Tuple<string, object>> GetObjects()
        {
            yield break;
        }

        public IEnumerable<Tuple<string, Delegate>> GetDelegates()
        {
            // forex("id").getExchangeRate("USD")

            var deviceResolver = new Func<string, ForeignExchangeRatesScriptObject>(id =>
            {
                var device = _gateway.GetDevices().SingleOrDefault(d => d.Id.Equals(id));
                return new ForeignExchangeRatesScriptObject(device);
            });

            yield return new Tuple<string, Delegate>("forex", deviceResolver);
        }

        public class ForeignExchangeRatesScriptObject
        {
            private readonly ForeignExchangeRatesDevice _device;

            public ForeignExchangeRatesScriptObject(ForeignExchangeRatesDevice device)
            {
                _device = device;
            }

            public object getExchangeRate(string currency)
            {
                if (_device == null)
                {
                    Log.Warning("Unable to get variable value because the device was not found.");
                    return null;
                }

                if (!_device.Rates.TryGetValue(currency, out double rate))
                {
                    Log.Warning("Unable to get exchange rate because the currency '{currency}' it was not found.", currency);
                    return null;
                }

                return rate;
            }
        }
    }
}
