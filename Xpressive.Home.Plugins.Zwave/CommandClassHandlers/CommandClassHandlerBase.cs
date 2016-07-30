using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;
using ZWave;
using ZWave.Channel;
using ZWave.CommandClasses;

namespace Xpressive.Home.Plugins.Zwave.CommandClassHandlers
{
    internal abstract class CommandClassHandlerBase : ICommandClassHandler
    {
        private readonly IMessageQueue _messageQueue;
        private readonly CommandClass _commandClass;

        protected CommandClassHandlerBase(IMessageQueue messageQueue, CommandClass commandClass)
        {
            _messageQueue = messageQueue;
            _commandClass = commandClass;
        }

        public CommandClass CommandClass => _commandClass;

        public void Handle(IDevice device, Node node, BlockingCollection<NodeCommand> queue)
        {
            Handle((ZwaveDevice)device, node, queue);
        }

        protected void UpdateVariable(NodeReport nodeReport, string variable, object value)
        {
            var device = nodeReport.Node.NodeID.ToString("D");
            _messageQueue.Publish(new UpdateVariableMessage("zwave", device, variable, value));
        }

        protected abstract void Handle(ZwaveDevice device, Node node, BlockingCollection<NodeCommand> queue);

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing) { }

        ~CommandClassHandlerBase()
        {
            Dispose(false);
        }
    }
}
