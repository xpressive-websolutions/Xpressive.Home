using System;
using System.Collections.Generic;
using System.Threading;
using log4net;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Automation
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
            private static readonly ILog _log = LogManager.GetLogger(typeof(LogScriptObject));

            public void error(string message)
            {
                _log.Error(message);
            }

            public void warning(string message)
            {
                _log.Warn(message);
            }

            public void info(string message)
            {
                _log.Info(message);
            }

            public void debug(string message)
            {
                _log.Debug(message);
            }
        }
    }
}
