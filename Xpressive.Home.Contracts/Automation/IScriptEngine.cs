using System.Threading.Tasks;

namespace Xpressive.Home.Contracts.Automation
{
    public interface IScriptEngine
    {
        void ExecuteWhenVariableChanges(string scriptId, string variable);

        Task ExecuteAsync(string scriptId);
    }
}