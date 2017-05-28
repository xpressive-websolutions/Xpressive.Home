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
        private static readonly ILog _log = LogManager.GetLogger(typeof(ScriptExecutionContext));
        private static readonly HashSet<Guid> _currentlyExecuting = new HashSet<Guid>();
        private static readonly object _lock = new object();
        private readonly Script _script;
        private readonly IEnumerable<IScriptObjectProvider> _objectProviders;

        public ScriptExecutionContext(Script script, IEnumerable<IScriptObjectProvider> objectProviders)
        {
            _script = script;
            _objectProviders = objectProviders;
        }

        public void Execute(string triggerVariable, object triggerValue)
        {
            if (!_script.IsEnabled)
            {
                return;
            }

            ExecuteEvenIfDisabled(triggerVariable, triggerValue);
        }

        public void ExecuteEvenIfDisabled(string triggerVariable, object triggerValue)
        {
            Task.Run(() => ExecuteAsTask(triggerVariable, triggerValue));
        }

        private void ExecuteAsTask(string triggerVariable, object triggerValue)
        {
            lock (_lock)
            {
                if (_currentlyExecuting.Contains(_script.Id))
                {
                    return;
                }
                _currentlyExecuting.Add(_script.Id);
            }

            _log.Info($"Execute script {_script.Name} ({_script.Id.ToString("n")})");

            try
            {
                var engine = SetupScriptEngine(triggerVariable, triggerValue);
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

            lock (_lock)
            {
                _currentlyExecuting.Remove(_script.Id);
            }
        }

        private Engine SetupScriptEngine(string triggerVariable, object triggerValue)
        {
            var engine = new Engine(cfg => cfg.TimeoutInterval(TimeSpan.FromMinutes(1)));

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

            engine.SetValue("trigger", new ScriptTrigger
            {
                variable = triggerVariable ?? string.Empty,
                value = triggerValue
            });

            return engine;
        }

        private class ScriptTrigger
        {
            public string variable { get; set; }

            public object value { get; set; }
        }
    }
}
