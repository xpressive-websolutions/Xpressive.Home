using System.Collections.Generic;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Denon
{
    internal interface IDenonGateway : IGateway
    {
        IEnumerable<DenonDevice> GetDevices();

        void PowerOn(DenonDevice device);
        void PowerOff(DenonDevice device);
        void ChangeVolumne(DenonDevice device, int volume);
        void Mute(DenonDevice device);
        void Unmute(DenonDevice device);
        void ChangeInput(DenonDevice device, string source);
    }
}
