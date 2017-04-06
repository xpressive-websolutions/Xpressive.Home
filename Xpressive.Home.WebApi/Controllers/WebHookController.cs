using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.WebApi.Controllers
{
    [RoutePrefix("api/v1/webhook")]
    public class WebHookController : ApiController
    {
        private readonly IWebHookService _webHookService;
        private readonly IMessageQueue _messageQueue;

        public WebHookController(IWebHookService webHookService, IMessageQueue messageQueue)
        {
            _webHookService = webHookService;
            _messageQueue = messageQueue;
        }

        [HttpPost, Route("{id}")]
        public async Task<IHttpActionResult> ExecuteAsync(string id)
        {
            var webHook = await _webHookService.GetWebHookAsync(id);

            if (webHook == null)
            {
                return NotFound();
            }

            if (Request.Content != null && Request.Content.IsFormData())
            {
                var formData = await Request.Content.ReadAsFormDataAsync();

                foreach (var key in formData.AllKeys)
                {
                    var value = formData[key];

                    _messageQueue.Publish(new UpdateVariableMessage(webHook.GatewayName, webHook.DeviceId, key, value));
                }
            }

            return Ok();
        }

        [HttpGet, Route("{gatewayName}/{deviceId}")]
        public async Task<IHttpActionResult> GetUrls(string gatewayName, string deviceId)
        {
            var webHooks = await _webHookService.GetWebHooksAsync(gatewayName, deviceId);
            var urls = new List<string>();
            var prefix = $"http://{Request.RequestUri.Authority}/api/v1/webhook/";

            foreach (var webHook in webHooks)
            {
                urls.Add(prefix + webHook.Id);
            }

            return Ok(urls);
        }
    }
}
