using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NPoco;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Services
{
    internal sealed class WebHookService : IWebHookService
    {
        private readonly IBase62Converter _base62Converter;

        public WebHookService(IBase62Converter base62Converter)
        {
            _base62Converter = base62Converter;
        }

        public async Task<IWebHook> RegisterNewWebHookAsync(string gatewayName, string id, IDevice device)
        {
            var webhook = new WebHook
            {
                Id = id,
                GatewayName = gatewayName,
                DeviceId = device.Id
            };

            using (var database = new Database("ConnectionString"))
            {
                await database.InsertAsync("WebHook", "Id", false, webhook);
            }

            return webhook;
        }

        public async Task<IWebHook> RegisterNewWebHookAsync(string gatewayName, IDevice device)
        {
            return await RegisterNewWebHookAsync(gatewayName, GenerateId(), device);
        }

        public async Task<IWebHook> GetWebHookAsync(string id)
        {
            using (var database = new Database("ConnectionString"))
            {
                var result = await database.FetchAsync<WebHook>("select * from WebHook where Id = @0", id);
                return result.SingleOrDefault();
            }
        }

        public async Task<IEnumerable<IWebHook>> GetWebHooksAsync(string gatewayName, string deviceId)
        {
            using (var database = new Database("ConnectionString"))
            {
                return await database.FetchAsync<WebHook>("select * from WebHook where GatewayName = @0 and DeviceId = @1", gatewayName, deviceId);
            }
        }

        public string GenerateId()
        {
            using (var cryptoServiceProvider = new RNGCryptoServiceProvider())
            {
                var binary = new byte[14];
                cryptoServiceProvider.GetNonZeroBytes(binary);
                return _base62Converter.ToBase62(binary).Substring(0, 16);
            }
        }
    }

    internal sealed class WebHook : IWebHook
    {
        public string Id { get; set; }
        public string GatewayName { get; set; }
        public string DeviceId { get; set; }
    }
}
