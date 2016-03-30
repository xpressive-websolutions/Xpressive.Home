using System;

namespace Xpressive.Home.Contracts.Gateway
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class DevicePropertyAttribute : Attribute { }
}
