using System;

namespace Xpressive.Home.Plugins.Daylight
{
    /// from http://pointofint.blogspot.ch/2014/06/sunrise-and-sunset-in-c.html
    internal static class SunsetCalculator
    {
        public static TimeSpan GetSunrise(double latitude, double longitude)
        {
            return GetSunrise(DateTime.Now.Date, latitude, longitude);
        }

        public static TimeSpan GetSunrise(DateTime date, double latitude, double longitude)
        {
            date = date.Date;
            var jd = CalcJd(date);
            var sunRise = CalcSunRiseUtc(jd, latitude, longitude);
            var utc = GetDateTime(sunRise, date);

            if (utc.HasValue)
            {
                return utc.Value.TimeOfDay;
            }

            return new TimeSpan(6, 0, 0);
        }

        public static TimeSpan GetSunset(double latitude, double longitude)
        {
            return GetSunset(DateTime.Now.Date, latitude, longitude);
        }

        public static TimeSpan GetSunset(DateTime date, double latitude, double longitude)
        {
            date = date.Date;
            var jd = CalcJd(date);
            var sunSet = CalcSunSetUtc(jd, latitude, longitude);
            var utc = GetDateTime(sunSet, date);

            if (utc.HasValue)
            {
                return utc.Value.TimeOfDay;
            }

            return new TimeSpan(18, 0, 0);
        }

        private static double RadToDeg(double angleRad)
        {
            return (180.0 * angleRad / Math.PI);
        }

        private static double DegToRad(double angleDeg)
        {
            return (Math.PI * angleDeg / 180.0);
        }

        //***********************************************************************/
        //* Name: calcJD	
        //* Type: Function	
        //* Purpose: Julian day from calendar day	
        //* Arguments:	
        //* year : 4 digit year	
        //* month: January = 1	
        //* day : 1 - 31	
        //* Return value:	
        //* The Julian day corresponding to the date	
        //* Note:	
        //* Number is returned for start of day. Fractional days should be	
        //* added later.	
        //***********************************************************************/
        private static double CalcJd(int year, int month, int day)
        {
            if (month <= 2)
            {
                year -= 1;
                month += 12;
            }
            var a = Math.Floor(year / 100.0);
            var b = 2 - a + Math.Floor(a / 4);

            var jd = Math.Floor(365.25 * (year + 4716)) + Math.Floor(30.6001 * (month + 1)) + day + b - 1524.5;
            return jd;
        }

        private static double CalcJd(DateTime date)
        {
            return CalcJd(date.Year, date.Month, date.Day);
        }

        //***********************************************************************/
        //* Name: calcTimeJulianCent	
        //* Type: Function	
        //* Purpose: convert Julian Day to centuries since J2000.0.	
        //* Arguments:	
        //* jd : the Julian Day to convert	
        //* Return value:	
        //* the T value corresponding to the Julian Day	
        //***********************************************************************/
        private static double CalcTimeJulianCent(double jd)
        {
            var t = (jd - 2451545.0) / 36525.0;
            return t;
        }

        //***********************************************************************/
        //* Name: calGeomMeanLongSun	
        //* Type: Function	
        //* Purpose: calculate the Geometric Mean Longitude of the Sun	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* Return value:	
        //* the Geometric Mean Longitude of the Sun in degrees	
        //***********************************************************************/
        private static double CalcGeomMeanLongSun(double t)
        {
            var l0 = 280.46646 + t * (36000.76983 + 0.0003032 * t);
            while (l0 > 360.0)
            {
                l0 -= 360.0;
            }
            while (l0 < 0.0)
            {
                l0 += 360.0;
            }
            return l0;	 // in degrees
        }

        //***********************************************************************/
        //* Name: calGeomAnomalySun	
        //* Type: Function	
        //* Purpose: calculate the Geometric Mean Anomaly of the Sun	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* Return value:	
        //* the Geometric Mean Anomaly of the Sun in degrees	
        //***********************************************************************/
        private static double CalcGeomMeanAnomalySun(double t)
        {
            var m = 357.52911 + t * (35999.05029 - 0.0001537 * t);
            return m;	 // in degrees
        }

