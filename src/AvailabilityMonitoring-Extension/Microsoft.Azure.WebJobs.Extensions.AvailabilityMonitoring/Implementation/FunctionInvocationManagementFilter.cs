using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.AvailabilityMonitoring;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
// Type 'IFunctionInvocationFilter' (and other Filter-related types) is marked as preview/obsolete,
// but the guidance from the Azure Functions team is to use it, so we disable the warning.
#pragma warning disable CS0618
    internal class FunctionInvocationManagementFilter : IFunctionInvocationFilter, IFunctionExceptionFilter
#pragma warning restore CS0618 // Type or member is obsolete (Filter-related types are obsolete, but we want to use them)
    {
        private readonly AvailabilityTestRegistry _availabilityTestRegistry;
        private readonly TelemetryClient _telemetryClient;
        private readonly AvailabilityTestScopeSettingsResolver _availabilityTestScopeSettingsResolver;

        public FunctionInvocationManagementFilter(AvailabilityTestRegistry availabilityTestRegistry, TelemetryClient telemetryClient, IConfiguration configuration, INameResolver nameResolver)
        {
            Validate.NotNull(availabilityTestRegistry, nameof(availabilityTestRegistry));
            Validate.NotNull(telemetryClient, nameof(telemetryClient));

            _availabilityTestRegistry = availabilityTestRegistry;
            _telemetryClient = telemetryClient;
            _availabilityTestScopeSettingsResolver = new AvailabilityTestScopeSettingsResolver(configuration, nameResolver);
        }

// Types 'FunctionExecutingContext' and 'IFunctionFilter' (and other Filter-related types) are marked as preview/obsolete,
// but the guidance from the Azure Functions team is to use it, so we disable the warning.
#pragma warning disable CS0618
        public Task OnExecutingAsync(FunctionExecutingContext executingContext, CancellationToken cancelControl)
#pragma warning restore CS0618
        {
            // A few lines which we need for attaching a debugger during development.
            // @ToDo: Remove before shipping.
            Console.WriteLine($"Filter Entry Point: {nameof(FunctionInvocationManagementFilter)}.{nameof(OnExecutingAsync)}(..).");
            Console.WriteLine($"FunctionInstanceId: {Format.SpellIfNull(executingContext?.FunctionInstanceId)}.");
            Process proc = Process.GetCurrentProcess();
            Console.WriteLine($"Process name: \"{proc.ProcessName}\", Process Id: \"{proc.Id}\".");
            // --

            Validate.NotNull(executingContext, nameof(executingContext));

            // Grab the invocation id and the logger:
            Guid functionInstanceId = executingContext.FunctionInstanceId;
            ILogger log = executingContext.Logger;

            // Check if this is an Availability Test.
            // There are 3 cases:
            //  1) This IS an Availability Test and this is an in-proc/.Net functuion:
            //     This filter runs AFTER the bindings.
            //     The current function was already registered, becasue the attribute binding was already executed.
            //  2) This IS an Availability Test and this is an out-of-proc/non-.Net function:
            //     This filter runs BEFORE the bindings.
            //      a) If this is the first time the filter runs for the current function, TryGetTestConfig(..) will
            //         read the metadata file, extract the config and return True.
            //      b) If this is not the first time, the function is already registered as described in (a).
            //  3) This is NOT an Availability Test:
            //     We will get False here and do nothing.

            bool isAvailabilityTest = _availabilityTestRegistry.Functions.IsAvailabilityTest(executingContext, out string functionName, out IAvailabilityTestConfiguration testConfig);
            if (! isAvailabilityTest)
            {
                if (log != null)
                {
                    using (log.BeginScope(LogMonikers.Scopes.CreateForTestInvocation(functionName)))
                    {
                        log.LogDebug($"Availability Test Pre-Function routine was invoked and determned that this function is NOT an Availability Test:"
                                    + " {{FunctionName=\"{FunctionName}\", FunctionInstanceId=\"{FunctionInstanceId}\"}}",
                                      functionName, functionInstanceId);
                    }
                }

                return Task.CompletedTask;
            }

            // If configured, use a fall-back logger:
            log = AvailabilityTest.Log.CreateFallbackLogIfRequired(log);

            IReadOnlyDictionary<string, object> logScopeInfo = LogMonikers.Scopes.CreateForTestInvocation(functionName);
            using (log.BeginScopeSafe(logScopeInfo))
            {
                log?.LogDebug($"Availability Test Pre-Function routine was invoked:"
                             + " {{FunctionName=\"{FunctionName}\", FunctionInstanceId=\"{FunctionInstanceId}\","
                             + " TestConfiguration={{TestDisplayNameTemplate=\"{TestDisplayNameTemplate}\","
                             + " LocationDisplayNameTemplate=\"{LocationDisplayNameTemplate}\","
                             + " LocationIdTemplate=\"{LocationIdTemplate}\"}} }}",
                              functionName, functionInstanceId, testConfig.TestDisplayName, testConfig.LocationDisplayName, testConfig.LocationId);

                // - In case (1) described above, we have already registered this invocation:
                //   The function parameters have been instantiated, and attached to the invocationState.
                //   However, the parameters are NOT yet initialized, as we did not have a AvailabilityTestScope instance yet.
                //   We will set up an AvailabilityTestScope and attach it to the invocationState.
                //   Then we will initialize the parameters using data from that scope.
                // - In case (2) described above, we have not yet registered the invocation:
                //   A new invocationState will end up being created now. 
                //   We will set up an AvailabilityTestScope and attach it to the invocationState.
                //   Subsequently, when the binings eventually get invoked by the Functions tuntime,
                //   they will instantiate and initialize the parameters using data from that scope.

                // Get the invocation state bag:

                AvailabilityTestInvocationState invocationState = _availabilityTestRegistry.Invocations.GetOrRegister(functionInstanceId, log);

                // If test configuration makes reference to configuration, resolve the settings
                IAvailabilityTestConfiguration resolvedTestConfig = _availabilityTestScopeSettingsResolver.Resolve(testConfig, functionName);

                // Start the availability test scope (this will start timers and set up the activity span):
                AvailabilityTestScope testScope = AvailabilityTest.StartNew(resolvedTestConfig, _telemetryClient, flushOnDispose: true, log, logScopeInfo);
                invocationState.AttachTestScope(testScope);

                // If we have previously instantiated a result collector, initialize it now:
                if (invocationState.TryGetResultCollector(out AvailabilityResultAsyncCollector resultCollector))
                {
                    resultCollector.Initialize(testScope);
                }

                // If we have previously instantiated a test info, initialize it now:
                if (invocationState.TryGetTestInfos(out IEnumerable<AvailabilityTestInfo> testInfos))
                {
                    AvailabilityTestInfo model = testScope.CreateAvailabilityTestInfo();
                    foreach (AvailabilityTestInfo testInfo in testInfos)
                    {
                        testInfo.CopyFrom(model);
                    }
                }
            }

            return Task.CompletedTask;
        }

// Types 'FunctionExceptionContext' and 'IFunctionFilter' (and other Filter-related types) are marked as preview/obsolete,
// but the guidance from the Azure Functions team is to use it, so we disable the warning.
#pragma warning disable CS0618
        public Task OnExceptionAsync(FunctionExceptionContext exceptionContext, CancellationToken cancelControl)
#pragma warning restore CS0618
        {
            // A few lines which we need for attaching a debugger during development.
            // @ToDo: Remove before shipping.
            Console.WriteLine($"Filter Entry Point: {nameof(FunctionInvocationManagementFilter)}.{nameof(OnExceptionAsync)}(..).");
            Console.WriteLine($"FunctionInstanceId: {Format.SpellIfNull(exceptionContext?.FunctionInstanceId)}.");
            Process proc = Process.GetCurrentProcess();
            Console.WriteLine($"Process name: \"{proc.ProcessName}\", Process Id: \"{proc.Id}\".");
            // --

            // Get error:
            Exception error = exceptionContext?.Exception 
                                ?? new Exception("OnExceptionAsync(..) is invoked, but no Exception information is available. ");

            OnPostFunctionError(exceptionContext, error, nameof(OnExceptionAsync));

            return Task.CompletedTask;
        }

// Types 'FunctionExecutedContext' and 'IFunctionFilter' (and other Filter-related types) are marked as preview/obsolete,
// but the guidance from the Azure Functions team is to use it, so we disable the warning.
#pragma warning disable CS0618
        public Task OnExecutedAsync(FunctionExecutedContext executedContext, CancellationToken cancelControl)
#pragma warning restore CS0618
        {
            // A few lines which we need for attaching a debugger during development.
            // @ToDo: Remove before shipping.
            Console.WriteLine($"Filter Entry Point: {nameof(FunctionInvocationManagementFilter)}.{nameof(OnExecutedAsync)}(..).");
            Console.WriteLine($"FunctionInstanceId: {Format.SpellIfNull(executedContext?.FunctionInstanceId)}.");
            Process proc = Process.GetCurrentProcess();
            Console.WriteLine($"Process name: \"{proc.ProcessName}\", Process Id: \"{proc.Id}\".");
            // --

            Exception error = null;
            if (executedContext?.FunctionResult?.Succeeded != true)
            {
                error = executedContext?.FunctionResult?.Exception;
                error = error ?? new Exception("FunctionResult.Succeeded is false, but no Exception information is available.");
            }

            OnPostFunctionError(executedContext, error, nameof(OnExecutedAsync));

            return Task.CompletedTask;
        }

// Types 'FunctionFilterContext' and 'IFunctionFilter' (and other Filter-related types) are marked as preview/obsolete,
// but the guidance from the Azure Functions team is to use it, so we disable the warning.
#pragma warning disable CS0618
        private void OnPostFunctionError(FunctionFilterContext filterContext, Exception error, string entryPointName)
#pragma warning restore CS0618
        {
            // The functions runtime communicates some exceptions only via OnExceptionAsync(..) (e.g., timeouts).
            // Some other exceptions may be also be communicated via OnExecutedAsync(..).
            // Rather than trying to predict this flaky behaviour, we are being defensve and are processing both callbacks.
            // Whichever happens first will call this method. We will deregister the invocation and process the error.
            // The second call (if it happens) will find this invocation no longer registered it will return.
            // If no error occurred at all, the result is handeled by the result collector (IAsyncCollector<>).
            // So, for no-error cases, all we need to do here is to deregister the invocation and return right away.

            Validate.NotNull(filterContext, nameof(filterContext));

            // Grab the invocation id, the logger and the function name:
            Guid functionInstanceId = filterContext.FunctionInstanceId;
            ILogger log = filterContext.Logger;
            string functionName = filterContext.FunctionName;

            // Unwrap generic function exception:
            while (error != null 
                        && error is FunctionInvocationException funcInvocEx 
                        && funcInvocEx.InnerException != null)
            {
                error = funcInvocEx.InnerException;
            }

            // If configured, use a fall-back logger:
            log = AvailabilityTest.Log.CreateFallbackLogIfRequired(log);

            IReadOnlyDictionary<string, object> logScopeInfo = LogMonikers.Scopes.CreateForTestInvocation(functionName);
            using (log?.BeginScopeSafe(logScopeInfo))
            {
                log?.LogDebug($"Availability Test Post-Function error handling routine (via {entryPointName}) beginning:"
                                + " {{FunctionName=\"{FunctionName}\", FunctionInstanceId=\"{FunctionInstanceId}\","
                                + " ErrorType=\"{ErrorType}\"}}",
                                 functionName, functionInstanceId,
                                 Format.SpellIfNull(error?.GetType()?.Name));

                // A function is an Availability Test iff is has a return value marked with [AvailabilityTestResult];
                // whereas a [AvailabilityTestInfo] is optional to get test information at runtime.
                // User could have marked a parameter with [AvailabilityTestInfo] but no return value with [AvailabilityTestResult]:
                // That does not make sense, but we need to do something graceful. Since in the binder (see CreateAvailabilityTestInfo) we
                // did not have a way of knowing whether the return value is tagged, we have initialized the test info and registered the invocation.
                // We need to clean it up now even if the function is not an Availability Test.

                bool isTrackedInvocation  = _availabilityTestRegistry.Invocations.TryDeregister(functionInstanceId, log, out AvailabilityTestInvocationState invocationState);
                if (! isTrackedInvocation)
                {
                    log?.LogDebug($"Availability Test Post-Function error handling routine (via {entryPointName}) finished:"
                                + " This function invocation instance is not being tracked."
                                + " {{FunctionName=\"{FunctionName}\", FunctionInstanceId=\"{FunctionInstanceId}\","
                                + " ErrorType=\"{ErrorType}\"}}",
                                 functionName, functionInstanceId,
                                 Format.SpellIfNull(error?.GetType()?.Name));
                    return;
                }

                // If no exception was thrown by the function, the results collector will be called to set the return value.
                // It will Complete the Availability Test Scope, so there is nothing to do here.

                if (error == null)
                {
                    log?.LogDebug($"Availability Test Post-Function error handling routine (via {entryPointName}) finished:"
                                + " No error to be handled."
                                + " {{FunctionName=\"{FunctionName}\", FunctionInstanceId=\"{FunctionInstanceId}\","
                                + " ErrorType=\"{ErrorType}\"}}",
                                 functionName, functionInstanceId,
                                 Format.SpellIfNull(error?.GetType()?.Name));
                    return;
                }

                // An exception has occurred in the function, so we need to complete the Availability Test Scope here.

                if (! invocationState.TryGetTestScope(out AvailabilityTestScope testScope))
                {
                    // This should never happen!

                    log?.LogError($"Availability Test Post-Function error handling routine (via {entryPointName}) finised:"
                                +  " Error: No AvailabilityTestScope was attached to the invocation state - Cannot continue processing!"
                                +  " {{FunctionName=\"{FunctionName}\", FunctionInstanceId=\"{FunctionInstanceId}\"}}"
                                +  " ErrorType=\"{ErrorType}\", ErrorMessage=\"{ErrorMessage}\"}}",
                                  functionName, functionInstanceId,
                                  error.GetType().Name, error.Message);
                    return;
                }

                bool isTimeout = (error is FunctionTimeoutException);

                testScope.Complete(error, isTimeout);
                testScope.Dispose();

                log?.LogDebug($"Availability Test Post-Function error handling routine (via {entryPointName}) finidhed:"
                            + $" {nameof(AvailabilityTestScope)} was completed and disposed."
                            +  " {{FunctionName=\"{FunctionName}\", FunctionInstanceId=\"{FunctionInstanceId}\","
                            +  " ErrorType=\"{ErrorType}\", ErrorMessage=\"{ErrorMessage}\","
                            +  " TestConfiguration={{TestDisplayName=\"{TestDisplayName}\","
                            +  " LocationDisplayName=\"{LocationDisplayName}\","
                            +  " LocationId=\"{LocationId}\"}} }}",
                             functionName, functionInstanceId,
                             error.GetType().Name, error.Message,
                             testScope.TestDisplayName, testScope.LocationDisplayName, testScope.LocationId);
            }
        }
    }
}
