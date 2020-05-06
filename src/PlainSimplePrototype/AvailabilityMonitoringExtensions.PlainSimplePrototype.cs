// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Script.WebHost
{
    public static class AvailabilityTestWebJobsBuilderExtensions
    {
        public static IWebJobsBuilder AddAvailabilityTests(this IWebJobsBuilder builder)
        {
            builder.AddExtension<AvailabilityTestExtensionConfigProvider>();

            builder.Services.AddSingleton<IFunctionFilter, AvailabilityTestInvocationFilter>();
            builder.Services.AddSingleton<AvailabilityTestManager>();

            return builder;
        }
    }

    [Extension("AvailabilityTest")]
    internal class AvailabilityTestExtensionConfigProvider : IExtensionConfigProvider
    {
        private readonly AvailabilityTestManager _manager;

        public AvailabilityTestExtensionConfigProvider(AvailabilityTestManager manager)
        {
            _manager = manager;
        }

        public void Initialize(ExtensionConfigContext context)
        {
            // this binding provider exists soley to allow us to inspect functions to determine
            // whether they're availability tests
            // the fluent APIs below don't allow us to do this easily
            context.AddBindingRule<AvailabilityTestContextAttribute>().Bind(new AvailabilityTestDiscoveryBindingProvider(_manager));

            var inputRule = context.AddBindingRule<AvailabilityTestContextAttribute>()
                .BindToInput<AvailabilityTestContext>((AvailabilityTestContextAttribute attr, ValueBindingContext valueContext) =>
                {
                    // new up the context to be passed to user code
                    var testContext = new AvailabilityTestContext
                    {
                        TestDisplayName = "My Test",
                        StartTime = DateTime.UtcNow
                    };
                    return Task.FromResult<AvailabilityTestContext>(testContext);
                });

            var outputRule = context.AddBindingRule<AvailabilityTestResultAttribute>();
            outputRule.BindToCollector<AvailabilityTestResultAttribute, AvailabilityTestResult>((AvailabilityTestResultAttribute attr, ValueBindingContext valueContext) =>
            {
                var collector = new AvailabilityTestCollector(_manager, valueContext.FunctionInstanceId);
                return Task.FromResult<IAsyncCollector<AvailabilityTestResult>>(collector);
            });

            context.AddConverter<AvailabilityTestContext, string>(ctxt =>
            {
                return JsonConvert.SerializeObject(ctxt);
            });

            context.AddConverter<string, AvailabilityTestResult>(json =>
            {
                return JsonConvert.DeserializeObject<AvailabilityTestResult>(json);
            });
        }
    }

    internal class AvailabilityTestManager
    {
        private readonly ConcurrentDictionary<string, bool> _availabilityTests = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<Guid, AvailabilityTestInvocationContext> _availabilityTestInvocationsContextMap = new ConcurrentDictionary<Guid, AvailabilityTestInvocationContext>();

        public AvailabilityTestManager()
        {
        }

        public bool IsAvailabilityTest(string functionName)
        {
            if (_availabilityTests.TryGetValue(functionName, out bool value))
            {
                return value;
            }

            return false;
        }

        public bool IsAvailabilityTest(FunctionInvocationContext context)
        {
            if (IsAvailabilityTest(context.FunctionName))
            {
                return true;
            }

            // For .NET languages, binding happens BEFORE filters, so if the function is an availability test
            // we'll have known that already above
            // The following check handles OOP languages, where bindings happen late and dynamically, AFTER filters.
            // In these cases, we must read the function metadata, since NO binding has yet occurred
            if (context.Arguments.TryGetValue("_context", out object value))
            {
                var executionContext = value as ExecutionContext;
                if (executionContext != null)
                {
                    var metadataPath = Path.Combine(executionContext.FunctionAppDirectory, "function.json");
                    bool isAvailabilityTest = HasAvailabilityTestBinding(metadataPath);
                    _availabilityTests[context.FunctionName] = isAvailabilityTest;
                    return isAvailabilityTest;
                }
            }

            return false;
        }

        public void RegisterTest(string functionName)
        {
            _availabilityTests[functionName] = true;
        }

        public Task<AvailabilityTestInvocationContext> StartInvocationAsync(FunctionInvocationContext context)
        {
            var invocationContext = _availabilityTestInvocationsContextMap.GetOrAdd(context.FunctionInstanceId, n =>
            {
                return new AvailabilityTestInvocationContext
                {
                    InvocationId = context.FunctionInstanceId
                };
            });
            return Task.FromResult(invocationContext);
        }

        public AvailabilityTestInvocationContext GetInvocation(Guid instanceId)
        {
            if (_availabilityTestInvocationsContextMap.TryGetValue(instanceId, out AvailabilityTestInvocationContext invocationContext))
            {
                return invocationContext;
            }
            return null;
        }

        public Task CompleteInvocationAsync(FunctionInvocationContext context)
        {
            if (_availabilityTestInvocationsContextMap.TryRemove(context.FunctionInstanceId, out AvailabilityTestInvocationContext invocationContext))
            {
                // TODO: complete the invocation
            }
            return Task.CompletedTask;
        }

        private bool HasAvailabilityTestBinding(string metadataPath)
        {
            try
            {
                // TODO: make this code robust
                string content = File.ReadAllText(metadataPath);
                JObject jo = JObject.Parse(content);
                var bindings = (JArray)jo["bindings"];
                bool isAvailabilityTest = bindings.Any(p =>
                    string.Compare((string)p["type"], "availabilityTestContext", StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare((string)p["type"], "availabilityTestResult", StringComparison.OrdinalIgnoreCase) == 0);
                return isAvailabilityTest;
            }
            catch
            {
                // best effort
                return false;
            }
        }
    }

    internal class AvailabilityTestInvocationContext
    {
        public Guid InvocationId { get; set; }

        public AvailabilityTestResult Result { get; set; }
    }

    internal class AvailabilityTestDiscoveryBindingProvider : IBindingProvider
    {
        private readonly AvailabilityTestManager _manager;

        public AvailabilityTestDiscoveryBindingProvider(AvailabilityTestManager manager)
        {
            _manager = manager;
        }

        public Task<IBinding> TryCreateAsync(BindingProviderContext context)
        {
            // Note that this only works for .NET functions, not OOP languages, because those bindings
            // are evaluated late-bound, dynamically, and the triggering function is an IL genned wrapper
            // without any AvailabilityTest attributes at all!
            // OOP languages are handled internally by the AvailabilityTestManager by reading function metadata
            var availabilityTestAttribute = context.Parameter.GetCustomAttributes(false).OfType<AvailabilityTestResultAttribute>().SingleOrDefault();
            if (availabilityTestAttribute != null)
            {
                string functionName = GetFunctionName((MethodInfo)context.Parameter.Member);
                _manager.RegisterTest(functionName);
            }

            return Task.FromResult<IBinding>(null);
        }

        private static string GetFunctionName(MethodInfo methodInfo)
        {
            // the format returned here must match the same format passed to invocation filters
            // this is the same code used by the SDK for this
            var functionNameAttribute = methodInfo.GetCustomAttribute<FunctionNameAttribute>();
            return (functionNameAttribute != null) ? functionNameAttribute.Name : $"{methodInfo.DeclaringType.Name}.{methodInfo.Name}";
        }
    }

    // Collector used to receive test results. IAsyncCollector is the type that allows interop with
    // other languages. In .NET, user can just return an AvailabilityTestResult directly from their function -
    // the framework should pass this to your collector
    internal class AvailabilityTestCollector : IAsyncCollector<AvailabilityTestResult>
    {
        private readonly Guid _instanceId;
        private readonly AvailabilityTestManager _manager;

        public AvailabilityTestCollector(AvailabilityTestManager manager, Guid instanceId)
        {
            _manager = manager;
            _instanceId = instanceId;
        }

        public Task AddAsync(AvailabilityTestResult item, CancellationToken cancellationToken = default)
        {
            var invocation = _manager.GetInvocation(_instanceId);
            invocation.Result = item;

            return Task.CompletedTask;
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    // Attribute applied to input parameter
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class AvailabilityTestContextAttribute : Attribute
    {
        // TODO: Add any configuration properties the user needs to set
        // Mark with [AutResolve] to have them resolved from app settings, etc. automatically
    }

    // Attribute applied to return value, representing the test result
    [Binding]
    [AttributeUsage(AttributeTargets.ReturnValue)]
    public class AvailabilityTestResultAttribute : Attribute
    {
    }

    // Input context type passed to user function
    public class AvailabilityTestContext
    {
        public string TestDisplayName { get; set; }

        public string TestArmResourceName { get; set; }

        public string LocationDisplayName { get; set; }

        public string LocationId { get; set; }

        public DateTimeOffset StartTime { get; set; }
    }

    // Result type the user returns from their function
    public class AvailabilityTestResult
    {
        public string Result { get; set; }
    }

    internal class AvailabilityTestInvocationFilter : IFunctionInvocationFilter
    {
        private readonly AvailabilityTestManager _manager;

        public AvailabilityTestInvocationFilter(AvailabilityTestManager manager)
        {
            _manager = manager;
        }

        public async Task OnExecutedAsync(FunctionExecutedContext executedContext, CancellationToken cancellationToken)
        {
            if (_manager.IsAvailabilityTest(executedContext))
            {
                await _manager.CompleteInvocationAsync(executedContext);
            }
        }

        public async Task OnExecutingAsync(FunctionExecutingContext executingContext, CancellationToken cancellationToken)
        {
            if (_manager.IsAvailabilityTest(executingContext))
            {
                await _manager.StartInvocationAsync(executingContext);
            }
        }
    }

    public static class RuleExtensions
    {
        public static void BindToCollector<TAttribute, TMessage>(this FluentBindingRule<TAttribute> rule, Func<TAttribute, ValueBindingContext, Task<IAsyncCollector<TMessage>>> buildFromAttribute) where TAttribute : Attribute
        {
            // TEMP: temporary workaround code effectively adding a ValueBindingContext collector overload,
            // until it's added to the SDK
            Type patternMatcherType = typeof(FluentBindingRule<>).Assembly.GetType("Microsoft.Azure.WebJobs.Host.Bindings.PatternMatcher");
            var patternMatcherNewMethodInfo = patternMatcherType.GetMethods()[4];  // TODO: get this method properly via reflection
            patternMatcherNewMethodInfo = patternMatcherNewMethodInfo.MakeGenericMethod(new Type[] { typeof(TAttribute), typeof(IAsyncCollector<TMessage>) });
            var patternMatcherInstance = patternMatcherNewMethodInfo.Invoke(null, new object[] { buildFromAttribute });

            MethodInfo bindToCollectorMethod = typeof(FluentBindingRule<TAttribute>).GetMethod(
                                                                "BindToCollector",
                                                                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                                                                binder: null,
                                                                new Type[] { patternMatcherType },
                                                                modifiers: null);
            bindToCollectorMethod = bindToCollectorMethod.MakeGenericMethod(new Type[] { typeof(TMessage) });
            bindToCollectorMethod.Invoke(rule, new object[] { patternMatcherInstance });
        }
    }
}
