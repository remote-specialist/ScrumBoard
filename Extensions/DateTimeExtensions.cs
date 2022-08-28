namespace Extensions
{
    public static class DateTimeExtensions
    {
        public static int BusinessDaysUntil(this DateTime from, DateTime to)
            => !(from > to)
             ? Enumerable.Range(1, (int) to.Subtract(from).TotalDays)
                         .Select(x => from.AddDays(x))
                         .Count(x => x.DayOfWeek != DayOfWeek.Saturday 
                                  && x.DayOfWeek != DayOfWeek.Sunday)
             : 0;

        public static bool IsWorkingTime(this DateTime d)
        {
            var dateTime = d.ToUniversalTime() + TimeSpan.FromHours(3.0);
            switch (dateTime.DayOfWeek)
            {
                case DayOfWeek.Sunday:
                case DayOfWeek.Saturday:
                    return false;
                default:
                    return dateTime.Hour >= 7 && dateTime.Hour <= 23;
            }
        }
    }
}