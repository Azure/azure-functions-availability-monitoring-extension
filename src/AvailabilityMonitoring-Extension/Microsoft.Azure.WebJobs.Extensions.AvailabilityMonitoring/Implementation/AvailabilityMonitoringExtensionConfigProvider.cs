using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DataContracts;
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
        private readonly IConfiguration _configuration;
        private readonly INameResolver _nameResolver;
        //private readonly ILogger _log;

        public AvailabilityMonitoringExtensionConfigProvider(IConfiguration configuration, INameResolver nameResolver)
        {
            _configuration = configuration;
            _nameResolver = nameResolver;
            //_log = log;
        }

        public void Initialize(ExtensionConfigContext context)
        {
            Validate.NotNull(context, nameof(context));

            //_log?.LogInformation("Initializing Availability Monitoring Extension.");

            // FluentBindingRule<ApiAvailabilityTest> is marked as Obsolete, yet it is the type returned from AddBindingRule(..)
            // We could use "var", but one should NEVER use "var" except in Lync expressions
            // or when the type is clear from the *same* line to an unfamiliar reader.
            // Neither is the case, so we use the type explicitly and work around the obsolete-warning.
#pragma warning disable CS0618 
            FluentBindingRule<AvailabilityTestAttribute> rule = context.AddBindingRule<AvailabilityTestAttribute>();
#pragma warning restore CS0618 

            rule.BindToInput<AvailabilityTestInfo>(CreateFunctionParameter_AvailabilityTestInfo);
            rule.BindToInput<AvailabilityTelemetry>(CreateFunctionParameter_AvailabilityTelemetry);
            rule.BindToInput<JObject>(CreateFunctionParameter_JObject);
            rule.BindToInput<String>(CreateFunctionParameter_String);

            //rule.AddConverter<IAsyncCollector<string>, string>( (c) => (c as AvailabilityTestInfoStringAsyncCollector)?.InitialValue ?? c.GetType().Name);

            //var bindingProvider = new AvailabilityTestAttributeBindingProvider(hack: this);
            //rule.SetPostResolveHook(PostResolveHook)
            //    .Bind(bindingProvider);

            //var bindingProvider = new AvailabilityTestAttributeBindingProvider(hack: this);
            //rule.Bind(bindingProvider);

            //rule.BindToValueProvider(HackBinderFactory);

//#pragma warning disable CS0618
//            FluentBindingRule<AvailabilityTestResultAttribute> resultAttributeRule = context.AddBindingRule<AvailabilityTestResultAttribute>();
//#pragma warning restore CS0618 

//            var resultBindingProvider = new AvailabilityTestAttributeBindingProvider(hack: this);
//            resultAttributeRule.Bind(resultBindingProvider);

        }

        private class AvailabilityParameterDescriptor : ParameterDescriptor
        {
            public Guid DescriptorId;
        }

        private ParameterDescriptor PostResolveHook(AvailabilityTestAttribute attribute, ParameterInfo parameter, INameResolver nameResolver)
        {
            var desc = new AvailabilityParameterDescriptor
            {
                Name = parameter.Name,
                Type = parameter.ParameterType.GetType().Name,
                DescriptorId = Guid.NewGuid()
            };

            Console.WriteLine();
            Console.WriteLine($"PostResolveHook(..) invoked: Name: {desc.Name}, Type: {desc.Type}, DescriptorId: {desc.DescriptorId}.");
            Console.WriteLine();

            return desc;
        }


        private Task<IValueBinder> HackBinderFactory(AvailabilityTestAttribute attribute, Type valueType)
        {
            var fakeFunctionBindingContext = new FunctionBindingContext(Guid.Empty, CancellationToken.None);
            var fakeValueBindingContext = new ValueBindingContext(fakeFunctionBindingContext, CancellationToken.None);

            Func<Task<object>> parameterValueBuilder;

            if (typeof(AvailabilityTestInfo).Equals(valueType))
            {
                parameterValueBuilder = () => Task.FromResult<object>(this.CreateFunctionParameter_AvailabilityTestInfo(attribute, fakeValueBindingContext).Result);
            }
            else if (typeof(AvailabilityTelemetry).Equals(valueType))
            {
                parameterValueBuilder = () => Task.FromResult<object>(this.CreateFunctionParameter_AvailabilityTelemetry(attribute, fakeValueBindingContext).Result);
            }
            else if (typeof(JObject).Equals(valueType))
            {
                parameterValueBuilder = () => Task.FromResult<object>(this.CreateFunctionParameter_JObject(attribute, fakeValueBindingContext).Result);
            }
            else if (typeof(String).Equals(valueType))
            {
                parameterValueBuilder = () => Task.FromResult<object>(this.CreateFunctionParameter_String(attribute, fakeValueBindingContext).Result);
            }
            else if (typeof(Object).Equals(valueType))
            {
                parameterValueBuilder = () => Task.FromResult<object>(this.CreateFunctionParameter_AvailabilityTestInfo(attribute, fakeValueBindingContext).Result);
            }
            else
            {
                throw new ArgumentException($"Only values of the following types can be constructed based on the {nameof(AvailabilityTestAttribute)}:"
                                          + $" \"{nameof(AvailabilityTestInfo)}\", \"{nameof(AvailabilityTelemetry)}\", \"{nameof(JObject)}\", \"{nameof(String)}\"."
                                          + $" However, a value of type \"{valueType.FullName}\" was requested.");
            }

            var valueBinder = new AvailabilityTestAttributeValueBinder(valueType, parameterName: "unknown", parameterValueBuilder);

            return Task.FromResult<IValueBinder>(valueBinder);
        }

        internal Task<AvailabilityTestInfo> CreateFunctionParameter_AvailabilityTestInfo(AvailabilityTestAttribute attribute, ValueBindingContext context)
        {
            Validate.NotNull(attribute, nameof(attribute));
            Validate.NotNull(context, nameof(context));

            AvailabilityTestInfo invocationInfo = CreateAndRegisterInvocation(attribute, context, typeof(AvailabilityTestInfo));
            return Task.FromResult(invocationInfo);
        }

        internal Task<AvailabilityTelemetry> CreateFunctionParameter_AvailabilityTelemetry(AvailabilityTestAttribute attribute, ValueBindingContext context)
        {
            Validate.NotNull(attribute, nameof(attribute));
            Validate.NotNull(context, nameof(context));

            AvailabilityTestInfo invocationInfo = CreateAndRegisterInvocation(attribute, context, typeof(AvailabilityTelemetry));
            return Task.FromResult(Convert.AvailabilityTestInfoToAvailabilityTelemetry(invocationInfo));
        }

        internal Task<JObject> CreateFunctionParameter_JObject(AvailabilityTestAttribute attribute, ValueBindingContext context)
        {
            Validate.NotNull(attribute, nameof(attribute));
            Validate.NotNull(context, nameof(context));

            AvailabilityTestInfo invocationInfo = CreateAndRegisterInvocation(attribute, context, typeof(JObject));
            return Task.FromResult(Convert.AvailabilityTestInfoToJObject(invocationInfo));
        } 

        internal Task<string> CreateFunctionParameter_String(AvailabilityTestAttribute attribute, ValueBindingContext context)
        {
            Validate.NotNull(attribute, nameof(attribute));
            Validate.NotNull(context, nameof(context));

            AvailabilityTestInfo invocationInfo = CreateAndRegisterInvocation(attribute, context, typeof(String));
            return Task.FromResult(Convert.AvailabilityTestInfoToString(invocationInfo));
        }

        internal Task<IAsyncCollector<string>> CreateFunctionParameter_AvailabilityTestInfoStringAsyncCollector(AvailabilityTestAttribute attribute, ValueBindingContext context)
        {
            Validate.NotNull(attribute, nameof(attribute));
            Validate.NotNull(context, nameof(context));

            AvailabilityTestInfo invocationInfo = CreateAndRegisterInvocation(attribute, context, typeof(String));

            string availabilityTestInfoString = Convert.AvailabilityTestInfoToString(invocationInfo);
            var asyncCollector = new AvailabilityTestInfoStringAsyncCollector(availabilityTestInfoString);

            return Task.FromResult<IAsyncCollector<string>>(asyncCollector);
        }

        private AvailabilityTestInfo CreateAndRegisterInvocation(AvailabilityTestAttribute attribute, ValueBindingContext context, Type functionParameterType)
        {
            //IDisposable logScope = _log?.BeginScope(new Dictionary<string, string>()
            //{
            //    ["Microsoft.Azure.AvailabilityMonitoring.FunctionInstanceId"] = Format.Guid(context.FunctionInstanceId),
            //});

            Console.WriteLine($"CreateAndRegisterInvocation. FunctionInstanceId: \"{context.FunctionInstanceId}\".");

            //try
            //{
            //_log?.LogInformation($"Creating an Availability Test parameter of type \"{functionParameterType.Name}\" from an"
            //                  + $" {nameof(AvailabilityTestAttribute)}("
            //                  + $"{nameof(AvailabilityTestAttribute.TestDisplayName)}=\"{Format.NotNullOrWord(attribute.TestDisplayName)}\","
            //                  + $"{nameof(AvailabilityTestAttribute.LocationDisplayName)}=\"{Format.NotNullOrWord(attribute.LocationDisplayName)}\","
            //                  + $"{nameof(AvailabilityTestAttribute.LocationId)}=\"{Format.NotNullOrWord(attribute.LocationId)}\").");

                Console.WriteLine($"Creating an Availability Test parameter of type \"{functionParameterType.Name}\" from an"
                                  + $" {nameof(AvailabilityTestAttribute)}("
                                  + $"{nameof(AvailabilityTestAttribute.TestDisplayName)}=\"{Format.NotNullOrWord(attribute.TestDisplayName)}\","
                                  + $"{nameof(AvailabilityTestAttribute.LocationDisplayName)}=\"{Format.NotNullOrWord(attribute.LocationDisplayName)}\","
                                  + $"{nameof(AvailabilityTestAttribute.LocationId)}=\"{Format.NotNullOrWord(attribute.LocationId)}\").");

                AvailabilityTestInfo availabilityTestInfo = CreateAvailabilityTestInfo(attribute, context);

                FunctionInvocationStateCache.SingeltonInstance.RegisterFunctionInvocation(context.FunctionInstanceId, availabilityTestInfo, functionParameterType);

                //_log?.LogInformation($"Resolved settings and created a {nameof(AvailabilityTestInfo)}:"
                //                 + $" {nameof(AvailabilityTestInfo.TestDisplayName)}=\"{Format.NotNullOrWord(availabilityTestInfo.TestDisplayName)}\","
                //                 + $" {nameof(AvailabilityTestInfo.LocationDisplayName)}=\"{Format.NotNullOrWord(availabilityTestInfo.LocationDisplayName)}\","
                //                 + $" {nameof(AvailabilityTestInfo.LocationId)}=\"{Format.NotNullOrWord(availabilityTestInfo.LocationId)}\".");

                Console.WriteLine($"Resolved settings and created a {nameof(AvailabilityTestInfo)}:"
                                 + $" {nameof(AvailabilityTestInfo.TestDisplayName)}=\"{Format.NotNullOrWord(availabilityTestInfo.TestDisplayName)}\","
                                 + $" {nameof(AvailabilityTestInfo.LocationDisplayName)}=\"{Format.NotNullOrWord(availabilityTestInfo.LocationDisplayName)}\","
                                 + $" {nameof(AvailabilityTestInfo.LocationId)}=\"{Format.NotNullOrWord(availabilityTestInfo.LocationId)}\".");

            return availabilityTestInfo;
            //}
            //finally
            //{
            //    logScope?.Dispose();
            //}
        }

        private AvailabilityTestInfo CreateAvailabilityTestInfo(AvailabilityTestAttribute attribute, ValueBindingContext context)
        {
            // Get the test info settings from the attribute:
            string testDisplayName = attribute.TestDisplayName;
            string locationDisplayName = attribute.LocationDisplayName;
            string locationId = attribute.LocationId;

            // Whenever a setting is missing, attempt to fill it from the config ir the environment:

            // Test Display Name
            testDisplayName = TryFillValueFromConfig(testDisplayName, AvailabilityTestAttribute.DefaultConfigKeys.TestDisplayName);
            if (String.IsNullOrWhiteSpace(testDisplayName))
            {
                testDisplayName = context.FunctionContext.MethodName;
            }

            if (String.IsNullOrWhiteSpace(testDisplayName))
            {
                throw new ArgumentException($"The Availability Test Display Name must be set, but it was not."
                                          + $" To set that value, use one of the following (in order of precedence):"
                                          + $" (a) Explicitly set the property {nameof(AvailabilityTestAttribute)}"
                                          + $".{nameof(AvailabilityTestAttribute.TestDisplayName)} (%%-tags are supported);"
                                          + $" (b) Use the App Setting \"{AvailabilityTestAttribute.DefaultConfigKeys.TestDisplayName}\";"
                                          + $" (c) Use an environment variable \"{AvailabilityTestAttribute.DefaultConfigKeys.TestDisplayName}\";"
                                          + $" (d) The name of the Azure Function will be used as a fallback.");
            }

            // Location Display Name
            const string LocationDisplayName_FallbackKey1 = "REGION_NAME";
            const string LocationDisplayName_FallbackKey2 = "Location";

            locationDisplayName = TryFillValueFromConfig(locationDisplayName, AvailabilityTestAttribute.DefaultConfigKeys.LocationDisplayName);
            locationDisplayName = TryFillValueFromConfig(locationDisplayName, LocationDisplayName_FallbackKey1);
            locationDisplayName = TryFillValueFromConfig(locationDisplayName, LocationDisplayName_FallbackKey2);

            if (String.IsNullOrWhiteSpace(locationDisplayName))
            {
                throw new ArgumentException($"The Location Display Name of the Availability Test must be set, but it was not."
                                          + $" To set that value, use one of the following (in order of precedence):"
                                          + $" (a) Explicitly set the property {nameof(AvailabilityTestAttribute)}"
                                          + $".{nameof(AvailabilityTestAttribute.LocationDisplayName)} (%%-tags are supported);"
                                          + $" (b) Use the App Setting \"{AvailabilityTestAttribute.DefaultConfigKeys.LocationDisplayName}\";"
                                          + $" (c) Use an environment variable \"{AvailabilityTestAttribute.DefaultConfigKeys.LocationDisplayName}\";"
                                          + $" (d) Use an App Setting (or the environment variable) \"{LocationDisplayName_FallbackKey1}\";"
                                          + $" (e) Use an App Setting (or the environment variable) \"{LocationDisplayName_FallbackKey2}\".");
            }

            // Location Id
            locationId = TryFillValueFromConfig(locationId, AvailabilityTestAttribute.DefaultConfigKeys.LocationId);
            locationId = locationId ?? locationDisplayName?.Trim()?.ToLowerInvariant()?.Replace(' ', '-');

            if (String.IsNullOrWhiteSpace(locationDisplayName))
            {
                throw new ArgumentException($"The Location Id of the Availability Test must be set, but it was not."
                                          + $" To set that value, use one of the following (in order of precedence):"
                                          + $" (a) Explicitly set the property {nameof(AvailabilityTestAttribute)}"
                                          + $".{nameof(AvailabilityTestAttribute.LocationId)} (%%-tags are supported);"
                                          + $" (b) Use the App Setting \"{AvailabilityTestAttribute.DefaultConfigKeys.LocationId}\";"
                                          + $" (c) As a fallback, a Location Id will be derived from the Location Display Name (if that is set).");
            }

            // We did our best to get the value. Create the Test Info now:

            var availabilityTestInfo = new AvailabilityTestInfo(testDisplayName,
                                                                //attribute.TestArmResourceName,
                                                                locationDisplayName,
                                                                locationId);
            return availabilityTestInfo;
        }

        private string TryFillValueFromConfig(string value, string configKey)
        {
            // If we already have value, we are done:
            if (false == String.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            // Try getting from configuration:
            if (_configuration != null)
            {
                value = _configuration[configKey];
                value = NameResolveWholeStringRecursive(value);
            }

            // If we have value now, we are done:
            if (false == String.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            // In case we had no configuration, try looking in the environment explicitly:
            value = Environment.GetEnvironmentVariable(configKey);
            value = NameResolveWholeStringRecursive(value);

            // Nothing else we can do:
            return value;
        }

        private string NameResolveWholeStringRecursive(string name)
        {
            if (_nameResolver == null || String.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            string resolvedName = _nameResolver.ResolveWholeString(name);
            while (false == String.IsNullOrWhiteSpace(resolvedName) && false == resolvedName.Equals(name, StringComparison.Ordinal))
            {
                name = resolvedName;
                resolvedName = _nameResolver.ResolveWholeString(name);
            }

            return resolvedName;
        }
    }
}
