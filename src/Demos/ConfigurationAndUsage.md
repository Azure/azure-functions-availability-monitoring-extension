# Setting up Azure Function

1. [C#](#Azure%20Function%20in%20C#)
2. [JavaScript](#Azure%20Function%20in%20JavaScript)
3. [Browser testing](#Azure%20Function%20for%20browser%20testing)


# Azure Function in C#

1) Create Azure Function template in VS (VS 2017 or VS 2019) and choose TimerTrigger as an initial template configuration

2) Install 1.0.0-alpha.3 version of Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring nuget to your project
    - Add Application Insights CAT feed from myget as a nuget source:
    https://www.myget.org/F/applicationinsights-cat/api/v3/index.json 
    - Check 'Include prerelease' option
 

3) Use custom function code from the samples that can be found here:
https://github.com/Azure/azure-functions-availability-monitoring-extension/blob/master/src/Demos/AvailabilityMonitoring-Extension-DemoFunction/CatDemoFunctions.cs

4) Deploy your code to Azure Function app and connect Function app to Application Insights 

 Note: your Function app should have Linux selected as Operating System in order to have correct End-to-end tracing correlation, if you'd like to have just AvailabilityTelemetry and set up alerting on it Windows image also would work.


<br>

### Coded Availability Tests API

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


<br><br>

# Azure Function in JavaScript

You can reference already created samples or create your new one from the scratch. Also you can skip 1st step below if you already have Timer Trigger function written in JavaScript that you want to onboard to coded availability tests.

### Samples

1) [https usage + sync function with output bindings](https://github.com/Azure/azure-functions-availability-monitoring-extension/tree/master/src/Demos/JavaScript-Monitoring-Samples/JavaScript-OutputBindings)
2) [axios usage + async function with return statement](https://github.com/Azure/azure-functions-availability-monitoring-extension/tree/master/src/Demos/JavaScript-Monitoring-Samples/JavaScript-ReturnSample)

**NOTE**: Usage is not limited to https and axios, they're used here just for illustration.

### Create your own Function

1) Create Azure function in JavaScript languge with Timer Trigger template in VSCode or any other preferrable way

Full documentation can be found [here](https://docs.microsoft.com/azure/azure-functions/functions-create-first-azure-function-azure-cli?tabs=bash%2Cbrowser&pivots=programming-language-javascript).

``` powershell
func init --javascript
func new --name JSSample --template "Timer trigger"
```


2) Remove extensionBundle section from host.json 

**NOTE**: This step is required, otherwise availability telemetry will not be generated at all
This section should be removed:

``` json
 "extensionBundle": {
    "id": "Microsoft.Azure.Functions.ExtensionBundle",
    "version": "[1.*, 2.0.0)"
  }
```

Resulted file should look like [this](https://github.com/Azure/azure-functions-availability-monitoring-extension/tree/master/src/Demos/JavaScript-Monitoring-Samples/host.json)

3) Install 1.0.0-alpha.3 version of Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring nuget to your project - add custom nuget source pointing to myget and install actual nuget:

``` powershell
dotnet nuget add source https://www.myget.org/F/applicationinsights-cat/api/v3/index.json -n myget-appinsights
func extensions install -p Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring -v 1.0.0-alpha.3
```

5) Define output binding in function.json of availabilityTestResult type

``` json
{
  "bindings": [
    {
      "name": "myTimer",
      "type": "timerTrigger",
      "direction": "in",
      "schedule": "0 */1 * * * *",
      "runOnStartup": true
    },
    {
      "type": "availabilityTestResult",
      "direction": "out",
      "name": "availabilityTelemetry"
    }
  ]
}
```

Alternatively you can use return statement:

``` json
{
  "bindings": [
    {
      "name": "myTimer",
      "type": "timerTrigger",
      "direction": "in",
      "schedule": "0 */1 * * * *",
      "runOnStartup": true
    },
    {
      "type": "availabilityTestResult",
      "direction": "out",
      "name": "$return"
    }
  ]
}
```


Optionally you can specify test name in the output section, otherwise function name will be used:

``` json
{
   ...
    "direction": "out",
    "testDisplayName": "My Availability Test"
}
```


5) Initialize correct bindings before completing the function execution:

``` javascript
context.bindings.availabilityTelemetry = {
            success: success,
            message: message
        };
context.done();
```

In case of async function and return statement it'll look like this:

``` javascript
return {
            success: success,
            message: message
        };
```

6) Publish you function using Azure Function Core Tools:

**NOTE**: Do not use extension for VS Code for publishing as it messes up with host.json and brings back removed section.

``` powershell
func azure functionapp publish <azureFunctionName>
```

<br>

Optinally you can also enable AppInsights SDK for Node.JS in order to collect generated outgoing dependency calls from your Function.
Full documentation for Node.JS SDK can be found [here](https://github.com/microsoft/ApplicationInsights-node.js/blob/develop/README.md).

1) Enable Application Insights SDK for Node.JS:

``` powershell
npm install applicationinsights --save
```

2) Setup SDK in the Function code:

``` javascript
const appInsights = require('applicationinsights');
appInsights.setup().start();
```

<br>

# Azure Function for browser testing

You can also repeat the steps from the sections above for any other technologies like browser testing - for instance for Playwright or Selenium.

**NOTE**: Headless browser support for Chromium was recently added to the Azure Function consumption plan in Linux (not supported in Windows consumption plan) and you can either use it with some customizations (see docs below) or build custom Docker image that includes chromium or other browser of your choice and deploy it to the Premium plan.

1. Playwright

- Some generic samples like authentication can be found [here](https://github.com/microsoft/playwright/blob/master/docs/examples/README.md).
- Sample project that illustrates how to integrate playwright with Azure Function can be found [here](https://github.com/arjun27/playwright-azure-functions).

Consumption plan:
- Use this [documentation](https://dev.to/azure/running-headless-chromium-in-azure-functions-with-puppeteer-and-playwright-2fgk) and a [sample](https://github.com/anthonychu/functions-headless-chromium) to deploy Playwright to the Linux consumption plan.

Docker image:
- Custom Docker image documentation with already included browsers can be found [here](https://github.com/microsoft/playwright/tree/master/docs/docker).

2. Selenium

- JavaScript documentation can be found [here](https://www.selenium.dev/selenium/docs/api/javascript/).
