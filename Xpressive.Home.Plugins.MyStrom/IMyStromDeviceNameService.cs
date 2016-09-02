using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xpressive.Home.Plugins.MyStrom
{
    internal interface IMyStromDeviceNameService
    {
        Task<IDictionary<string, string>> GetDeviceNamesByMacAsync();
    }
}
