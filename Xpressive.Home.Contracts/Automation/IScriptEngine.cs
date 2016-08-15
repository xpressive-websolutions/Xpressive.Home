using System;
using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Automation
{
    public interface IScriptEngine
    {
        Task ExecuteAsync(Guid scriptId);

        Task ExecuteEvenIfDisabledAsync(Guid scriptId);
    }
}
