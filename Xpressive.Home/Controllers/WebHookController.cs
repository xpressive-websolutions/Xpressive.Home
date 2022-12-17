using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Controllers
{
    [Route("api/v1/webhook")]
    public class WebHookController : Controller
    {
        private readonly IWebHookService _webHookService;
        private readonly IMessageQueue _messageQueue;

        public WebHookController(IWebHookService webHookService, IMessageQueue messageQueue)
        {
            _webHookService = webHookService;
            _messageQueue = messageQueue;
        }

        [HttpPost, Route("{id}")]
        public async Task<IActionResult> ExecuteAsync(string id)
        {
            var webHook = await _webHookService.GetWebHookAsync(id);

            if (webHook == null)
            {
                return NotFound();
            }

            if (Request != null && Request.HasFormContentType)
            {
                var formData = await Request.ReadFormAsync();

                foreach (var key in formData.Keys)
                {
                    var value = formData[key];

                    _messageQueue.Publish(new UpdateVariableMessage(webHook.GatewayName, webHook.DeviceId, key, value));
                }
            }

            return Ok();
        }

        [HttpGet, Route("{gatewayName}/{deviceId}")]
        public async Task<IActionResult> GetUrls(string gatewayName, string deviceId)
        {
            var webHooks = await _webHookService.GetWebHooksAsync(gatewayName, deviceId);
            var urls = new List<string>();
            var prefix = $"http://{Request.Host}/api/v1/webhook/";

            foreach (var webHook in webHooks)
            {
                urls.Add(prefix + webHook.Id);
            }

            return Ok(urls);
        }
    }
}