        //***********************************************************************/
        //* Name: calcEccentricityEarthOrbit	
        //* Type: Function	
        //* Purpose: calculate the eccentricity of earth's orbit	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* Return value:	
        //* the unitless eccentricity	
        //***********************************************************************/
        private static double CalcEccentricityEarthOrbit(double t)
        {
            var e = 0.016708634 - t * (0.000042037 + 0.0000001267 * t);
            return e;	 // unitless
        }

        //***********************************************************************/
        //* Name: calcSunEqOfCenter	
        //* Type: Function	
        //* Purpose: calculate the equation of center for the sun	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* Return value:	
        //* in degrees	
        //***********************************************************************/
        private static double CalcSunEqOfCenter(double t)
        {
            var m = CalcGeomMeanAnomalySun(t);

            var mrad = DegToRad(m);
            var sinm = Math.Sin(mrad);
            var sin2M = Math.Sin(mrad + mrad);
            var sin3M = Math.Sin(mrad + mrad + mrad);

            var c = sinm * (1.914602 - t * (0.004817 + 0.000014 * t)) + sin2M * (0.019993 - 0.000101 * t) + sin3M * 0.000289;
            return c;	 // in degrees
        }

        //***********************************************************************/
        //* Name: calcSunTrueLong	
        //* Type: Function	
        //* Purpose: calculate the true longitude of the sun	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* Return value:	
        //* sun's true longitude in degrees	
        //***********************************************************************/
        private static double CalcSunTrueLong(double t)
        {
            var l0 = CalcGeomMeanLongSun(t);
            var c = CalcSunEqOfCenter(t);

            var o = l0 + c;
            return o;	 // in degrees
        }

        //***********************************************************************/
        //* Name: calcSunApparentLong	
        //* Type: Function	
        //* Purpose: calculate the apparent longitude of the sun	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* Return value:	
        //* sun's apparent longitude in degrees	
        //***********************************************************************/
        private static double CalcSunApparentLong(double t)
        {
            var o = CalcSunTrueLong(t);

            var omega = 125.04 - 1934.136 * t;
            var lambda = o - 0.00569 - 0.00478 * Math.Sin(DegToRad(omega));
            return lambda;	 // in degrees
        }

        //***********************************************************************/
        //* Name: calcMeanObliquityOfEcliptic	
        //* Type: Function	
        //* Purpose: calculate the mean obliquity of the ecliptic	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* Return value:	
        //* mean obliquity in degrees	
        //***********************************************************************/
        private static double CalcMeanObliquityOfEcliptic(double t)
        {
            var seconds = 21.448 - t * (46.8150 + t * (0.00059 - t * (0.001813)));
            var e0 = 23.0 + (26.0 + (seconds / 60.0)) / 60.0;
            return e0;	 // in degrees
        }

        //***********************************************************************/
        //* Name: calcObliquityCorrection	
        //* Type: Function	
        //* Purpose: calculate the corrected obliquity of the ecliptic	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* Return value:	
        //* corrected obliquity in degrees	
        //***********************************************************************/
        private static double CalcObliquityCorrection(double t)
        {
            var e0 = CalcMeanObliquityOfEcliptic(t);

            var omega = 125.04 - 1934.136 * t;
            var e = e0 + 0.00256 * Math.Cos(DegToRad(omega));
            return e;	 // in degrees
        }

        //***********************************************************************/
        //* Name: calcSunDeclination	
        //* Type: Function	
        //* Purpose: calculate the declination of the sun	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* Return value:	
        //* sun's declination in degrees	
        //***********************************************************************/
        private static double CalcSunDeclination(double t)
        {
            var e = CalcObliquityCorrection(t);
            var lambda = CalcSunApparentLong(t);
            var sint = Math.Sin(DegToRad(e)) * Math.Sin(DegToRad(lambda));
            var theta = RadToDeg(Math.Asin(sint));
            return theta;	 // in degrees
        }

