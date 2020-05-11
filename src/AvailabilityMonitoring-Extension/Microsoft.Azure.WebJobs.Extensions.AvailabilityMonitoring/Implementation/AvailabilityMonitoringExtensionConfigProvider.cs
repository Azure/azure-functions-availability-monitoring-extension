using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.AvailabilityMonitoring;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    [Extension("AvailabilityMonitoring")]
    internal class AvailabilityMonitoringExtensionConfigProvider : IExtensionConfigProvider
    {
        
        private readonly ILogger _log;
        private readonly AvailabilityTestRegistry _availabilityTestRegistry;

        public AvailabilityMonitoringExtensionConfigProvider(AvailabilityTestRegistry availabilityTestRegistry, ILoggerFactory loggerFactory)
        {
            Validate.NotNull(availabilityTestRegistry, nameof(availabilityTestRegistry));
            Validate.NotNull(loggerFactory, nameof(loggerFactory));

            _availabilityTestRegistry = availabilityTestRegistry;
            _log = loggerFactory.CreateLogger(LogMonikers.Categories.Extension);
        }

        public void Initialize(ExtensionConfigContext extensionConfigContext)
        {
            _log?.LogInformation("Availability Monitoring Extension is initializing:"
                               + " {{Version=\"{Version}\"}}",
                                 this.GetType().Assembly.GetName().Version);

            Validate.NotNull(extensionConfigContext, nameof(extensionConfigContext));

            // A Coded Availablity Test is defined as such by returning a value that is bound by AvailabilityTestResult-Attribute.
            // A paramater bound by AvailabilityTestInfo-Attribute is optional.
            // Such parameter can be used to programmatically get information about the current availablity test, or it can be omitted.

            // FluentBindingRule<T> is marked as Obsolete, yet it is the type returned from AddBindingRule(..)
            // We could use "var", but one should NEVER use "var" except in Lync expressions
            // or when the type is clear from the *same* line to an unfamiliar reader.
            // Neither is the case, so we use the type explicitly and work around the obsolete-warning by disabling it.
#pragma warning disable CS0618
            FluentBindingRule<AvailabilityTestResultAttribute> testResultRule = extensionConfigContext.AddBindingRule<AvailabilityTestResultAttribute>();
            FluentBindingRule<AvailabilityTestInfoAttribute> testInfoRule = extensionConfigContext.AddBindingRule<AvailabilityTestInfoAttribute>();
#pragma warning restore CS0618

            // This binding is used to get and process the return value of the function:
            testResultRule.BindToCollector<AvailabilityTestResultAttribute, AvailabilityTelemetry>(CreateAvailabilityTelemetryAsyncCollector);
            testResultRule.BindToCollector<AvailabilityTestResultAttribute, bool>(CreateBoolAsyncCollector);
            extensionConfigContext.AddConverter<string, AvailabilityTelemetry>(Convert.StringToAvailabilityTelemetry);

            // This is an optional In-parameter that allows user code to get runtime info about the availablity test:
            testInfoRule.BindToInput<AvailabilityTestInfo>(CreateAvailabilityTestInfo);
            extensionConfigContext.AddConverter<AvailabilityTestInfo, string>(Convert.AvailabilityTestInfoToString);
        }

        private Task<IAsyncCollector<AvailabilityTelemetry>> CreateAvailabilityTelemetryAsyncCollector(AvailabilityTestResultAttribute attribute, ValueBindingContext valueBindingContext)
        {
            AvailabilityResultAsyncCollector resultCollector = CreateAvailabilityResultAsyncCollector(attribute, valueBindingContext);
            return Task.FromResult<IAsyncCollector<AvailabilityTelemetry>>(resultCollector);
        }

        private Task<IAsyncCollector<bool>> CreateBoolAsyncCollector(AvailabilityTestResultAttribute attribute, ValueBindingContext valueBindingContext)
        {
            AvailabilityResultAsyncCollector resultCollector = CreateAvailabilityResultAsyncCollector(attribute, valueBindingContext);
            return Task.FromResult<IAsyncCollector<bool>>(resultCollector);
        }

        private AvailabilityResultAsyncCollector CreateAvailabilityResultAsyncCollector(AvailabilityTestResultAttribute attribute, ValueBindingContext valueBindingContext)
        {
            // A function is defined as an Availability Test iff is has a return value marked with [AvailabilityTestResult].
            // If that is the case, this method will be invoked as some point to construct a collector for the return value.
            // Depending on the kind of the function, this will happen in different ways:
            //
            //  - For .Net functions (in-proc), this method runs BEFORE function filters:
            //     a) We will register this function as an Availability Test in the Functions registry (this is a NoOp for all,
            //        except the very first invocation).
            //     b) We will create new invocation state bag and register it with the Invocations registry.
            //     c) We will instantiate a result collector and attach it to the state bag.
            //     d) Later on, BEFORE the function body runs, the runtime execute the pre-function filter. At that point we will: 
            //         ~ Initialize an Availablity Test Scope and attach it to the invocation state bag;
            //         ~ Link the results collector and the test scope.
            //     e) Subsequently, AFTER the function body runs the result will be set in one of two ways:
            //         ~ If no error:        the runtime will add the return value to the result collector -> the collector will Complete the Test Scope;
            //         ~ If error/exception: the runtime will invoke the post-function filter -> the filter will Complete the Test Scope.
            //
            //  - For non-.Net functions (out-of-proc), this method runs AFTER function filters (and, potentially, even AFTER the function body has completed):
            //     a) Registering this function as an Availability Test in the Functions registry will be a NoOp.
            //     b) We will receive an existing invocation state bag; the Availablity Test Scope will be already set in the state bag.
            //     c&d) We will instantiate a result collector and link it with the test scope right away; we will attach the collector to the state bag.
            //     e) The results will be set in a simillar manner as for .Net described above.

            Validate.NotNull(attribute, nameof(attribute));
            Validate.NotNull(valueBindingContext, nameof(valueBindingContext));
            
            string functionName = valueBindingContext.FunctionContext.MethodName;

            using (_log.BeginScope(LogMonikers.Scopes.CreateForTestInvocation(functionName)))
            {
                // Register this Function as an Availability Test (NoOp for all invocations of this method, except the very first one):
                _availabilityTestRegistry.Functions.Register(functionName, attribute, _log);

                // Register this particular invocation of this function:
                Guid functionInstanceId = valueBindingContext.FunctionInstanceId;
                AvailabilityTestInvocationState invocationState = _availabilityTestRegistry.Invocations.GetOrRegister(functionInstanceId, _log);

                // Create the result collector:
                var resultCollector = new AvailabilityResultAsyncCollector();

                // If the test scope is already set (out-of-proc function), then link it with the collector:
                bool isTestScopeInitialized = invocationState.TryGetTestScope(out AvailabilityTestScope testScope);
                if (isTestScopeInitialized)
                {
                    resultCollector.Initialize(testScope);
                }
                  
                // Attache the collector to the invocation state bag:
                invocationState.AttachResultCollector(resultCollector);

                // Done:
                return resultCollector;
            }
        }

        private Task<AvailabilityTestInfo> CreateAvailabilityTestInfo(AvailabilityTestInfoAttribute attribute, ValueBindingContext valueBindingContext)
        {
            // A function is an Availability Test iff is has a return value marked with [AvailabilityTestResult];
            // whereas a [AvailabilityTestInfo] is OPTIONAL to get test information at runtime.
            // User could have marked a parameter with [AvailabilityTestInfo] but no return value with [AvailabilityTestResult]:
            // That does not make sense, but we need to do something graceful. 
            // There is no telling what will run first: this method, or CreateAvailabilityTelemetryAsyncCollector(..) above.
            // From here we cannot call _availabilityTestRegistry.Functions.Register(..), becasue the attribute type we get
            // here does not contain any configuration.
            // We will attach a raw test info object to this invocation. 
            // If a test-RESULT-attribute is attached to this function later, it will supply configuration eventually.
            // If not, the test info will remain raw and we must remember to clear the invocation from the registry in the post-function filter.

            Validate.NotNull(attribute, nameof(attribute));
            Validate.NotNull(valueBindingContext, nameof(valueBindingContext));

            string functionName = valueBindingContext.FunctionContext.MethodName;

            using (_log.BeginScope(LogMonikers.Scopes.CreateForTestInvocation(functionName)))
            {
                // Register this particular invocation of this function:
                Guid functionInstanceId = valueBindingContext.FunctionInstanceId;
                AvailabilityTestInvocationState invocationState = _availabilityTestRegistry.Invocations.GetOrRegister(functionInstanceId, _log);

                // Create the test info:
                var testInfo = new AvailabilityTestInfo();

                // If the test scope is already set (out-of-proc function), then use it to initialize the test info:
                bool isTestScopeInitialized = invocationState.TryGetTestScope(out AvailabilityTestScope testScope);
                if (isTestScopeInitialized)
                {
                    testInfo.CopyFrom(testScope.CreateAvailabilityTestInfo());
                }

                // Attach the test info to the invocation state bag:
                invocationState.AttachTestInfo(testInfo);

                // Done:
                return Task.FromResult(testInfo);
            }
        }
    }
}
