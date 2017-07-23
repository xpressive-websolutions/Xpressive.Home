using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Xpressive.Home.Plugins.Workday.Tests
{
    public class WorkdayDeviceTest
    {
        private readonly ITestOutputHelper _output;

        public WorkdayDeviceTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Given_Winterthur()
        {
            var calculator = new WorkdayCalculator();

            var winterthur = new WorkdayDevice
            {
                Workdays = "MON,TUE,WED,THU,FRI",
                Holidays = "JAN1,JAN2,EASTER+0,EASTER+1,EASTER-2,MAY1,EASTER+39,EASTER+49,EASTER+50,EASTER-41,AUG1,DEC25,DEC26"
            };

            var begin = new DateTime(2017, 1, 1);
            var end = new DateTime(2017, 12, 31);
            var holidays = calculator.GetHolidays(winterthur, begin, end).ToList();
            var workdays = calculator.GetWorkdays(winterthur, begin, end).ToList();

            _output.WriteLine($"Holidays ({holidays.Count})");
            foreach (var holiday in holidays)
            {
                _output.WriteLine(holiday.ToLongDateString());
            }

            _output.WriteLine("");
            _output.WriteLine($"Workdays ({workdays.Count})");
            foreach (var workday in workdays)
            {
                _output.WriteLine(workday.ToLongDateString());
            }
        }

        [Fact]
        public void Given_Winterthur_Fasnacht()
        {
            var calculator = new WorkdayCalculator();

            var winterthur = new WorkdayDevice
            {
                Workdays = "MON,TUE,WED,THU,FRI",
                Holidays = "EASTER-41"
            };

            var begin = new DateTime(2000, 1, 1);
            var end = new DateTime(2035, 12, 31);
            var holidays = calculator.GetHolidays(winterthur, begin, end).ToList();

            _output.WriteLine($"Holidays");
            foreach (var holiday in holidays)
            {
                _output.WriteLine(holiday.ToLongDateString());
            }
        }
    }
}
