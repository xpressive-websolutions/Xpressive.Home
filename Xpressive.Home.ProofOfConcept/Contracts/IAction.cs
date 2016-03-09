using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Xpressive.Home.ProofOfConcept
{
    public interface IAction
    {
        string Name { get; }
        IList<string> Fields { get; }
    }

    public interface IIpAddressService
    {
        string GetIpAddress();

        IEnumerable<string> GetOtherIpAddresses();
        IEnumerable<string> GetOtherIpAddresses(string ipAddress);
    }

    internal class IpAddressService : IIpAddressService
    {
        public IEnumerable<string> GetOtherIpAddresses()
        {
            var ipAddress = GetIpAddress();
            return GetOtherIpAddresses(ipAddress);
        }

        public IEnumerable<string> GetOtherIpAddresses(string ipAddress)
        {
            var parts = ipAddress.Split('.');
            var prefix = string.Join(".", parts.Take(3));

            for (var i = 0; i < 256; i++)
            {
                yield return $"{prefix}.{i}";
            }
        }

        public string GetIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }

            return string.Empty;
        }
    }

    public interface IProperty
    {
        string Name { get; }
        bool IsReadOnly { get; }

        bool IsValidValue(string value);
    }

    public abstract class PropertyBase : IProperty
    {
        private readonly string _name;
        private readonly bool _isReadOnly;

        protected PropertyBase(string name, bool isReadOnly = true)
        {
            _name = name;
            _isReadOnly = isReadOnly;
        }

        public string Name => _name;
        public bool IsReadOnly => _isReadOnly;

        public abstract bool IsValidValue(string value);
    }

    public class NumericProperty : PropertyBase
    {
        private readonly double _minValue;
        private readonly double _maxValue;

        public NumericProperty(string name, double minValue, double maxValue, bool isReadOnly = true) : base(name, isReadOnly)
        {
            _minValue = minValue;
            _maxValue = maxValue;
        }

        public double MinValue => _minValue;
        public double MaxValue => _maxValue;

        public override bool IsValidValue(string value)
        {
            double d;
            return double.TryParse(value, out d) && d >= _minValue && d <= _maxValue;
        }
    }

    public class TextProperty : PropertyBase
    {
        public TextProperty(string name, bool isReadOnly = true) : base(name, isReadOnly) { }

        public override bool IsValidValue(string value)
        {
            return true;
        }
    }

    public class ColorProperty : PropertyBase
    {
        public ColorProperty(string name, bool isReadOnly = true) : base(name, isReadOnly) { }

        public override bool IsValidValue(string value)
        {
            return true;
        }
    }

    public class BoolProperty : PropertyBase
    {
        public BoolProperty(string name, bool isReadOnly = true) : base(name, isReadOnly) { }

        public override bool IsValidValue(string value)
        {
            return true;
        }
    }

    //public class DateProperty : PropertyBase
    //{
    //    public DateProperty(string name, bool isReadOnly = true) : base(name, isReadOnly) { }

    //    public override bool IsValidValue(string value)
    //    {
    //        return true;
    //    }
    //}

    //public class DateTimeProperty : PropertyBase
    //{
    //    public DateTimeProperty(string name, bool isReadOnly = true) : base(name, isReadOnly) { }

    //    public override bool IsValidValue(string value)
    //    {
    //        return true;
    //    }
    //}

    //public class TimeProperty : PropertyBase
    //{
    //    public TimeProperty(string name, bool isReadOnly = true) : base(name, isReadOnly) { }

    //    public override bool IsValidValue(string value)
    //    {
    //        return true;
    //    }
    //}

    //public class WeekdayProperty : PropertyBase
    //{
    //    public WeekdayProperty(string name, bool isReadOnly = true) : base(name, isReadOnly) { }

    //    public override bool IsValidValue(string value)
    //    {
    //        return true;
    //    }
    //}
}