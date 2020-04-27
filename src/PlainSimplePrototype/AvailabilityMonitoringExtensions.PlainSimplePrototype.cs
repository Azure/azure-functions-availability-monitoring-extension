using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

[assembly: WebJobsStartup(typeof(AvailabilityMonitoringExtension.PlainSimplePrototype.AvailabilityMonitoringWebJobsStartup))]

namespace AvailabilityMonitoringExtension.PlainSimplePrototype
{
    internal class AvailabilityMonitoringWebJobsStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddAvailabilityMonitoring();
        }
    }

    public static class AvailabilityMonitoringtWebJobsBuilderExtensions
    {
        public static IWebJobsBuilder AddAvailabilityMonitoring(this IWebJobsBuilder builder)
        {
            builder.AddExtension<AvailabilityMonitoringExtensionConfigProvider>();

            builder.Services.AddSingleton<IFunctionFilter, AvailabilityTestExceptionFilter>();
            builder.Services.AddSingleton<AvailabilityTestManager>();

            return builder;
        }
    }

    [Binding]
    [AttributeUsage(AttributeTargets.ReturnValue)]
    public class AvailabilityTestResultAttribute : Attribute
    {
        public string TestDisplayName { get; set; }
    }

    public class AvailabilityTestResult
    {
        public string TestDisplayName { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
    }

    [Extension("AvailabilityMonitoring.PlainSimplePrototype")]
    internal class AvailabilityMonitoringExtensionConfigProvider : IExtensionConfigProvider
    {
        private readonly AvailabilityTestManager _manager;

        public AvailabilityMonitoringExtensionConfigProvider(AvailabilityTestManager manager)
        {
            _manager = manager;
        }


        public void Initialize(ExtensionConfigContext extensionConfigContext)
        {
            extensionConfigContext.AddConverter<string, AvailabilityTestResult>( (jsonStr) => JsonConvert.DeserializeObject<AvailabilityTestResult>(jsonStr) );

            // FluentBindingRule<ApiAvailabilityTest> is marked as Obsolete, yet it is the type returned from AddBindingRule(..)
            // We could use "var", but one should NEVER use "var" except in Lync expressions
            // or when the type is clear from the *same* line to an unfamiliar reader.
            // Neither is the case, so we use the type explicitly and work around the obsolete-warning.
#pragma warning disable CS0618
            FluentBindingRule<AvailabilityTestResultAttribute> rule = extensionConfigContext.AddBindingRule<AvailabilityTestResultAttribute>();
#pragma warning restore CS0618 

            var bindingProvider = new AvailabilityTestResultAttributeBindingProvider(_manager);
            rule.Bind(bindingProvider);
        }
    }

    internal class AvailabilityTestResultAttributeBindingProvider : IBindingProvider
    {
        private readonly AvailabilityTestManager _availabilityTestManager;

        public AvailabilityTestResultAttributeBindingProvider(AvailabilityTestManager availabilityTestManager)
        {
            _availabilityTestManager = availabilityTestManager;
        }

        public Task<IBinding> TryCreateAsync(BindingProviderContext bindingProviderContext)
        {
            Console.WriteLine();
            Console.WriteLine("Executing AvailabilityTestResultAttributeBindingProvider.TryCreateAsync(..).");

            ParameterInfo parameter = bindingProviderContext.Parameter;

            // Get and validate attribute:
            AvailabilityTestResultAttribute attribute = parameter?.GetCustomAttribute<AvailabilityTestResultAttribute>(inherit: false);
            if (attribute == null)
            {
                throw new InvalidOperationException($"An {nameof(AvailabilityTestResultAttributeBindingProvider)} should only be"
                                                 + $" registered to bind parameters tagged with the {nameof(AvailabilityTestResultAttribute)}."
                                                 + $" However, no such attribute was found.");
            }

            // Get and validate parameter type:

            Type valueType = parameter.ParameterType;
            string parameterName = parameter.Name ?? "null";

            Console.WriteLine($"    valueType: {valueType.FullName}");

            IBinding binding;

            // Are all of these combinations below really possible/necesary?

            // Binding to AvailabilityTestResult:
            if (typeof(AvailabilityTestResult).IsAssignableFrom(valueType)
                    || typeof(AvailabilityTestResult).MakeByRefType().IsAssignableFrom(valueType))
            {
                binding = new AvailabilityTestResultAttributeBinding<AvailabilityTestResult>(useCollector: false, _availabilityTestManager, attribute);
            }
            else if (typeof(IAsyncCollector<AvailabilityTestResult>).IsAssignableFrom(valueType)
                    || typeof(IAsyncCollector<AvailabilityTestResult>).MakeByRefType().IsAssignableFrom(valueType))
            {
                binding = new AvailabilityTestResultAttributeBinding<AvailabilityTestResult>(useCollector: true, _availabilityTestManager, attribute);
            }

            // Binding to String:
            else if (typeof(string).IsAssignableFrom(valueType)
                    || typeof(string).MakeByRefType().IsAssignableFrom(valueType))
            {
                binding = new AvailabilityTestResultAttributeBinding<string>(useCollector: false, _availabilityTestManager, attribute);
            }
            else if (typeof(IAsyncCollector<string>).IsAssignableFrom(valueType)
                    || typeof(IAsyncCollector<string>).MakeByRefType().IsAssignableFrom(valueType))
            {
                binding = new AvailabilityTestResultAttributeBinding<string>(useCollector: true, _availabilityTestManager, attribute);
            }

            // Binding to Boolean:
            else if (typeof(bool).IsAssignableFrom(valueType)
                    || typeof(bool).MakeByRefType().IsAssignableFrom(valueType))
            {
                binding = new AvailabilityTestResultAttributeBinding<bool>(useCollector: false, _availabilityTestManager, attribute);
            }
            else if (typeof(IAsyncCollector<bool>).IsAssignableFrom(valueType)
                    || typeof(IAsyncCollector<bool>).MakeByRefType().IsAssignableFrom(valueType))
            {
                binding = new AvailabilityTestResultAttributeBinding<bool>(useCollector: true, _availabilityTestManager, attribute);
            }

            else
            {
                throw new ArgumentException($"Only values of the following types can be constructed based on the {nameof(AvailabilityTestResultAttribute)}:"
                                          + $" \"{typeof(AvailabilityTestResult).Name}\","
                                          + $" \"{typeof(IAsyncCollector<AvailabilityTestResult>).Name}\","
                                          + $" \"{typeof(AvailabilityTestResult).MakeByRefType().Name}\","
                                          + $" \"{typeof(IAsyncCollector<AvailabilityTestResult>).MakeByRefType().Name}\","
                                          + $" plus any types for wich a Converter to the above types was registered."
                                          + $" However, a value named \"{parameterName}\" of type \"{valueType.FullName}\" was requested.");
            }

            return Task.FromResult<IBinding>(binding);
        }
    }

    internal class AvailabilityTestResultAttributeBinding<T> : IBinding
    {
        private readonly bool _useCollector;
        private readonly AvailabilityTestManager _availabilityTestManager;
        private readonly AvailabilityTestResultAttribute _attribute;

        public AvailabilityTestResultAttributeBinding(bool useCollector, AvailabilityTestManager availabilityTestManager, AvailabilityTestResultAttribute attribute)
        {
            _useCollector = useCollector;
            _availabilityTestManager = availabilityTestManager;
            _attribute = attribute;
        }

        public bool FromAttribute
        {
            get { return true; }
        }

        public Task<IValueProvider> BindAsync(object value, ValueBindingContext valueBindingContext)
        {
            Console.WriteLine();
            Console.WriteLine("Executing AvailabilityTestResultAttributeBinding.BindAsync(Object, ValueBindingContext).");

            // I do not fully understand the purpose of this API. When is it called?
            throw new NotImplementedException();
        }

        public Task<IValueProvider> BindAsync(BindingContext bindingContext)
        {
            Console.WriteLine();
            Console.WriteLine("Executing AvailabilityTestResultAttributeBinding.BindAsync(BindingContext).");

            Guid functionInstanceId = bindingContext.FunctionInstanceId;
            Console.WriteLine($"    functionInstanceId: {functionInstanceId}.");

            _availabilityTestManager.SetupAvailabilityTest(functionInstanceId, _attribute);

            var binder = new AvailabilityTestResultValueBinder<T>(_useCollector, functionInstanceId, _availabilityTestManager);
            return Task.FromResult<IValueProvider>(binder);
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            Console.WriteLine();
            Console.WriteLine("Executing AvailabilityTestResultAttributeBinding.ToParameterDescriptor().");

            // I do not fully understand the purpose of this API. What is a ParameterDescriptor used for?
            return new ParameterDescriptor();
        }
    }

    internal class AvailabilityTestResultValueBinder<T> : IValueBinder
    {
        private static readonly Type ValueType_Direct = typeof(AvailabilityTestResult);
        private static readonly Type ValueType_Collector = typeof(AvailabilityTestResultAsyncCollector<T>);

        private readonly bool _useCollector;
        private readonly Guid _functionInstanceId;
        private readonly AvailabilityTestManager _availabilityTestManager;

        public AvailabilityTestResultValueBinder(bool useCollector, Guid functionInstanceId, AvailabilityTestManager availabilityTestManager)
        {
            _useCollector = useCollector;
            _functionInstanceId = functionInstanceId;
            _availabilityTestManager = availabilityTestManager;
        }

        public Type Type { get { return _useCollector ? ValueType_Collector : ValueType_Direct; } }

        public Task<object> GetValueAsync()
        {
            Console.WriteLine();
            Console.WriteLine($"Executing AvailabilityTestResultValueBinder.GetValueAsync() {{_functionInstanceId={_functionInstanceId}}}.");

            if (! _useCollector)
            {
                throw new InvalidOperationException($"This {nameof(AvailabilityTestResultValueBinder<T>)} has been configured to"
                                                  + $" NOT use {nameof(IAsyncCollector<AvailabilityTestResult>)}."
                                                  + $" It does not expect that {nameof(GetValueAsync)}(..) will be invoked."
                                                  + $" This is because a {nameof(AvailabilityTestResultValueBinder<T>)} that is"
                                                  + $" not using a collector is designed to"
                                                  + $" only work with {nameof(AvailabilityTestResultAttribute)} which"
                                                  + $" can only be applied to return values."
                                                  + $" It is not expected that it will be invoked for initializing new values.");
            }

            IAsyncCollector<T> collector = new AvailabilityTestResultAsyncCollector<T>(_functionInstanceId, _availabilityTestManager);
            return Task.FromResult<object>(collector);
        }

        public Task SetValueAsync(object valueToSet, CancellationToken cancelControl)
        {
            Console.WriteLine();
            Console.WriteLine($"Executing AvailabilityTestResultValueBinder.SetValueAsync() {{_functionInstanceId={_functionInstanceId}}}.");
            Console.WriteLine($"    Specified valueToSet type: {valueToSet?.GetType()?.Name ?? "null"}");
            Console.WriteLine($"    Specified valueToSet:      {valueToSet?.ToString() ?? "null"}");

            if (_useCollector)
            {
                throw new InvalidOperationException($"This {nameof(AvailabilityTestResultValueBinder<T>)} has been configured to"
                                                  + $" use {nameof(IAsyncCollector<AvailabilityTestResult>)}."
                                                  + $" It does not expect that {nameof(SetValueAsync)}(..) will be invoked."
                                                  + $" Instead, {nameof(IAsyncCollector<AvailabilityTestResult>.AddAsync)}(..) should be called.");
            }

            AvailabilityTestResult availabilityTestResult = Convert.ValueToAvailabilityTestResult(valueToSet);
            _availabilityTestManager.CompleteAvailabilityTest(_functionInstanceId, availabilityTestResult);
            return Task.CompletedTask;
        }

        public string ToInvokeString()
        {
            Console.WriteLine();
            Console.WriteLine($"Executing AvailabilityTestResultValueBinder.ToInvokeString() {{_functionInstanceId={_functionInstanceId}}}.");

            // I do not fully understand the purpose of this API. When is it called?
            return String.Empty;
        }
    }

    internal class AvailabilityTestResultAsyncCollector<T> : IAsyncCollector<T>
    {
        private readonly Guid _functionInstanceId;
        private readonly AvailabilityTestManager _availabilityTestManager;

        public AvailabilityTestResultAsyncCollector(Guid functionInstanceId, AvailabilityTestManager availabilityTestManager)
        {
            _functionInstanceId = functionInstanceId;
            _availabilityTestManager = availabilityTestManager;
        }

        public Task AddAsync(T item, CancellationToken cancelControl = default)
        {
            Console.WriteLine();
            Console.WriteLine($"Executing AvailabilityTestResultAsyncCollector.AddAsync() {{_functionInstanceId={_functionInstanceId}}}.");
            Console.WriteLine($"    Specified valueToSet type: {item?.GetType()?.Name ?? "null"}");
            Console.WriteLine($"    Specified valueToSet:      {item?.ToString() ?? "null"}");

            AvailabilityTestResult availabilityTestResult = Convert.ValueToAvailabilityTestResult(item);
            _availabilityTestManager.CompleteAvailabilityTest(_functionInstanceId, availabilityTestResult);
            return Task.CompletedTask;
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            Console.WriteLine();
            Console.WriteLine($"Executing AvailabilityTestResultAsyncCollector.FlushAsync() {{_functionInstanceId={_functionInstanceId}}}.");

            return Task.CompletedTask;
        }
    }

    internal class AvailabilityTestExceptionFilter : IFunctionExceptionFilter
    {
        private readonly AvailabilityTestManager _availabilityTestManager;

        public AvailabilityTestExceptionFilter(AvailabilityTestManager availabilityTestManager)
        {
            _availabilityTestManager = availabilityTestManager;
        }

        public Task OnExceptionAsync(FunctionExceptionContext exceptionContext, CancellationToken cancellationToken)
        {
            Console.WriteLine();
            Console.WriteLine($"Executing AvailabilityTestExceptionFilter.OnExceptionAsync(..).");

            Guid functionInstanceId = exceptionContext.FunctionInstanceId;
            Console.WriteLine($"    functionInstanceId: {functionInstanceId}.");

            _availabilityTestManager.FailAvailabilityTest(functionInstanceId, exceptionContext.Exception);
            return Task.CompletedTask;
        }
    }

    internal class AvailabilityTestManager
    {
        private readonly ConcurrentDictionary<Guid, AvailabilityTestState> _runningTests = new ConcurrentDictionary<Guid, AvailabilityTestState>();

        internal void SetupAvailabilityTest(Guid functionInstanceId, AvailabilityTestResultAttribute attribute)
        {
            Console.WriteLine();
            Console.WriteLine($"Executing AvailabilityTestManager.SetupAvailabilityTest(functionInstanceId: {functionInstanceId}).");

            var testState = new AvailabilityTestState()
            {
                TestDisplayName = String.IsNullOrWhiteSpace(attribute?.TestDisplayName) ? "TestDisplayName not specified" : attribute?.TestDisplayName
            };
            
            if (! _runningTests.TryAdd(functionInstanceId, testState))
            {
                throw new InvalidOperationException($"Availability Test with functionInstanceId = {functionInstanceId} is already running.");
            }

            Console.WriteLine($"    Setting up Activity.");
            Console.WriteLine($"    Starting timers.");
        }

        internal void CompleteAvailabilityTest(Guid functionInstanceId, AvailabilityTestResult availabilityTestResult)
        {
            Console.WriteLine();
            Console.WriteLine($"Executing AvailabilityTestManager.CompleteAvailabilityTest(functionInstanceId: {functionInstanceId}, ..).");

            if (! _runningTests.TryRemove(functionInstanceId, out AvailabilityTestState availabilityTestState))
            {
                throw new InvalidOperationException($"Availability Test with functionInstanceId = {functionInstanceId} was not running.");
            }

            // Users can optionally set some values; we use dynamic defaults if they do not:
            if (String.IsNullOrWhiteSpace(availabilityTestResult.TestDisplayName))
            {
                availabilityTestResult.TestDisplayName = availabilityTestState.TestDisplayName;
            }

            TearDownCompleteAvailabilityTest(availabilityTestResult, availabilityTestState);
        }

        internal void FailAvailabilityTest(Guid functionInstanceId, Exception error)
        {
            Console.WriteLine();
            Console.WriteLine($"Executing AvailabilityTestManager.FailAvailabilityTest(functionInstanceId: {functionInstanceId}, error: {error?.GetType().Name ?? "null"}).");

            if (! _runningTests.TryRemove(functionInstanceId, out AvailabilityTestState availabilityTestState))
            {
                // This filter gets invoked for all function failures, including those that are not Availability Tests. 
                // So if we do not find a record for this functionInstanceId, we assume it is not an Availability Test and do nothing.
                return;
            }

            // Unwrap FunctionInvocationException:
            while (error != null && error is FunctionInvocationException && error.InnerException != null)
            {
                error = error.InnerException;
            }

            var failedResult = new AvailabilityTestResult
            {
                TestDisplayName = availabilityTestState.TestDisplayName,
                Message = (error == null) ? "Availability Test completed with an enknown error." : $"Availability Test completed with an error ({error.GetType().Name}): {error.Message}",
                Success = false
            };

            TearDownCompleteAvailabilityTest(failedResult, availabilityTestState);
        }

        private void TearDownCompleteAvailabilityTest(AvailabilityTestResult availabilityTestResult, AvailabilityTestState availabilityTestState)
        {
            Console.WriteLine();
            Console.WriteLine($"Executing AvailabilityTestManager.TearDownCompleteAvailabilityTest(..).");

            Console.WriteLine($"    Stopping timers.");
            Console.WriteLine($"    Stopping up Activity.");
            Console.WriteLine($"    Sending result to endpoint.");

            string jsonStr = JsonConvert.SerializeObject(availabilityTestResult, Formatting.Indented);
            Console.WriteLine(jsonStr);
        }
    }

    internal class AvailabilityTestState
    {
        public string TestDisplayName { get; set; }
    }

    internal static class Convert
    {
        public static AvailabilityTestResult ValueToAvailabilityTestResult<T>(T value)
        {
            AvailabilityTestResult availabilityTestResult;

            if (value == null)
            {
                availabilityTestResult = new AvailabilityTestResult
                {
                    Success = false,
                    Message = "Availability Test completed with an unknown outcome"
                };
            }
            else if (value is AvailabilityTestResult tstRes)
            {
                availabilityTestResult = tstRes;
            }
            else if (value is string tstResStr)
            {
                availabilityTestResult = JsonConvert.DeserializeObject<AvailabilityTestResult>(tstResStr);
            }
            else if (value is bool tstResBool)
            {
                availabilityTestResult = new AvailabilityTestResult
                {
                    Success = tstResBool,
                    Message = tstResBool ? "Availability Test completed with success" : "Availability Test completed with a failure"
                };
            }
            else
            {
                throw new InvalidOperationException($"Cannot convert a value of type {value.GetType().Name} to {nameof(AvailabilityTestResult)}.");
            }

            return availabilityTestResult;
        }
    }
}
