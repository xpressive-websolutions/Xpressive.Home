using Microsoft.AspNetCore.Mvc;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Controllers
{
    [Route("api/v1/rename")]
    public class RenameDeviceController : Controller
    {
        private readonly IMessageQueue _messageQueue;

        public RenameDeviceController(IMessageQueue messageQueue)
        {
            _messageQueue = messageQueue;
        }

        [HttpPost, Route("{gateway}/{deviceId}/{name}")]
        public void Execute(string gateway, string deviceId, string name)
        {
            _messageQueue.Publish(new RenameDeviceMessage(gateway, deviceId, name));
        }
    }
}
