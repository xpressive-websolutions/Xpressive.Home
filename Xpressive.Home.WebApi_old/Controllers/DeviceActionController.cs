using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.WebApi.Controllers
{
    [Route("api/v1/action")]
    public class DeviceActionController : Controller
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
