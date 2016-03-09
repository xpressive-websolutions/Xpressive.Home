using System;
using System.Collections.Specialized;
using System.Net;
using System.Threading.Tasks;
using Jint;

namespace Xpressive.Home.ProofOfConcept.Automation
{
    internal class AutomationEngine
    {
        private readonly IVariableRepository _variableRepository;
        private readonly IMessageQueue _messageQueue;

        public AutomationEngine(IVariableRepository variableRepository, IMessageQueue messageQueue)
        {
            _variableRepository = variableRepository;
            _messageQueue = messageQueue;
        }

        public void Execute(string script)
        {
            var engine = new Engine(options =>
            {
                options.DebugMode();
            });

            engine.SetValue("pushalot", new PushalotAutomationService(_variableRepository));
            engine.SetValue("email", new EmailAutomationService());
            engine.SetValue("tts", new TextToSpeechAutomationService());
            engine.SetValue("variable", new VariableAutomationService(_variableRepository, _messageQueue));
            engine.SetValue("log", new Action<object>(Console.WriteLine));

            engine.Execute(script);
        }
    }

    internal class PushalotAutomationService
    {
        private readonly IVariableRepository _variableRepository;

        public PushalotAutomationService(IVariableRepository variableRepository)
        {
            _variableRepository = variableRepository;
            _variableRepository.Register(new StringVariable("pushalot.key"));
        }

        public async Task Send(string message)
        {
            var key = _variableRepository.Get<StringVariable>("pushalot.key");

            if (key == null || string.IsNullOrEmpty(key.Value))
            {
                return;
            }

            using (var client = new WebClient())
            {
                var data = new NameValueCollection();
                data["AuthorizationToken"] = key.Value;
                data["Body"] = message;
                await client.UploadValuesTaskAsync("https://pushalot.com/api/sendmessage", data);
            }
        }
    }

    internal class EmailAutomationService
    {
    }

    internal class TextToSpeechAutomationService
    {
    }

    internal class VariableAutomationService
    {
        private readonly IVariableRepository _variableRepository;
        private readonly IMessageQueue _messageQueue;

        public VariableAutomationService(IVariableRepository variableRepository, IMessageQueue messageQueue)
        {
            _variableRepository = variableRepository;
            _messageQueue = messageQueue;
        }

        public object Get(string name)
        {
            var variable = _variableRepository.Get<IVariable>(name);

            if (variable != null)
            {
                return variable.Value;
            }

            return null;
        }

        public void Set(string name, object value)
        {
            _messageQueue.Publish(new UpdateVariableMessage(name, value));
        }
    }

    internal class LogAutomationService
    {
    }
}
