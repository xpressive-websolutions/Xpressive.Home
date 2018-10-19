using Microsoft.Extensions.DependencyInjection;

namespace Xpressive.Home.Contracts
{
    public interface IPlugin
    {
        void ConfigureServices(IServiceCollection services);
    }
}
