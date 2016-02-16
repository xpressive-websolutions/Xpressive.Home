using System;
using System.Collections.Generic;

namespace Xpressive.Home.ProofOfConcept
{
    public interface ISubscription
    {
        string Name { get; }

        /// <summary>
        /// Time to wait until subscription is evaluated again after successful evaluation.
        /// </summary>
        TimeSpan WaitTime { get; }

        /// <summary>
        /// All these subscriptions must be valid to enable the subscription
        /// </summary>
        IEnumerable<IDeviceSubscription> DeviceSubscriptions { get; }

        IDeviceAction Action { get; }
    }
}