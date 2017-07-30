using System.Collections.Generic;

namespace Xpressive.Home.Plugins.ForeignExchangeRates
{
    internal interface IForeignExchangeRatesGateway
    {
        IEnumerable<ForeignExchangeRatesDevice> GetDevices();
    }
}
