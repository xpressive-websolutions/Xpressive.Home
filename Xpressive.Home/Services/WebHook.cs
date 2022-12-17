using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Services
{
    public class WebHook : IWebHook
    {
        public string Id { get; set; }
        public string GatewayName { get; set; }
        public string DeviceId { get; set; }
    }
}
