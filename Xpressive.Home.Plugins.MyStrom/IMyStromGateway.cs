using System.Collections.Generic;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.MyStrom
{
    internal interface IMyStromGateway : IGateway
    {
        IEnumerable<MyStromDevice> GetDevices();

        void SwitchOn(MyStromDevice device);
        void SwitchOff(MyStromDevice device);
    }
}
