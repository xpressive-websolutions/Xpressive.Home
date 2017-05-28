using System;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Automation
{
    public interface IScriptEngine
    {
        Task ExecuteAsync(Guid scriptId, string triggerVariable, object triggerValue);

        Task ExecuteEvenIfDisabledAsync(Guid scriptId);
    }
}
