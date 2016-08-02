using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jint;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Automation
{
    internal class ScriptExecutionContext
    {
        private readonly Script _script;
        private readonly IEnumerable<IScriptObjectProvider> _objectProviders;

        public ScriptExecutionContext(Script script, IEnumerable<IScriptObjectProvider> objectProviders)
        {
            _script = script;
            _objectProviders = objectProviders;
        }

        public void Execute()
        {
            Task.Run(() => ExecuteAsTask());
        }

        private void ExecuteAsTask()
        {
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

            engine.Execute(_script.JavaScript);
        }
    }
}
