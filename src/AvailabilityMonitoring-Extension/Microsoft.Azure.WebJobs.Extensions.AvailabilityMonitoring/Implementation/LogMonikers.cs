using Microsoft.Azure.AvailabilityMonitoring;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    internal static class LogMonikers
    {
        public static class Categories
        {
            public const string Extension = "Host.Extensions.AvailabilityMonitoring";

            public static string CreateForTestInvocation(string functionName)
            {
                string category = $"Function.AvailabilityTest.{Format.SpellIfNull(functionName)}";
                return category;
            }
        }

        public static class Scopes
        {
            public static IReadOnlyDictionary<string, object> CreateForTestInvocation(string functionName)
            {
                var scope = new Dictionary<string, object>(capacity: 2)
                {
                    [LogConstants.CategoryNameKey] = Categories.CreateForTestInvocation(functionName),
                    [LogConstants.LogLevelKey] = LogLevel.Information
                };

                return scope;
            }
        }
    }
}
