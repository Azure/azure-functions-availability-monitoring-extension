using System;
using Microsoft.Azure.WebJobs;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    /// <summary>
    /// The format we are looking for:
    ///     AvailabilityTestInterval(15 minutes)
    ///     AvailabilityTestInterval(10 minutes)
    ///     AvailabilityTestInterval(5 minutes)
    ///     AvailabilityTestInterval(1 minutes)
    ///     AvailabilityTestInterval(1 minute)
    /// We are NON-case-sensitive and ONLY the values 1, 5, 10 and 15 minutes are allowed.
    /// </summary>
    internal class AvailabilityTimerTriggerScheduleNameResolver : INameResolver
    {
        public const string AvailabilityTestIntervalMoniker = "AvailabilityTestInterval";

        private static readonly Random Rnd = new Random();

        public string Resolve(string testIntervalSpec)
        {
            // Unless we have the right prefix, ignore the name - someone else will resolve it:
            if (false == IsAvailabilityTestIntervalSpecification(testIntervalSpec))
            {
                return testIntervalSpec;
            }

            int minuteInterval = ParseMinuteInterval(testIntervalSpec);

            string cronSpec = CreateRandomOffsetIntervalSpec(minuteInterval);
            return cronSpec;
        }

        private static bool IsAvailabilityTestIntervalSpecification(string testIntervalSpec)
        {
            // Ignore nulls and empty strings:
            if (String.IsNullOrWhiteSpace(testIntervalSpec))
            {
                return false;
            }

            // Check that the specified 'name' starts with the right prefix:
            return testIntervalSpec.Trim().StartsWith(AvailabilityTestIntervalMoniker, StringComparison.OrdinalIgnoreCase);
        }


        private static int ParseMinuteInterval(string originalTestIntervalSpec)
        {
            // Ok, we have the prefix that makes this our business. Remove and trim:

            string processedTestIntervalSpec = originalTestIntervalSpec = originalTestIntervalSpec.Trim();
            processedTestIntervalSpec = processedTestIntervalSpec.Substring(AvailabilityTestIntervalMoniker.Length);
            processedTestIntervalSpec = processedTestIntervalSpec.Trim();

            // Chek for the parentheses around "(N minutes)" and remove them:

            if (false == processedTestIntervalSpec.StartsWith("(") || false == processedTestIntervalSpec.EndsWith(")"))
            {
                ThrowFormatException("At least one of the parentheses is missing", originalTestIntervalSpec);
            }

            if (processedTestIntervalSpec.Length < 3)
            {
                ThrowFormatException("There is no minutes-value provided in parentheses", originalTestIntervalSpec);
            }

            processedTestIntervalSpec = processedTestIntervalSpec.Substring(1, processedTestIntervalSpec.Length - 2);
            processedTestIntervalSpec = processedTestIntervalSpec.Trim();

            // The remaining string should end in "minutes". ("minute" is also acceptable.) Validate it and remove accordingly.

            if (processedTestIntervalSpec.EndsWith("minutes", StringComparison.OrdinalIgnoreCase))
            {
                processedTestIntervalSpec = processedTestIntervalSpec.Substring(0, processedTestIntervalSpec.Length - "minutes".Length);
            }
            else if (processedTestIntervalSpec.EndsWith("minute", StringComparison.OrdinalIgnoreCase))
            {
                processedTestIntervalSpec = processedTestIntervalSpec.Substring(0, processedTestIntervalSpec.Length - "minute".Length);
            }
            else
            {
                ThrowFormatException("The interval provided in parentheses cannot be recognized (missing \"minutes\" unit name?)", originalTestIntervalSpec);
            }

            processedTestIntervalSpec = processedTestIntervalSpec.Trim();
            int intervalMinutes = -1;

            try
            {
                intervalMinutes = Int32.Parse(processedTestIntervalSpec);
            }
            catch (FormatException frmEx)
            {
                ThrowFormatException("Cannot parse the number of minutes in the interval", originalTestIntervalSpec, frmEx);
            }

            if (intervalMinutes == 1 || intervalMinutes == 5 || intervalMinutes == 10 || intervalMinutes == 15)
            {
                return intervalMinutes;
            }

            ThrowFormatException($"Invalid number of minutes in the interval: valid values are N = 1, 5, 10 or 15; specified value is \'{intervalMinutes}\'", originalTestIntervalSpec);
            return -1;
        }

        private static void ThrowFormatException(string errorInfo, string userInput)
        {
            ThrowFormatException(errorInfo, userInput, innerException: null);
        }

        private static void ThrowFormatException(string errorInfo, string userInput, Exception innerException)
        {
            const string FormatDefinitionInfo = "Expected format for the availability test interval specification is \"AvailabilityTestInterval(N minutes)\"";

            string message = $"{FormatDefinitionInfo}. {errorInfo}. (Actually encountered specification: \"{userInput}\").";

            if (innerException == null)
            {
                throw new FormatException(message, innerException);
            }
            else
            {
                throw new FormatException(message);
            }
        }

        private static string CreateRandomOffsetIntervalSpec(int intervalMins)
        {
            // The basic format of the CRON expressions is:
            // {second} {minute} {hour} {day} {month} {day of the week}
            // E.g.
            //     TimerTrigger("15 2/5 * * * *")
            // means every 5 minutes starting at 2, on 15 secs past the minute, i.e., 02:15, 07:15, 12:15, 17:15, ...

            int intervalTotalSecs = intervalMins * 60;
            int rndOffsTotalSecs = Rnd.Next(0, intervalTotalSecs);
            int rndOffsWholeMins = rndOffsTotalSecs / 60;
            int rndOffsSubminSecs = rndOffsTotalSecs % 60;

            string cronSpec = $"{rndOffsSubminSecs} {rndOffsWholeMins}/{intervalMins} * * * *";
            return cronSpec;
        }
    }
}
