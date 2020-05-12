using System;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.AvailabilityMonitoring;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring;
using Microsoft.Extensions.Logging;

namespace AvailabilityMonitoringExtensionDemo
{
    public class AvailabilityMonitoringDemo01
    {

        [FunctionName(nameof(AvailabilityMonitoringDemo01) + "-" + nameof(ReturnBool))]
        [return: AvailabilityTestResult]
        public bool ReturnBool(
                        [TimerTrigger(AvailabilityTestInterval.Minute01)] TimerInfo _,
                        ILogger log)
        {
            log.LogInformation($"@@@@@@@@@@@ \"{nameof(AvailabilityMonitoringDemo01)}.{nameof(ReturnBool)}\" started.");

            Task.Delay(TimeSpan.FromMilliseconds(300)).GetAwaiter().GetResult();

            return true;
        }

        [FunctionName(nameof(AvailabilityMonitoringDemo01) + "-" + nameof(ReturnTaskBool))]
        [return: AvailabilityTestResult]
        public async Task<bool> ReturnTaskBool(
                       [TimerTrigger(AvailabilityTestInterval.Minute01)] TimerInfo _,
                       ILogger log)
        {
            log.LogInformation($"########### \"{nameof(AvailabilityMonitoringDemo01)}.{nameof(ReturnTaskBool)}\" started.");

            await Task.Delay(TimeSpan.FromMilliseconds(300));

            return true;
        }

        [FunctionName(nameof(AvailabilityMonitoringDemo01) + "-" + nameof(ReturnAvailabilityTelemetry))]
        [return: AvailabilityTestResult]
        public AvailabilityTelemetry ReturnAvailabilityTelemetry(
                       [TimerTrigger(AvailabilityTestInterval.Minute01)] TimerInfo _,
                       ILogger log)
        {
            log.LogInformation($"$$$$$$$$$$$ \"{nameof(AvailabilityMonitoringDemo01)}.{nameof(ReturnAvailabilityTelemetry)}\" started.");

            Task.Delay(TimeSpan.FromMilliseconds(300)).GetAwaiter().GetResult();

            var availabilityResult = new AvailabilityTelemetry();
            availabilityResult.Success = true;
            availabilityResult.Message = "This is a user-written message indicating that the availability test finished successfully.";

            return availabilityResult;
        }

        [FunctionName(nameof(AvailabilityMonitoringDemo01) + "-" + nameof(ThrowException))]
        [return: AvailabilityTestResult]
        public bool ThrowException(
                       [TimerTrigger(AvailabilityTestInterval.Minute01)] TimerInfo _,
                       ILogger log)
        {
            log.LogInformation($"%%%%%%%%%%% \"{nameof(AvailabilityMonitoringDemo01)}.{nameof(ThrowException)}\" started.");

            Task.Delay(TimeSpan.FromMilliseconds(300)).GetAwaiter().GetResult();

            throw new Exception("This is a hypothetical test exception thrown by the user.");
        }

        [FunctionName(nameof(AvailabilityMonitoringDemo01) + "-" + nameof(Timeout))]
        [return: AvailabilityTestResult]
        public async Task<bool> Timeout(
                       [TimerTrigger(AvailabilityTestInterval.Minute01)] TimerInfo _,
                       ILogger log)
        {
            log.LogInformation($"^^^^^^^^^^^ \"{nameof(AvailabilityMonitoringDemo01)}.{nameof(Timeout)}\" started.");

            Console.WriteLine();
            Console.Write("Waiting to time out");

            DateTimeOffset startTime = DateTimeOffset.Now;

            TimeSpan passed = DateTimeOffset.Now - startTime;
            while (passed < TimeSpan.FromSeconds(120))
            {
                Console.Write($"...{(int)passed.TotalSeconds}");
                await Task.Delay(TimeSpan.FromSeconds(5));
                passed = DateTimeOffset.Now - startTime;
            }

            Console.WriteLine();
            return true;
        }

        [FunctionName(nameof(AvailabilityMonitoringDemo01) + "-" + nameof(ReturnValidString))]
        [return: AvailabilityTestResult]
        public string ReturnValidString(
                        [TimerTrigger(AvailabilityTestInterval.Minute01)] TimerInfo _,
                        ILogger log)
        {
            log.LogInformation($"&&&&&&&&&&& \"{nameof(AvailabilityMonitoringDemo01)}.{nameof(ReturnValidString)}\" started.");

            Task.Delay(TimeSpan.FromMilliseconds(300)).GetAwaiter().GetResult();

            var result = new
            {
                UnusedValue = 42,
                AnotherValue = new int[] { 18, 136, -38 },
                Success = true,
                Message = "This is a user-written message indicating that the availability test finished successfully."
            };

            string resultStr = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            return resultStr;
        }

        [FunctionName(nameof(AvailabilityMonitoringDemo01) + "-" + nameof(ReturnInvalidString))]
        [return: AvailabilityTestResult]
        public string ReturnInvalidString(
                        [TimerTrigger(AvailabilityTestInterval.Minute01)] TimerInfo _,
                        ILogger log)
        {
            log.LogInformation($"*********** \"{nameof(AvailabilityMonitoringDemo01)}.{nameof(ReturnInvalidString)}\" started.");

            Task.Delay(TimeSpan.FromMilliseconds(300)).GetAwaiter().GetResult();
            
            return "I-am-an-invalid-Availability-Test-Result-string";
        }

        [FunctionName(nameof(AvailabilityMonitoringDemo01) + "-" + nameof(TakeAvailabilityTestInfo))]
        [return: AvailabilityTestResult]
        public bool TakeAvailabilityTestInfo(
                        [TimerTrigger(AvailabilityTestInterval.Minute01)] TimerInfo _,
                        [AvailabilityTestInfo] AvailabilityTestInfo testInfo,
                        ILogger log)
        {
            log.LogInformation($"||||||||||| \"{nameof(AvailabilityMonitoringDemo01)}.{nameof(TakeAvailabilityTestInfo)}\" started.");
            log.LogInformation("||||||||||| testInfo = {{TestDisplayName = \"{TestDisplayName}\", LocationDisplayName = \"{LocationDisplayName}\", LocationId = \"{LocationId}\", StartTime = \"{StartTime}\"}}",
                               testInfo.TestDisplayName, testInfo.LocationDisplayName, testInfo.LocationId, testInfo.StartTime);

            Task.Delay(TimeSpan.FromMilliseconds(300)).GetAwaiter().GetResult();
            return true;
        }

        [FunctionName(nameof(AvailabilityMonitoringDemo01) + "-" + nameof(TakeString))]
        [return: AvailabilityTestResult]
        public bool TakeString(
                        [TimerTrigger(AvailabilityTestInterval.Minute01)] TimerInfo _,
                        [AvailabilityTestInfo] string testInfo,
                        ILogger log)
        {
            log.LogInformation($"~~~~~~~~~~~ \"{nameof(AvailabilityMonitoringDemo01)}.{nameof(TakeString)}\" started.");
            log.LogInformation("~~~~~~~~~~~ testInfo = \"{TestInfoString}\"", testInfo ?? "<null>");

            Task.Delay(TimeSpan.FromMilliseconds(300)).GetAwaiter().GetResult();
            return true;
        }


    }
}
