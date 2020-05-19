using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AvailabilityMonitoring_Extension_DemoFunction
{
    public static class EnvironmentSpoofer
    {
        //[FunctionName("EnvironmentSpoofer")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var result = new
            {
                environment = GetEnvironment()
            };

            return new OkObjectResult(result);
        }

        private static List<KeyValuePair<string, string>> GetEnvironment()
        {
            var environment = new Dictionary<object, object>();

            IDictionary environmentDict = Environment.GetEnvironmentVariables();
            foreach (DictionaryEntry entry in environmentDict)
            {
                environment.Add(entry.Key, entry.Value);
            }

            List<KeyValuePair<string, string>> orderedEnvironment = environment.Select(kvp => KeyValuePair.Create(kvp.Key.ToString(), kvp.Value.ToString()))
                                                                               .Select( (kvp) => KeyValuePair.Create(kvp.Key,
                                                                                                                     kvp.Key.Contains("Key", StringComparison.OrdinalIgnoreCase)
                                                                                                                            || kvp.Key.Contains("Passw", StringComparison.OrdinalIgnoreCase)
                                                                                                                            || kvp.Key.Contains("Pass", StringComparison.OrdinalIgnoreCase)
                                                                                                                            || kvp.Key.Contains("Pwd", StringComparison.OrdinalIgnoreCase)
                                                                                                                            || kvp.Key.Contains("Sig", StringComparison.OrdinalIgnoreCase)
                                                                                                                            || kvp.Value.Contains("Key", StringComparison.OrdinalIgnoreCase)
                                                                                                                            || kvp.Value.Contains("Passw", StringComparison.OrdinalIgnoreCase)
                                                                                                                            || kvp.Value.Contains("Pass", StringComparison.OrdinalIgnoreCase)
                                                                                                                            || kvp.Value.Contains("Pwd", StringComparison.OrdinalIgnoreCase)
                                                                                                                            || kvp.Value.Contains("Sig", StringComparison.OrdinalIgnoreCase)
                                                                                                                        ? "..."
                                                                                                                        : kvp.Value) )
                                                                               .OrderBy((kvp) => kvp.Key)
                                                                               .ToList();
            return orderedEnvironment;
        }
    }
}
