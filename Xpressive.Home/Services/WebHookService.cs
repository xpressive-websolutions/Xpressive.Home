using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Services;
using Xpressive.Home.DatabaseModel;

namespace Xpressive.Home.Services
{
    internal sealed class WebHookService : IWebHookService
    {
        private readonly IBase62Converter _base62Converter;
        private readonly IContextFactory _contextFactory;

        public WebHookService(IBase62Converter base62Converter, IContextFactory contextFactory)
        {
            _base62Converter = base62Converter;
            _contextFactory = contextFactory;
        }

        public async Task<IWebHook> RegisterNewWebHookAsync(string gatewayName, string id, IDevice device)
        {
            var webhook = new WebHook
            {
                Id = id,
                GatewayName = gatewayName,
                DeviceId = device.Id
            };

            await _contextFactory.InScope(async context =>
            {
                context.WebHook.Add(webhook);
                await context.SaveChangesAsync();
            });

            return webhook;
        }

        public async Task<IWebHook> RegisterNewWebHookAsync(string gatewayName, IDevice device)
        {
            return await RegisterNewWebHookAsync(gatewayName, GenerateId(), device);
        }

        public async Task<IWebHook> GetWebHookAsync(string id)
        {
            return await _contextFactory.InScope(async context => await context.WebHook.FindAsync(id));
        }

        public async Task<IEnumerable<IWebHook>> GetWebHooksAsync(string gatewayName, string deviceId)
        {
            return await _contextFactory.InScope(async context => await context.WebHook.Where(w => w.GatewayName == gatewayName && w.DeviceId == deviceId).ToListAsync());
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
}
