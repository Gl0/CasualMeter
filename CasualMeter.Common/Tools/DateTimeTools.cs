using System;

namespace CasualMeter.Common.Tools
{
    public static class DateTimeTools
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);

        /// <summary>
        /// Converts Unix Epoch time to local DateTime
        /// </summary>
        /// <param name="unixTimeStamp"></param>
        /// <returns></returns>
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            return Epoch.AddSeconds(unixTimeStamp).ToLocalTime();
        }

        /// <summary>
        /// Converts DateTime to unix epoch time
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static double DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (TimeZoneInfo.ConvertTimeToUtc(dateTime) - Epoch).TotalSeconds;
        }
    }
}
