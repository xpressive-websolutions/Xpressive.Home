using System;
using System.Collections.Generic;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.ForeignExchangeRates
{
    internal sealed class ForeignExchangeRatesDevice : DeviceBase
    {
        public ForeignExchangeRatesDevice()
        {
            Icon = "fa fa-money";
            Rates = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        }

        [DeviceProperty(3)]
        public string IsoCode { get; set; }

        public string LastUpdate { get; set; }
        public Dictionary<string, double> Rates { get; }

        public override bool IsConfigurationValid()
        {
            if (string.IsNullOrEmpty(IsoCode))
            {
                return false;
            }

            if (IsoCode.Length != 3)
            {
                return false;
            }

            return base.IsConfigurationValid();
        }
    }
}