        //***********************************************************************/
        //* Name: calcEquationOfTime	
        //* Type: Function	
        //* Purpose: calculate the difference between true solar time and mean	
        //*	 solar time	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* Return value:	
        //* equation of time in minutes of time	
        //***********************************************************************/
        private static double CalcEquationOfTime(double t)
        {
            var epsilon = CalcObliquityCorrection(t);
            var l0 = CalcGeomMeanLongSun(t);
            var e = CalcEccentricityEarthOrbit(t);
            var m = CalcGeomMeanAnomalySun(t);

            var y = Math.Tan(DegToRad(epsilon) / 2.0);
            y *= y;

            var sin2L0 = Math.Sin(2.0 * DegToRad(l0));
            var sinm = Math.Sin(DegToRad(m));
            var cos2L0 = Math.Cos(2.0 * DegToRad(l0));
            var sin4L0 = Math.Sin(4.0 * DegToRad(l0));
            var sin2M = Math.Sin(2.0 * DegToRad(m));

            var etime = y * sin2L0 - 2.0 * e * sinm + 4.0 * e * y * sinm * cos2L0 - 0.5 * y * y * sin4L0 - 1.25 * e * e * sin2M;

            return RadToDeg(etime) * 4.0;	// in minutes of time
        }

        //***********************************************************************/
        //* Name: calcHourAngleSunrise	
        //* Type: Function	
        //* Purpose: calculate the hour angle of the sun at sunrise for the	
        //*	 latitude	
        //* Arguments:	
        //* lat : latitude of observer in degrees	
        //*	solarDec : declination angle of sun in degrees	
        //* Return value:	
        //* hour angle of sunrise in radians	
        //***********************************************************************/
        private static double CalcHourAngleSunrise(double lat, double solarDec)
        {
            var latRad = DegToRad(lat);
            var sdRad = DegToRad(solarDec);
            var ha = (Math.Acos(Math.Cos(DegToRad(90.833)) / (Math.Cos(latRad) * Math.Cos(sdRad)) - Math.Tan(latRad) * Math.Tan(sdRad)));
            return ha;	 // in radians
        }

        //***********************************************************************/
        //* Name: calcSunsetUTC	
        //* Type: Function	
        //* Purpose: calculate the Universal Coordinated Time (UTC) of sunset	
        //*	 for the given day at the given location on earth	
        //* Arguments:	
        //* JD : julian day	
        //* latitude : latitude of observer in degrees	
        //* longitude : longitude of observer in degrees	
        //* Return value:	
        //* time in minutes from zero Z	
        //***********************************************************************/
        private static double CalcSunSetUtc(double jd, double latitude, double longitude)
        {
            var t = CalcTimeJulianCent(jd);
            var eqTime = CalcEquationOfTime(t);
            var solarDec = CalcSunDeclination(t);
            var hourAngle = CalcHourAngleSunrise(latitude, solarDec);
            hourAngle = -hourAngle;
            var delta = longitude + RadToDeg(hourAngle);
            var timeUtc = 720 - (4.0 * delta) - eqTime;	// in minutes
            return timeUtc;
        }

        private static double CalcSunRiseUtc(double jd, double latitude, double longitude)
        {
            var t = CalcTimeJulianCent(jd);
            var eqTime = CalcEquationOfTime(t);
            var solarDec = CalcSunDeclination(t);
            var hourAngle = CalcHourAngleSunrise(latitude, solarDec);
            var delta = longitude + RadToDeg(hourAngle);
            var timeUtc = 720 - (4.0 * delta) - eqTime;	// in minutes
            return timeUtc;
        }

        private static DateTime? GetDateTime(double minutes, DateTime date)
        {
            if ((minutes >= 0) && (minutes < 1440))
            {
                var floatHour = minutes / 60.0;
                var hour = Math.Floor(floatHour);
                var floatMinute = 60.0 * (floatHour - Math.Floor(floatHour));
                var minute = Math.Floor(floatMinute);
                var floatSec = 60.0 * (floatMinute - Math.Floor(floatMinute));
                var second = Math.Floor(floatSec + 0.5);
                if (second > 59)
                {
                    second = 0;
                    minute += 1;
                }
                if ((second >= 30))
                    minute++;
                if (minute > 59)
                {
                    minute = 0;
                    hour += 1;
                }
                return new DateTime(date.Year, date.Month, date.Day, (int)hour, (int)minute, (int)second, DateTimeKind.Utc);
            }
            return null;
        }
    }
}