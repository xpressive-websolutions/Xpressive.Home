namespace Xpressive.Home.Contracts.Messaging
{
    public sealed class ExecuteScriptMessage : IMessageQueueMessage
    {
        private readonly string _scriptId;
        private readonly double _delayInMilliseconds;

        public ExecuteScriptMessage(string scriptId, double delayInMilliseconds)
        {
            _scriptId = scriptId;
            _delayInMilliseconds = delayInMilliseconds;
        }

        public string ScriptId => _scriptId;
        public double DelayInMilliseconds => _delayInMilliseconds;
    }
}
