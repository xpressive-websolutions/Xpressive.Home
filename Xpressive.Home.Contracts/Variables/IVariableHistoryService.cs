using System.Collections.Generic;

namespace Xpressive.Home.Contracts.Variables
{
    public interface IVariableHistoryService
    {
        IEnumerable<IVariableHistoryValue> Get(string name);
    }
}
