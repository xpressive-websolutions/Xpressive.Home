using System.Collections.Generic;

namespace Xpressive.Home.Contracts.Gateway
{
    public interface IGateway
    {
        string Name { get; }
        IEnumerable<IDevice> Devices { get; }
        IEnumerable<IAction> Actions { get; }
    }
}