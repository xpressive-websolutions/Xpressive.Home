using System;
using System.Collections.Generic;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Services.Automation
{
    internal sealed class SchedulerScriptObjectProvider : IScriptObjectProvider
    {
        private readonly IMessageQueue _messageQueue;

        public SchedulerScriptObjectProvider(IMessageQueue messageQueue)
        {
            _messageQueue = messageQueue;
        }

        public IEnumerable<Tuple<string, object>> GetObjects()
        {
            yield break;
        }

        public IEnumerable<Tuple<string, Delegate>> GetDelegates()
        {
            yield return new Tuple<string, Delegate>("execute", new Action<string, double>(ExecuteScript));
        }

        private void ExecuteScript(string scriptId, double delayInMilliseconds)
        {
            Guid id;
            if (!Guid.TryParse(scriptId, out id))
            {
                return;
            }

            _messageQueue.Publish(new ExecuteScriptMessage(id, delayInMilliseconds));
        }
    }
}
