using System.Collections.Generic;
using System.Linq;

namespace Xpressive.Home.ProofOfConcept
{
    internal class SubscriptionService : ISubscriptionService
    {
        private readonly List<ISubscription> _subscriptions;
        private readonly object _lock = new object();

        public SubscriptionService()
        {
            _subscriptions = new List<ISubscription>();
        }

        public void Add(ISubscription subscription)
        {
            lock (_lock)
            {
                _subscriptions.Add(subscription);
            }
        }

        public void Delete(ISubscription subscription)
        {
            lock (_lock)
            {
                _subscriptions.Remove(subscription);
            }
        }

        public IEnumerable<ISubscription> Get()
        {
            lock (_lock)
            {
                return _subscriptions.ToList();
            }
        }
    }
}