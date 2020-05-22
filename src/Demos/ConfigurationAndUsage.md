
# Setting up Azure Function

1) Create Azure Function template in VS (VS 2017 or VS 2019) and choose TimerTrigger as an initial template configuration

2) Install 1.0.0-alpha.1 version of Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring nuget to your project
    - Add Application Insights CAT feed from myget as a nuget source:
    https://www.myget.org/F/applicationinsights-cat/api/v3/index.json 
    - Check 'Include prerelease' option
 

3) Use custom function code from the samples that can be found here:
https://github.com/Azure/azure-functions-availability-monitoring-extension/blob/master/src/Demos/AvailabilityMonitoring-Extension-DemoFunction/CatDemoFunctions.cs

4) Deploy your code to Azure Function app and connect Function app to Application Insights 

 Note: your Function app should have Linux selected as Operating System in order to have correct End-to-end tracing correlation, if you'd like to have just AvailabilityTelemetry and set up alerting on it Windows image also would work.


# Coded Availability Tests API

The API below is only for private preview and is subject to change in the future versions.

 
1) Availability test frequency can be specified by setting `[TimerTrigger(AvailabilityTestInterval.Minute01)]` attribute for your timer trigger. It can be set to 1, 5, 10 and 15 minutes

``` csharp
public static async Task<bool> Run(
                            [TimerTrigger(AvailabilityTestInterval.Minute01)] TimerInfo notUsed,
                            ILogger log)
```

Default timer interval in a form of CRON expression also can be set `[TimerTrigger("*/1 * * * * *")] TimerInfo timerInfo` and the difference is that `AvailabilityTestInterval` guarantees distribution of availability tests execution across multiple locations and by using the default one all tests in all locations will be executed at the same time.

`AvailabilityTestInterval` is recommended way of configuring tests deployed to multiple locations.

2) Default availability test name is equal to the function name but also can be customized by setting TestDisplayName on AvailabilityTestResult

``` csharp
[return: AvailabilityTestResult(TestDisplayName = "MyAvailabilityTestName")]

```
 
3) Function can return either boolean that will be converted to availability result success field or full AvailabilityTelemetry object with all needed modifications including enriched custom properties collection:

 ``` csharp
AvailabilityTelemetry result = testInfo.DefaultAvailabilityResult;
result.Properties["ResponseCode"] = <desired response code>;
result.Success = hasExpectedContent;
return result;
 ```