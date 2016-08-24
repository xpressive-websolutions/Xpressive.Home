using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace Xpressive.Home.Plugins.Sonos
{
    internal interface ISonosSoapClient
    {
        Task<XmlDocument> PostRequestAsync(Uri uri, string action, string body);

        Task<Dictionary<string, string>> ExecuteAsync(SonosDevice device, UpnpService service, UpnpAction action, Dictionary<string, string> values);
    }
}
