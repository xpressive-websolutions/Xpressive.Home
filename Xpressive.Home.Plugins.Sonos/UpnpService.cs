using System.Collections.Generic;

namespace Xpressive.Home.Plugins.Sonos
{
    public class UpnpService
    {
        public UpnpService()
        {
            Actions = new List<UpnpAction>();
        }

        public string Type { get; set; }
        public string Id { get; set; }
        public string ControlUrl { get; set; }
        public string DescriptionUrl { get; set; }
        public List<UpnpAction> Actions { get; }
    }
}
