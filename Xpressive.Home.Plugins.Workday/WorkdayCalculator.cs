using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Xpressive.Home.Plugins.Workday
{
    internal sealed class WorkdayCalculator : IWorkdayCalculator
    {
        private readonly Regex _dateRegex = new Regex(
            "[A-Z]{3}[0-9]{1,2}",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(1));
        private readonly Regex _easterRegex = new Regex(
            "EASTER(?<days>[\\-\\+][0-9]{1,2})",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(1));

        // https://de.wikipedia.org/wiki/Spencers_Osterformel
        public DateTime GetEasterDate(int year)
        {
            var a = year % 19;
            var b = year / 100;
            var c = year % 100;
            var d = b / 4;
            var e = b % 4;
            var f = (b + 8) / 25;
            var g = (b - f + 1) / 3;
            var h = (19 * a + b - d - g + 15) % 30;
            var i = c / 4;
            var k = c % 4;
            var l = (32 + 2 * e + 2 * i - h - k) % 7;
            var m = (a + 11 * h + 22 * l) / 451;
            var n = (h + l - 7 * m + 114) / 31;
            var p = (h + l - 7 * m + 114) % 31;
            return new DateTime(year, n, p + 1);
        }

        public IEnumerable<DateTime> GetWorkdays(WorkdayDevice device, DateTime begin, DateTime end)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (begin > end)
            {
                var temp = begin;
                begin = end;
                end = temp;
            }

            var holidays = GetHolidays(device, begin, end).ToList();
            var workdays = device.Workdays.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var day in GetDays(begin, end))
            {
                if (holidays.Contains(day.Date))
                {
                    // is holiday
                    continue;
                }

                if (!workdays.Contains(day.ToString("ddd", CultureInfo.InvariantCulture), StringComparer.OrdinalIgnoreCase))
                {
                    // is weekend
                    continue;
                }

                yield return day.Date;
            }
        }

        public IEnumerable<DateTime> GetHolidays(WorkdayDevice device, DateTime begin, DateTime end)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (string.IsNullOrEmpty(device.Holidays))
            {
                return new List<DateTime>(0);
            }

            if (begin > end)
            {
                var temp = begin;
                begin = end;
                end = temp;
            }

            var holidays = device.Holidays.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<DateTime>();
            var easter = GetYears(begin, end).Select(GetEasterDate).ToList();

            foreach (var holiday in holidays)
            {
                if (_dateRegex.IsMatch(holiday))
                {
                    foreach (var year in GetYears(begin, end))
                    {
                        var withYear = $"{year:D}{holiday}";
                        DateTime parsed;
                        if (DateTime.TryParseExact(withYear, "yyyyMMMd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out parsed) &&
                            IsInRange(parsed, begin, end))
                        {
                            result.Add(parsed.Date);
                        }
                    }
                }
                else
                {
                    var easterMatch = _easterRegex.Match(holiday);
                    if (easterMatch.Success)
                    {
                        var days = int.Parse(easterMatch.Groups["days"].Value);

                        foreach (var e in easter)
                        {
                            var ed = e.AddDays(days).Date;
                            if (IsInRange(ed, begin, end))
                            {
                                result.Add(ed);
                            }
                        }
                    }
                }
            }

            result.Sort();
            return result;
        }

        private static bool IsInRange(DateTime date, DateTime begin, DateTime end)
        {
            return date.Date >= begin.Date && date.Date <= end.Date;
        }

        private static IEnumerable<int> GetYears(DateTime begin, DateTime end)
        {
            for (var y = begin.Year; y <= end.Year; y++)
            {
                yield return y;
            }
        }

        private static IEnumerable<DateTime> GetDays(DateTime begin, DateTime end)
        {
            for (var date = begin.Date; date.Date <= end.Date; date = date.AddDays(1))
            {
                yield return date.Date;
            }
        }
    }
}
