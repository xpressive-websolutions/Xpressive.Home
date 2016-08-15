using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jint;
using Jint.Runtime;
using log4net;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Automation
{
    internal class ScriptExecutionContext
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof (ScriptExecutionContext));
        private readonly Script _script;
        private readonly IEnumerable<IScriptObjectProvider> _objectProviders;

        public ScriptExecutionContext(Script script, IEnumerable<IScriptObjectProvider> objectProviders)
        {
            _script = script;
            _objectProviders = objectProviders;
        }

        public void Execute()
        {
            if (!_script.IsEnabled)
            {
                return;
            }

            ExecuteEvenIfDisabled();
        }

        public void ExecuteEvenIfDisabled()
        {
            Task.Run(() => ExecuteAsTask());
        }

        private void ExecuteAsTask()
        {
            _log.Debug($"Execute script {_script.Name} ({_script.Id.ToString("n")})");

            var engine = new Engine(cfg => cfg.TimeoutInterval(TimeSpan.FromSeconds(30)));

            foreach (var objectProvider in _objectProviders)
            {
                foreach (var tuple in objectProvider.GetObjects())
                {
                    engine.SetValue(tuple.Item1, tuple.Item2);
                }

                foreach (var tuple in objectProvider.GetDelegates())
                {
                    engine.SetValue(tuple.Item1, tuple.Item2);
                }
            }

            try
            {
                engine.Execute(_script.JavaScript);
            }
            catch (JavaScriptException e)
            {
                _log.Error($"Error when executing script {_script.Name} ({_script.Id.ToString("n")}) at Line {e.LineNumber}: {e.Message}");
            }
            catch (Exception e)
            {
                _log.Error($"Error when executing script {_script.Name} ({_script.Id.ToString("n")})", e);
            }
        }
    }
}
