using System;
using System.Collections.Generic;

namespace Xpressive.Home.Contracts.Automation
{
    public interface IScriptObjectProvider
    {
        IEnumerable<Tuple<string, object>> GetObjects();

        IEnumerable<Tuple<string, Delegate>> GetDelegates();
    }
}
