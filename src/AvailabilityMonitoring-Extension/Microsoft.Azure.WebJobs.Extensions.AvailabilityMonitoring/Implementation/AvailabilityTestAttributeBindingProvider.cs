using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    internal class AvailabilityTestAttributeBindingProvider : IBindingProvider
    {
        private readonly AvailabilityMonitoringExtensionConfigProvider _hack;

        public AvailabilityTestAttributeBindingProvider(AvailabilityMonitoringExtensionConfigProvider hack)
        {
            _hack = hack;
        }

        //private Task<IBinding> TryCreateForResultAsync(BindingProviderContext bindingProviderContext)
        //{
        //    Console.WriteLine("Executing TryCreateForResultAsync(..) for result AvailabilityTestAttribute.");

        //    ParameterInfo parameter = bindingProviderContext.Parameter;
        //    AvailabilityTestResultAttribute resultAttribute = parameter?.GetCustomAttribute<AvailabilityTestResultAttribute>(inherit: false);
        //    Type valueType = parameter.ParameterType;
        //    bool valueTypeIsByRef = valueType.IsByRef;
        //    string parameterName = Format.NotNullOrWord(parameter.Name);

        //    Func<ValueBindingContext, Task<object>> parameterValueBuilder;
        //    if (!valueTypeIsByRef && typeof(IAsyncCollector<string>).Equals(valueType))
        //    {
        //        parameterValueBuilder = (ctx) => Task.FromResult<object>(_hack.CreateFunctionParameter_AvailabilityTestInfoStringAsyncCollector(resultAttribute, ctx).Result);
        //    }
        //    if (!valueTypeIsByRef && typeof(string).Equals(valueType))
        //    {
        //        parameterValueBuilder = (ctx) => Task.FromResult<object>(_hack.CreateFunctionParameter_AvailabilityTestInfoStringAsyncCollector(resultAttribute, ctx).Result);
        //    }
        //    if (valueTypeIsByRef && typeof(string).MakeByRefType().Equals(valueType))
        //    {
        //        parameterValueBuilder = (ctx) => Task.FromResult<object>(_hack.CreateFunctionParameter_AvailabilityTestInfoStringAsyncCollector(resultAttribute, ctx).Result);
        //    }
        //    else
        //    {
        //        throw new ArgumentException($"Only values of the following types can be constructed based on the {nameof(AvailabilityTestResultAttribute)}:"
        //                                  + $" \"{nameof(IAsyncCollector<string>)}\"."
        //                                  + $" However, a value named \"{Format.NotNullOrWord(parameter.Name)}\" of type \"{valueType.FullName}\" was requested.");
        //    }

        //    var binding = new AvailabilityTestAttributeBinding(valueType, parameterName, parameterValueBuilder);
        //    return Task.FromResult<IBinding>(binding);
        //}

        public Task<IBinding> TryCreateAsync(BindingProviderContext bindingProviderContext)
        {
            Validate.NotNull(bindingProviderContext, nameof(bindingProviderContext));

            ParameterInfo parameter = bindingProviderContext.Parameter;

            //AvailabilityTestResultAttribute resultAttribute = parameter?.GetCustomAttribute<AvailabilityTestResultAttribute>(inherit: false);
            //if (resultAttribute != null)
            //{
            //    return TryCreateForResultAsync(bindingProviderContext);
            //}

            // Get and validate attribute:
            AvailabilityTestAttribute attribute = parameter?.GetCustomAttribute<AvailabilityTestAttribute>(inherit: false);
            if (attribute == null)
            {
                throw new InvalidOperationException($"An {nameof(AvailabilityTestAttributeBindingProvider)} should only be"
                                                 + $" registered to bind parameters tagged with the {nameof(AvailabilityTestAttribute)}."
                                                 + $" However, no such attribute was found.");
            }

            Console.WriteLine("Executing TryCreateAsync(..) for non-result AvailabilityTestAttribute.");

            // Get and validate parameter type:

            Type valueType = parameter.ParameterType;
            string parameterName = Format.NotNullOrWord(parameter.Name);

            Func<ValueBindingContext, Task<object>> parameterValueBuilder;

            if (typeof(AvailabilityTestInfo).Equals(valueType))
            {
                parameterValueBuilder = (ctx) => Task.FromResult<object>(_hack.CreateFunctionParameter_AvailabilityTestInfo(attribute, ctx).Result);
            }
            else if (typeof(AvailabilityTelemetry).Equals(valueType))
            {
                parameterValueBuilder = (ctx) => Task.FromResult<object>(_hack.CreateFunctionParameter_AvailabilityTelemetry(attribute, ctx).Result);
            }
            else if (typeof(JObject).Equals(valueType))
            {
                parameterValueBuilder = (ctx) => Task.FromResult<object>(_hack.CreateFunctionParameter_JObject(attribute, ctx).Result);
            }
            else if (typeof(String).Equals(valueType))
            {
                parameterValueBuilder = (ctx) => Task.FromResult<object>(_hack.CreateFunctionParameter_String(attribute, ctx).Result);
            }
            //else if (typeof(IAsyncCollector<string>).Equals(valueType))
            //{
            //    parameterValueBuilder = (ctx) => Task.FromResult<object>(_hack.CreateFunctionParameter_AvailabilityTestInfoStringAsyncCollector(attribute, ctx).Result);
            //}
            else if (typeof(Object).Equals(valueType))
            {
                parameterValueBuilder = (ctx) => Task.FromResult<object>(_hack.CreateFunctionParameter_AvailabilityTestInfo(attribute, ctx).Result);
            }
            else
            {
                throw new ArgumentException($"Only values of the following types can be constructed based on the {nameof(AvailabilityTestAttribute)}:"
                                          + $" \"{nameof(AvailabilityTestInfo)}\", \"{nameof(AvailabilityTelemetry)}\", \"{nameof(JObject)}\", \"{nameof(String)}\"."
                                          + $" However, a value named \"{Format.NotNullOrWord(parameter.Name)}\" of type \"{valueType.FullName}\" was requested.");
            }

            var binding = new AvailabilityTestAttributeBinding(valueType, parameterName, parameterValueBuilder);
            return Task.FromResult<IBinding>(binding);
        }
    }
}