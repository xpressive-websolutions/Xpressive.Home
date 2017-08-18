using System;
using System.Collections.Generic;
using System.Linq;

namespace Xpressive.Home.Variables
{
    internal sealed class LimitedVariableBuffer
    {
        private readonly double _limitInHours;
        private readonly List<Tuple<DateTime, object>> _values = new List<Tuple<DateTime, object>>();
        private readonly object _lock = new object();

        public LimitedVariableBuffer(double limitInHours)
        {
            _limitInHours = limitInHours;
        }

        public void Add(object value)
        {
            lock (_lock)
            {
                _values.Add(Tuple.Create(DateTime.UtcNow, value));
                CleanUp();
            }
        }

        public IEnumerable<Tuple<DateTime, object>> Get()
        {
            List<Tuple<DateTime, object>> result;

            lock (_lock)
            {
                CleanUp();
                result = _values.ToList();
            }

            return result.OrderBy(t => t.Item1);
        }

        private void CleanUp()
        {
            var limit = DateTime.UtcNow.AddHours(-_limitInHours);
            _values.RemoveAll(t => t.Item1 < limit);
        }
    }
}
