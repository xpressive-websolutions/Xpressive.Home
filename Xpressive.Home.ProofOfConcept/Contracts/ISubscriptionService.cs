using System.Collections.Generic;

namespace Xpressive.Home.ProofOfConcept
{
    public interface ISubscriptionService
    {
        void Add(ISubscription subscription);

        void Delete(ISubscription subscription);

        IEnumerable<ISubscription> Get();
    }
}