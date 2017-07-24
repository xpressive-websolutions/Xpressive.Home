using System;
using System.Collections.Generic;

namespace Xpressive.Home.Plugins.Workday
{
    internal interface IWorkdayCalculator
    {
        DateTime GetEasterDate(int year);

        IEnumerable<DateTime> GetWorkdays(WorkdayDevice device, DateTime begin, DateTime end);

        IEnumerable<DateTime> GetHolidays(WorkdayDevice device, DateTime begin, DateTime end);
    }
}
