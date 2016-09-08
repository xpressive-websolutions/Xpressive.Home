using System.Collections.Generic;
using Xpressive.Home.Contracts.Gateway;

namespace Xpressive.Home.Plugins.Sonos
{
    internal interface ISonosGateway : IGateway
    {
        IEnumerable<SonosDevice> GetDevices();

        void Play(SonosDevice device);
        void Pause(SonosDevice device);
        void Stop(SonosDevice device);
        void PlayRadio(SonosDevice device, string stream, string title);
        void PlayFile(SonosDevice device, string file, string title, string album);
        void ChangeVolume(SonosDevice device, double volume);
    }
}
