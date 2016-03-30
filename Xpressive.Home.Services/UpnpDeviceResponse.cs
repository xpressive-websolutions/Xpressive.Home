using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Services
{
    internal class UpnpDeviceResponse : IUpnpDeviceResponse
    {
        private readonly string _location;
        private readonly string _ipAddress;
        private readonly string _server;
        private readonly string _st;
        private readonly string _usn;
        private readonly Dictionary<string, string> _otherHeaders;

        public UpnpDeviceResponse(string location, string server, string st, string usn)
        {
            _location = location;
            _server = server;
            _st = st;
            _usn = usn;
            _otherHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var uri = new Uri(location);
            _ipAddress = uri.Host;
        }

        public string Location => _location;
        public string IpAddress => _ipAddress;
        public string Server => _server;
        public string St => _st;
        public string Usn => _usn;
        public IDictionary<string, string> OtherHeaders => new ReadOnlyDictionary<string, string>(_otherHeaders);

        internal void AddHeader(string key, string value)
        {
            _otherHeaders.Add(key, value);
        }
    }
}