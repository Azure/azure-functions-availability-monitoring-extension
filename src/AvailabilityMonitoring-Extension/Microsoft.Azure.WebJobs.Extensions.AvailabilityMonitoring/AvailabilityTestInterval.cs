using System;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    public static class AvailabilityTestInterval
    {
        private const string Moniker = "AvailabilityTestInterval";

        public const string Minute01 =  "% AvailabilityTestInterval.Minute01 %";
        public const string Minutes05 = "% AvailabilityTestInterval.Minutes05 %";
        public const string Minutes10 = "% AvailabilityTestInterval.Minutes10 %";
        public const string Minutes15 = "% AvailabilityTestInterval.Minutes15 %";

        private static class ValidSpecifiers
        {
            public static readonly string Minutes01 = RemoveEnclosingNameResolverTags("% AvailabilityTestInterval.Minutes01 %");
            public static readonly string Minute01 =  RemoveEnclosingNameResolverTags(AvailabilityTestInterval.Minute01);
            public static readonly string Minutes05 = RemoveEnclosingNameResolverTags(AvailabilityTestInterval.Minutes05);
            public static readonly string Minutes10 = RemoveEnclosingNameResolverTags(AvailabilityTestInterval.Minutes10);
            public static readonly string Minutes15 = RemoveEnclosingNameResolverTags(AvailabilityTestInterval.Minutes15);
        }

        private static readonly Random Rnd = new Random();


        internal static bool IsSpecification(string testIntervalSpec)
        {
            // Ignore nulls and empty strings:
            if (String.IsNullOrWhiteSpace(testIntervalSpec))
            {
                return false;
            }

            // If starts AND ends with '%', throw those away (this also trims):
            testIntervalSpec = RemoveEnclosingNameResolverTags(testIntervalSpec);

            // Check that the specified 'name' starts with the right prefix:
            return testIntervalSpec.StartsWith(Moniker, StringComparison.OrdinalIgnoreCase);
        }

        internal static int Parse(string testIntervalSpec)
        {
            // Ensure not null:
            testIntervalSpec = Convert.NotNullOrWord(testIntervalSpec);

            // Remove '%' (if any) and trim:
            testIntervalSpec = RemoveEnclosingNameResolverTags(testIntervalSpec);

            if (AvailabilityTestInterval.ValidSpecifiers.Minute01.Equals(testIntervalSpec, StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (AvailabilityTestInterval.ValidSpecifiers.Minutes01.Equals(testIntervalSpec, StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (AvailabilityTestInterval.ValidSpecifiers.Minutes05.Equals(testIntervalSpec, StringComparison.OrdinalIgnoreCase))
            {
                return 5;
            }

            if (AvailabilityTestInterval.ValidSpecifiers.Minutes10.Equals(testIntervalSpec, StringComparison.OrdinalIgnoreCase))
            {
                return 10;
            }

            if (AvailabilityTestInterval.ValidSpecifiers.Minutes15.Equals(testIntervalSpec, StringComparison.OrdinalIgnoreCase))
            {
                return 15;
            }

            throw new FormatException($"Invalid availability test interval specification: \"{testIntervalSpec}\""
                                    + $" (Expected format is \"{Moniker}.MinSpec\", where \'MinSpec\' is one of"
                                    + $" {{\"{nameof(ValidSpecifiers.Minute01)}\", \"{nameof(ValidSpecifiers.Minutes05)}\","
                                    + $" \"{nameof(ValidSpecifiers.Minutes10)}\", \"{nameof(ValidSpecifiers.Minutes15)}\"}}).");
        }

        internal static string CreateCronIntervalSpecWithRandomOffset(int intervalMins)
        {
            // The basic format of the CRON expressions is:
            // {second} {minute} {hour} {day} {month} {day of the week}
            // E.g.
            //     TimerTrigger("15 2/5 * * * *")
            // means every 5 minutes starting at 2, on 15 secs past the minute, i.e., 02:15, 07:15, 12:15, 17:15, ...

            if (intervalMins != 1 && intervalMins != 5 && intervalMins != 10 && intervalMins != 15)
            {
                throw new ArgumentException($"Invalid number of minutes in the interval: valid values are M = 1, 5, 10 or 15; specified value is \'{intervalMins}\'.");
            }

            int intervalTotalSecs = intervalMins * 60;
            int rndOffsTotalSecs = Rnd.Next(0, intervalTotalSecs);
            int rndOffsWholeMins = rndOffsTotalSecs / 60;
            int rndOffsSubminSecs = rndOffsTotalSecs % 60;

            string cronSpec = $"{rndOffsSubminSecs} {rndOffsWholeMins}/{intervalMins} * * * *";
            return cronSpec;
        }

        private static string RemoveEnclosingNameResolverTags(string s)
        {
            // Trim:
            s = s.Trim();

            // If starts AND ends with '%', remove those:
            while (s.Length > 2 && s.StartsWith("%") && s.EndsWith("%"))
            {
                s = s.Substring(1, s.Length - 2);
                s = s.Trim();
            }

            return s;
        }
    }
}
