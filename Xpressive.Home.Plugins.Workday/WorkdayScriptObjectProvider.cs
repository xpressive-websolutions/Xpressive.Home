using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.Workday
{
    internal sealed class WorkdayScriptObjectProvider : IScriptObjectProvider
    {
        private readonly IWorkdayGateway _gateway;
        private readonly IWorkdayCalculator _calculator;

        public WorkdayScriptObjectProvider(IWorkdayGateway gateway, IWorkdayCalculator calculator)
        {
            _gateway = gateway;
            _calculator = calculator;
        }

        public IEnumerable<Tuple<string, object>> GetObjects()
        {
            yield break;
        }

        public IEnumerable<Tuple<string, Delegate>> GetDelegates()
        {
            // workday("id").isWorkday(new date())

            var deviceResolver = new Func<string, WorkdayScriptObject>(id =>
            {
                var device = _gateway.GetDevices().SingleOrDefault(d => d.Id.Equals(id));
                return new WorkdayScriptObject(device, _calculator);
            });

            yield return new Tuple<string, Delegate>("workday", deviceResolver);
        }

        public class WorkdayScriptObject
        {
            private readonly WorkdayDevice _device;
            private readonly IWorkdayCalculator _calculator;

            public WorkdayScriptObject(WorkdayDevice device, IWorkdayCalculator calculator)
            {
                _device = device;
                _calculator = calculator;
            }

            public object isWorkday(DateTime date)
            {
                if (_device == null)
                {
                    Log.Warning("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _calculator.GetWorkdays(_device, date.Date, date.Date).Any();
            }

            public object isHoliday(DateTime date)
            {
                if (_device == null)
                {
                    Log.Warning("Unable to get variable value because the device was not found.");
                    return null;
                }

                return _calculator.GetHolidays(_device, date.Date, date.Date).Any();
            }
        }
    }
}
