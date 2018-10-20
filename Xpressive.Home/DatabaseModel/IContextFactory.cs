using System;
using System.Threading.Tasks;

namespace Xpressive.Home.DatabaseModel
{
    public interface IContextFactory
    {
        Task<T> InScope<T>(Func<XpressiveHomeContext, Task<T>> action);

        Task InScope(Func<XpressiveHomeContext, Task> action);
    }
}
