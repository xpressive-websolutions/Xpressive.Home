using System;

namespace Xpressive.Home.ProofOfConcept
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class GatewayPropertyAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class DevicePropertyAttribute : Attribute { }
}