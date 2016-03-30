using System;
using System.Threading.Tasks;
using System.Xml;

namespace Xpressive.Home.Plugins.Sonos
{
    internal interface ISonosSoapClient
    {
        Task<XmlDocument> PostRequestAsync(Uri uri, string action, string body);
    }
}