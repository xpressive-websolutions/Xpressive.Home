using System.Collections.Generic;

namespace Xpressive.Home.Plugins.Sonos
{
    public class UpnpAction
    {
        public UpnpAction()
        {
            InputArguments = new List<string>();
            OutputArguments = new List<string>();
        }

        public string Name { get; set; }
        public List<string> InputArguments { get; }
        public List<string> OutputArguments { get; }
    }
}
