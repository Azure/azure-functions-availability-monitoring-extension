using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.AvailabilityMonitoring;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    /// <summary>
    ///This binding provider exists soley to inspect functions in order to determine whether they are an Availability Tests.
    ///
    /// It uses BindingProviderContext, which is not available when binding via BindToCollector(..) as
    /// we do for the actual return value below.
    ///
    /// Attention!:
    /// This works only for in-proc, .Net-based Functions.
    /// For out-of-proc runtimes this approach does not work, there, return-value bindings
    /// may be evaluated after the function user code completes. 
    /// For out-of-proc runtimes, the AvailabilityTestInvocationManager will read the
    /// function metadata to determine whether the function is an Availability Test.
    /// </summary>
    internal class AvailabilityTestDiscoveryBindingProvider : IBindingProvider
    {
        private readonly AvailabilityTestRegistry _availabilityTestRegistry;
        private readonly ILogger _log;

        public AvailabilityTestDiscoveryBindingProvider(AvailabilityTestRegistry availabilityTestRegistry, ILogger log) 
        {
            Validate.NotNull(availabilityTestRegistry, nameof(availabilityTestRegistry));

            _availabilityTestRegistry = availabilityTestRegistry;
            _log = log;
        }

        public Task<IBinding> TryCreateAsync(BindingProviderContext bindingProviderContext)
        {
            ParameterInfo parameter = bindingProviderContext?.Parameter;
            AvailabilityTestResultAttribute attribute = parameter?.GetCustomAttribute<AvailabilityTestResultAttribute>(inherit: false);


            if (attribute != null &&parameter.Member is MethodInfo methodInfo)
            { 
                string functionName = GetFunctionName(methodInfo);
                _availabilityTestRegistry.Functions.Register(functionName, attribute, _log);
            }

            return Task.FromResult<IBinding>(null);
        }

        private static string GetFunctionName(MethodInfo methodInfo)
        {
            // The Function name returned by this method MUST be the same as passed to invocation filters!
            // This is the same code as used by the WebJobs SDK for this.

            FunctionNameAttribute functionNameAttribute = methodInfo.GetCustomAttribute<FunctionNameAttribute>();
            return (functionNameAttribute != null) ? functionNameAttribute.Name : $"{methodInfo.DeclaringType.Name}.{methodInfo.Name}";
        }
    }
}
