using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Automation
{
    public interface IScriptEngine
    {
        Task ExecuteAsync(string scriptId, string triggerVariable, object triggerValue);

        Task ExecuteEvenIfDisabledAsync(string scriptId);
    }
}
