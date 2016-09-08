using System.Collections.Generic;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Netatmo
{
    internal interface INetatmoGateway : IGateway
    {
        IEnumerable<NetatmoDevice> GetDevices();
    }
}
