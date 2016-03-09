using System;
using System.Collections.Generic;
using System.Linq;

namespace Xpressive.Home.ProofOfConcept
{
    //internal class SubscriptionPropertyObserver : ISubscriptionPropertyObserver
    //{
    //    private readonly ISubscriptionService _subscriptionService;
    //    private readonly IDevicePropertyStore _devicePropertyStore;
    //    private readonly IGatewayResolver _gatewayResolver;
    //    private readonly Dictionary<ISubscription, DateTime> _recentExecution;
    //    private readonly object _executionLock = new object();

    //    public SubscriptionPropertyObserver(ISubscriptionService subscriptionService, IDevicePropertyStore devicePropertyStore, IGatewayResolver gatewayResolver)
    //    {
    //        _subscriptionService = subscriptionService;
    //        _devicePropertyStore = devicePropertyStore;
    //        _gatewayResolver = gatewayResolver;
    //        _recentExecution = new Dictionary<ISubscription, DateTime>();

    //        _devicePropertyStore.DevicePropertyChanged += (s, e) =>
    //        {
    //            lock (_executionLock)
    //            {
    //                var subscriptions = _subscriptionService.Get().Where(IsValid).ToList();
    //                subscriptions.ForEach(Execute);
    //            }
    //        };
    //    }

    //    private void Execute(ISubscription subscription)
    //    {
    //        DateTime recentExecution;
    //        if (_recentExecution.TryGetValue(subscription, out recentExecution) && DateTime.UtcNow < recentExecution + subscription.WaitTime)
    //        {
    //            return;
    //        }

    //        _recentExecution[subscription] = DateTime.UtcNow;
    //        Console.WriteLine("Execute {0}.{1}", subscription.Action.GatewayName, subscription.Action.ActionName);

    //        var gateway = _gatewayResolver.Resolve(subscription.Action.GatewayName);
    //        gateway.Execute(subscription.Action);
    //    }

    //    private bool IsValid(ISubscription subscription)
    //    {
    //        return subscription.DeviceSubscriptions.All(IsValid);
    //    }

    //    private bool IsValid(IDeviceSubscription deviceSubscription)
    //    {
    //        var gatewayName = deviceSubscription.GatewayName;
    //        var deviceId = deviceSubscription.DeviceId;

    //        foreach (var propertyValue in deviceSubscription.DevicePropertyValues)
    //        {
    //            var persistedValue = _devicePropertyStore.Get(gatewayName, deviceId, propertyValue.Key);

    //            if (!string.Equals(persistedValue, propertyValue.Value, StringComparison.Ordinal))
    //            {
    //                return false;
    //            }
    //        }

    //        return true;
    //    }
    //}
}