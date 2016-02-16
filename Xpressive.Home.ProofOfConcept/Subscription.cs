using System;
using System.Collections.Generic;

namespace Xpressive.Home.ProofOfConcept
{
    internal class Subscription : ISubscription
    {
        private readonly IDeviceAction _action;
        private readonly IEnumerable<IDeviceSubscription> _deviceSubscriptions;
        private readonly string _name;
        private TimeSpan _waitTime;

        public Subscription(string name, IDeviceAction action, IEnumerable<IDeviceSubscription> deviceSubscriptions)
        {
            _name = name;
            _action = action;
            _deviceSubscriptions = new List<IDeviceSubscription>(deviceSubscriptions);
            _waitTime = TimeSpan.Zero;
        }

        public IDeviceAction Action => _action;
        public IEnumerable<IDeviceSubscription> DeviceSubscriptions => _deviceSubscriptions;
        public string Name => _name;

        public TimeSpan WaitTime
        {
            get { return _waitTime; }
            set { _waitTime = value; }
        }
    }
}