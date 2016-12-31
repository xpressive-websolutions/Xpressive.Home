using System;

namespace Xpressive.Home.Contracts.Messaging
{
    public sealed class ExecuteScriptMessage : IMessageQueueMessage
    {
        private readonly Guid _scriptId;
        private readonly double _delayInMilliseconds;

        public ExecuteScriptMessage(Guid scriptId, double delayInMilliseconds)
        {
            _scriptId = scriptId;
            _delayInMilliseconds = delayInMilliseconds;
        }

        public Guid ScriptId => _scriptId;
        public double DelayInMilliseconds => _delayInMilliseconds;
    }
}
