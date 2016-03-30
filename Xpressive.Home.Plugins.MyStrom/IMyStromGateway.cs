using System.Collections.Generic;

namespace Xpressive.Home.Plugins.MyStrom
{
    internal interface IMyStromGateway
    {
        IEnumerable<MyStromDevice> GetDevices();

        void SwitchOn(MyStromDevice device);
        void SwitchOff(MyStromDevice device);
    }
}