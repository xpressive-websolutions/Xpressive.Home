using System;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Messaging;
using Xunit;
using Xunit.Abstractions;

namespace Xpressive.Home.Plugins.Netatmo.Tests
{
    public class Given_a_netatmo_gateway
    {
        private readonly ITestOutputHelper _output;

        public Given_a_netatmo_gateway(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task X()
        {
            var gateway = new NetatmoGateway(new MessageQueueMock(_output.WriteLine));
            await gateway.ScanDeviceAndDataAsync();
        }
    }

    public class MessageQueueMock : IMessageQueue
    {
        private readonly Action<string> _log;

        public MessageQueueMock(Action<string> log)
        {
            _log = log;
        }

        public void Publish<T>(T message) where T : IMessageQueueMessage
        {
            var u = message as UpdateVariableMessage;

            if (u != null)
            {
                _log($"UpdateVariable: {u.Name}={u.Value}");
            }
        }

        public void Subscribe<T>(Action<T> action) where T : IMessageQueueMessage
        {
            throw new NotImplementedException();
        }
    }
}
