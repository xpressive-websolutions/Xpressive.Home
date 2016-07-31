using System;
using Xpressive.Home.Contracts.Gateway;
using ZWave;
using ZWave.Channel;

namespace Xpressive.Home.Plugins.Zwave
{
    public interface ICommandClassHandler : IDisposable
    {
        CommandClass CommandClass { get; }

        void Handle(IDevice device, Node node, ZwaveCommandQueue queue);
    }
}
