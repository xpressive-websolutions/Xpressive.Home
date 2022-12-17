using System.Collections.Generic;
using System.Data.Common;
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
        private readonly DbConnection _dbConnection;

        public WebHookService(IBase62Converter base62Converter, DbConnection dbConnection)
        {
            _base62Converter = base62Converter;
            _dbConnection = dbConnection;
        }

        public async Task<IWebHook> RegisterNewWebHookAsync(string gatewayName, string id, IDevice device)
        {
            var webhook = new WebHook
            {
                Id = id,
                GatewayName = gatewayName,
                DeviceId = device.Id
            };

            using (var database = new Database(_dbConnection))
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
            using (var database = new Database(_dbConnection))
            {
                var result = await database.FetchAsync<WebHook>("select * from WebHook where Id = @0", id);
                return result.SingleOrDefault();
            }
        }

        public async Task<IEnumerable<IWebHook>> GetWebHooksAsync(string gatewayName, string deviceId)
        {
            using (var database = new Database(_dbConnection))
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
