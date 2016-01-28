using System;

namespace ModestTree
{
    public static class DateTimeUtil
    {
        const int SECOND = 1;
        const int MINUTE = 60 * SECOND;
        const int HOUR = 60 * MINUTE;
        const int DAY = 24 * HOUR;
        const int MONTH = 30 * DAY;

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970,1,1,0,0,0,0,System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds( unixTimeStamp ).ToLocalTime();
            return dtDateTime;
        }

        public static string FormatFutureDateAsRelative(DateTime givenDate)
        {
            var ts = new TimeSpan(givenDate.Ticks - DateTime.UtcNow.Ticks);

            if (ts.Ticks < 0)
            {
                return "the past";
            }

            double delta = Math.Abs(ts.TotalSeconds);

            if (delta < 1 * MINUTE)
            {
                return ts.Seconds == 1 ? "one second" : ts.Seconds + " seconds";
            }

            if (delta < 45 * MINUTE)
            {
                return ts.Minutes == 1 ? "a minute" : ts.Minutes + " minutes";
            }

            if (delta < 24 * HOUR)
            {
                return ts.Hours <= 1 ? "an hour" : ts.Hours + " hours";
            }

            if (delta < 48 * HOUR)
            {
                return "tomorrow";
            }

            if (delta < 30 * DAY)
            {
                return ts.Days + " days";
            }

            if (delta < 12 * MONTH)
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "one month" : months + " months";
            }
            else
            {
                int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                return years <= 1 ? "one year" : years + " years";
            }
        }

        public static string FormatPastDateAsRelative(DateTime givenDate)
        {
            var ts = new TimeSpan(DateTime.UtcNow.Ticks - givenDate.Ticks);

            if (ts.Ticks < 0)
            {
                return "not yet";
            }

            double delta = Math.Abs(ts.TotalSeconds);

            if (delta < 1 * MINUTE)
            {
                return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";
            }

            if (delta < 45 * MINUTE)
            {
                return ts.Minutes == 1 ? "a minute ago" : ts.Minutes + " minutes ago";
            }

            if (delta < 24 * HOUR)
            {
                return ts.Hours <= 1 ? "an hour ago" : ts.Hours + " hours ago";
            }

            if (delta < 48 * HOUR)
            {
                return "yesterday";
            }

            if (delta < 30 * DAY)
            {
                return ts.Days + " days ago";
            }

            if (delta < 12 * MONTH)
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "one month ago" : months + " months ago";
            }
            else
            {
                int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                return years <= 1 ? "one year ago" : years + " years ago";
            }
        }
    }
}
