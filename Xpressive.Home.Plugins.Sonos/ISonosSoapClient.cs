using System.Collections.Generic;
using System.Threading.Tasks;

namespace Xpressive.Home.Plugins.Sonos
{
    internal interface ISonosSoapClient
    {
        Task<Dictionary<string, string>> ExecuteAsync(SonosDevice device, UpnpService service, UpnpAction action, Dictionary<string, string> values);
    }
}
