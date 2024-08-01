namespace PVLib
{
    public struct Time
    {
        public int Hour, Minute, Second, Millisecond, Day, Month, Year;
        Time(DateTime date)
        {
            Year = date.Year;
            Hour = date.Hour;
            Minute = date.Minute;
            Second = date.Second;
            Millisecond = date.Millisecond;
            Day = date.Day;
            Month = date.Month;

        }
        Time(TimeSpan span)
        {
            Hour = span.Hours;
            Minute = span.Minutes;
            Second = span.Seconds;
            Millisecond = span.Milliseconds;
            Day = span.Days;
            Month = 0;
            Year = 0;
        }
        public static implicit operator DateTime(Time time)
        {
            return new(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, time.Millisecond);
        }
        public static implicit operator Time(DateTime time)
        {
            return new(time);
        }
        public static implicit operator TimeSpan(Time time)
        {
            return new TimeSpan(0,time.Hour, time.Minute, time.Second,time.Millisecond);
        }
        public static implicit operator Time(TimeSpan time)
        {
            return new(time);
        }
    }
}
