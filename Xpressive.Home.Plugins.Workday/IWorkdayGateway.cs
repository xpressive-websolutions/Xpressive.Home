using System.Collections.Generic;

namespace Xpressive.Home.Plugins.Workday
{
    internal interface IWorkdayGateway
    {
        IEnumerable<WorkdayDevice> GetDevices();
    }
}
