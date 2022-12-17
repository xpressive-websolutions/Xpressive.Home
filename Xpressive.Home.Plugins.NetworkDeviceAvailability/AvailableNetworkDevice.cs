using System;
using System.Text.RegularExpressions;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.NetworkDeviceAvailability
{
    public class AvailableNetworkDevice : DeviceBase
    {
        public static Regex MacAddressValidator = new Regex(
            "[0-9a-f]{12}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline,
            TimeSpan.FromSeconds(1));

        public string LastSeen { get; set; }
        public string IpAddress { get; set; }
        public string Manufacturer { get; set; }
        public bool IsAvailable { get; set; }

        public override bool IsConfigurationValid()
        {
            if (string.IsNullOrEmpty(Id))
            {
                return false;
            }

            var match = MacAddressValidator.Match(Id);

            if (!match.Success || !Id.Equals(match.Value, StringComparison.Ordinal))
            {
                return false;
            }

            return base.IsConfigurationValid();
        }
    }
}
