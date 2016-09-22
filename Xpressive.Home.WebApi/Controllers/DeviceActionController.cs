using System.Collections.Generic;
using System.Web.Http;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.WebApi.Controllers
{
    [RoutePrefix("api/v1/action")]
    public class DeviceActionController : ApiController
    {
        private readonly IMessageQueue _messageQueue;

        public DeviceActionController(IMessageQueue messageQueue)
        {
            _messageQueue = messageQueue;
        }

        [HttpPost, Route("{gateway}/{deviceId}/{actionName}")]
        public void Execute(string gateway, string deviceId, string actionName, [FromBody] Dictionary<string, string> parameters)
        {
            _messageQueue.Publish(new CommandMessage(gateway, deviceId, actionName, parameters));
        }
    }
}
