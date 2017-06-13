using System.Collections.Generic;

namespace Xpressive.Home.Plugins.NissanLeaf
{
    internal interface INissanLeafGateway
    {
        IEnumerable<NissanLeafDevice> GetDevices();

        void StartCharging(NissanLeafDevice device);
        void StartClimateControl(NissanLeafDevice device);
        void StopClimateControl(NissanLeafDevice device);
    }
}
