using System;
using System.Collections.Generic;
using Xpressive.Home.Contracts.Automation;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Variables;

namespace Xpressive.Home.Automation
{
    internal sealed class VariableScriptObjectProvider : IScriptObjectProvider
    {
        private readonly IVariableRepository _variableRepository;
        private readonly IMessageQueue _messageQueue;

        public VariableScriptObjectProvider(IMessageQueue messageQueue, IVariableRepository variableRepository)
        {
            _messageQueue = messageQueue;
            _variableRepository = variableRepository;
        }

        public IEnumerable<Tuple<string, object>> GetObjects()
        {
            yield return Tuple.Create("variable", (object) new VariableScriptObject(_messageQueue, _variableRepository));
        }

        public IEnumerable<Tuple<string, Delegate>> GetDelegates()
        {
            yield break;
        }

        public class VariableScriptObject
        {
            private readonly IVariableRepository _variableRepository;
            private readonly IMessageQueue _messageQueue;

            public VariableScriptObject(IMessageQueue messageQueue, IVariableRepository variableRepository)
            {
                _messageQueue = messageQueue;
                _variableRepository = variableRepository;
            }

            public object get(string name)
            {
                var variable = _variableRepository.Get<IVariable>(name);
                return variable?.Value;
            }

            public void set(string name, object value)
            {
                _messageQueue.Publish(new UpdateVariableMessage(name, value));
            }
        }
    }
}
