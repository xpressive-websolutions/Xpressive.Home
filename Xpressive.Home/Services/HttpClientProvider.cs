using System.Net.Http;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Services
{
    internal sealed class HttpClientProvider : IHttpClientProvider
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public HttpClient Get()
        {
            return _httpClient;
        }
    }
}
