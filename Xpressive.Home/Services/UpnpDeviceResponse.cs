using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Services
{
    internal class UpnpDeviceResponse : IUpnpDeviceResponse
    {
        private readonly Dictionary<string, string> _otherHeaders;

        public UpnpDeviceResponse(string location, string server, string usn)
        {
            Location = location;
            Server = server;
            Usn = usn;
            _otherHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var uri = new Uri(location);
            IpAddress = uri.Host;
        }

        public string Location { get; }
        public string IpAddress { get; }
        public string Server { get; }
        public string Usn { get; }
        public IDictionary<string, string> OtherHeaders => new ReadOnlyDictionary<string, string>(_otherHeaders);

        public string FriendlyName { get; set; }
        public string Manufacturer { get; set; }
        public string ModelName { get; set; }

        internal void AddHeader(string key, string value)
        {
            _otherHeaders.Add(key, value);
        }
    }
}
