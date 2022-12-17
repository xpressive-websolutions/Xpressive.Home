using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Xpressive.Home.DatabaseModel
{
    public class ContextFactory : IContextFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ContextFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<T> InScope<T>(Func<XpressiveHomeContext, Task<T>> action)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<XpressiveHomeContext>();
                return await action(context);
            }
        }

        public async Task InScope(Func<XpressiveHomeContext, Task> action)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<XpressiveHomeContext>();
                await action(context);
            }
        }
    }
}
