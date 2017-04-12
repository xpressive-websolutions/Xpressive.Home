using System.Net.Http;

namespace Xpressive.Home.Contracts.Services
{
    public interface IHttpClientProvider
    {
        HttpClient Get();
    }
}
