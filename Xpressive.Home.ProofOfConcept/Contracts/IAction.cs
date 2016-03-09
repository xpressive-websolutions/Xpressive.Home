using System.Collections.Generic;

namespace Xpressive.Home.ProofOfConcept.Contracts
{
    public interface IAction
    {
        string Name { get; }
        IList<string> Fields { get; }
    }
}