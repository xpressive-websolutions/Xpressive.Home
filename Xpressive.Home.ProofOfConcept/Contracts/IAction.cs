using System.Collections.Generic;

namespace Xpressive.Home.ProofOfConcept
{
    public interface IAction
    {
        string Name { get; }
        IList<string> Fields { get; }
    }
}