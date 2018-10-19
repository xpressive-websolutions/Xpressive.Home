using System;
using System.Collections.Generic;
using System.Threading;
using Serilog;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Services.Automation
{
    internal sealed class DefaultScriptObjectProvider : IScriptObjectProvider
    {
        public IEnumerable<Tuple<string, object>> GetObjects()
        {
            yield return new Tuple<string, object>("log", new LogScriptObject());
        }

        public IEnumerable<Tuple<string, Delegate>> GetDelegates()
        {
            yield return new Tuple<string, Delegate>("sleep", new Action<double>(milliseconds => Thread.Sleep((int)milliseconds)));
        }

        public class LogScriptObject
        {
            public void error(string message)
            {
                Log.Error(message);
            }

            public void warning(string message)
            {
                Log.Warning(message);
            }

            public void info(string message)
            {
                Log.Information(message);
            }

            public void debug(string message)
            {
                Log.Debug(message);
            }
        }
    }
}
