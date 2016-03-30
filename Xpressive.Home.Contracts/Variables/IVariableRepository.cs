using System.Collections.Generic;

namespace Xpressive.Home.Contracts.Variables
{
    public interface IVariableRepository
    {
        T Get<T>(string name) where T : IVariable;

        IEnumerable<IVariable> Get();
    }
}