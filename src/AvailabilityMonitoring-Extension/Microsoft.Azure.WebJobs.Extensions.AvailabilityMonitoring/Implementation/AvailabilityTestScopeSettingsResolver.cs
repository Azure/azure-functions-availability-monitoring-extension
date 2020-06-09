using Microsoft.Azure.AvailabilityMonitoring;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    internal class AvailabilityTestScopeSettingsResolver
    {
        public static class EnvironmentVariableNames
        {
            public const string LocationDisplayName1 = "REGION_NAME";
            public const string LocationDisplayName2 = "Location";
        }

        private class AvailabilityTestConfiguration : IAvailabilityTestInternalConfiguration
        {
            public string TestDisplayName { get; }
            public string LocationDisplayName { get; }
            public AvailabilityTestConfiguration(string testDisplayName, string locationDisplayName)
            {
                this.TestDisplayName = testDisplayName;
                this.LocationDisplayName = locationDisplayName;
            }
        }

        private readonly INameResolver _nameResolver;

        public AvailabilityTestScopeSettingsResolver(INameResolver nameResolver)
        {
            _nameResolver = nameResolver;
        }

        public IAvailabilityTestInternalConfiguration Resolve(IAvailabilityTestConfiguration testConfig, string functionName)
        {
            // Test Display Name:
            string testDisplayName = testConfig?.TestDisplayName;
            if (String.IsNullOrWhiteSpace(testDisplayName))
            {
                testDisplayName = functionName;
            }

            if (String.IsNullOrWhiteSpace(testDisplayName))
            {
                throw new ArgumentException("The Availability Test Display Name must be set, but it was not."
                                          +  " To set that value, explicitly set the property \"{nameof(AvailabilityTestResultAttribute.TestDisplayName)}\""
                                          + $" Otherwise the name of the Azure Function will be used as a fallback.");
            }


            // Location Display Name:
            string locationDisplayName = TryFillValueFromEnvironment(null, EnvironmentVariableNames.LocationDisplayName1);
            locationDisplayName = TryFillValueFromEnvironment(locationDisplayName, EnvironmentVariableNames.LocationDisplayName2);

            if (String.IsNullOrWhiteSpace(locationDisplayName))
            {
                throw new ArgumentException("The Location Display Name of the Availability Test must be set, but it was not."
                                          +  " Check that one of the following environment variables are set:"
                                          + $" (a) \"{EnvironmentVariableNames.LocationDisplayName1}\";"
                                          + $" (b) \"{EnvironmentVariableNames.LocationDisplayName2}\".");
            }

            // We did our best to get the config. 

            var resolvedConfig = new AvailabilityTestScopeSettingsResolver.AvailabilityTestConfiguration(testDisplayName, locationDisplayName);
            return resolvedConfig;
        }

        private string TryFillValueFromEnvironment(string value, string environmentVariableName)
        {
            // If we already have value, we are done:
            if (false == String.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            // In case we had no configuration, try looking in the environment explicitly:
            string valueFromEnvironment = null;
            try
            {
                valueFromEnvironment = Environment.GetEnvironmentVariable(environmentVariableName);
            }
            catch
            { }

            // Apply name resolution
            if (valueFromEnvironment != null)
            {
                value = NameResolveWholeStringRecursive(valueFromEnvironment);
            }

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
