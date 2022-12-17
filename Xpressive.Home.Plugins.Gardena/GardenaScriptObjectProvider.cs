using System;
using System.Collections.Generic;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.Gardena
{
    internal class GardenaScriptObjectProvider : IScriptObjectProvider
    {
        public IEnumerable<Tuple<string, object>> GetObjects()
        {
            yield break;
        }

        public IEnumerable<Tuple<string, Delegate>> GetDelegates()
        {
            yield break;
        }
    }
}
