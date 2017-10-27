using System;
using System.Collections.Generic;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.CloudTwin
{
    internal sealed class CloudTwin : DeviceBase
    {
        private readonly Dictionary<string, object> _properties;

        public CloudTwin()
        {
            _properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            Icon = "fa fa-cloud-upload";
        }

        internal IDictionary<string, object> Properties => _properties;

        internal DateTime LastUpdate { get; set; }
    }
}
