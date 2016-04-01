using System;

namespace Xpressive.Home.Contracts.Gateway
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class DevicePropertyAttribute : Attribute
    {
        private readonly int _sortOrder;

        public DevicePropertyAttribute(int sortOrder)
        {
            _sortOrder = sortOrder;
        }

        public int SortOrder => _sortOrder;
    }
}
