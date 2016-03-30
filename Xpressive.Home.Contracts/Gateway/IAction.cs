using System.Collections.Generic;

namespace Xpressive.Home.Contracts.Gateway
{
    public interface IAction
    {
        string Name { get; }
        IList<string> Fields { get; }
    }
}