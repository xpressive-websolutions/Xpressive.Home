using System.Collections.Generic;

namespace Xpressive.Home.Plugins.Daylight
{
    internal interface IDaylightGateway
    {
        IEnumerable<DaylightDevice> GetDevices();
    }
}
