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
        public static class ConfigurationKeys
        {
            public static class SectionNames
            {
                public const string AvailabilityTestResults = "AvailabilityTestResults";
                public const string AzureFunctionsJobHost = "AzureFunctionsJobHost";
            }

            public static class KeyNames
            {
                public const string TestDisplayName = "TestDisplayName";
                public const string LocationDisplayName = "LocationDisplayName";
                public const string LocationId = "LocationId";
            }

            public static class EnvironmentVariableNames
            {
                public const string TestDisplayName = SectionNames.AvailabilityTestResults + "." + KeyNames.TestDisplayName;

                public const string LocationDisplayName = SectionNames.AvailabilityTestResults + "." + KeyNames.LocationDisplayName;
                public const string LocationDisplayName_Fallback1 = "REGION_NAME";
                public const string LocationDisplayName_Fallback2 = "Location";

                public const string LocationId = SectionNames.AvailabilityTestResults + "." + KeyNames.LocationId;
            }
        }

        private class AvailabilityTestConfiguration : IAvailabilityTestConfiguration
        {
            public string TestDisplayName { get; }
            public string LocationDisplayName { get; }
            public string LocationId { get; }
            public AvailabilityTestConfiguration(string testDisplayName, string locationDisplayName, string locationId)
            {
                this.TestDisplayName = testDisplayName;
                this.LocationDisplayName = locationDisplayName;
                this.LocationId = locationId;
            }
        }

        private readonly IConfiguration _configuration;
        private readonly INameResolver _nameResolver;

        public AvailabilityTestScopeSettingsResolver(IConfiguration configuration, INameResolver nameResolver)
        {
            _configuration = configuration;
            _nameResolver = nameResolver;
        }

        public IAvailabilityTestConfiguration Resolve(IAvailabilityTestConfiguration testConfig, string functionName)
        {
            // Whenever a setting is missing, attempt to fill it from the config ir the environment:

            // Test Display Name:
            string testDisplayName = testConfig?.TestDisplayName;
            
            testDisplayName = TryFillValueFromConfig(
                                    testDisplayName,
                                    ConfigurationKeys.SectionNames.AvailabilityTestResults,
                                    ConfigurationKeys.KeyNames.TestDisplayName);

            testDisplayName = TryFillValueFromEnvironment(
                                    testDisplayName,
                                    ConfigurationKeys.EnvironmentVariableNames.TestDisplayName);

            if (String.IsNullOrWhiteSpace(testDisplayName))
            {
                testDisplayName = functionName;
            }

            if (String.IsNullOrWhiteSpace(testDisplayName))
            {
                throw new ArgumentException("The Availability Test Display Name must be set, but it was not."
                                          +  " To set that value, use one of the following (in order of precedence):"
                                          + $" (a) Explicitly set the property \"{nameof(AvailabilityTestResultAttribute.TestDisplayName)}\""
                                          + $" on the '{AvailabilityTestResultAttribute.BindingTypeName}'-binding"
                                          +  " (via the attribute or via function.json) (%%-tags are supported);"
                                          + $" (b) Use the App Setting \"{ConfigurationKeys.KeyNames.TestDisplayName}\" in"
                                          + $" configuration section \"{ConfigurationKeys.SectionNames.AvailabilityTestResults}\";"
                                          + $" (c) Use an environment variable \"{ConfigurationKeys.EnvironmentVariableNames.TestDisplayName}\";"
                                          +  " (d) The name of the Azure Function will be used as a fallback.");
            }


            // Location Display Name:
            string locationDisplayName = testConfig?.LocationDisplayName;

            locationDisplayName = TryFillValueFromConfig(
                                    locationDisplayName,
                                    ConfigurationKeys.SectionNames.AvailabilityTestResults,
                                    ConfigurationKeys.KeyNames.LocationDisplayName);

            locationDisplayName = TryFillValueFromEnvironment(
                                    locationDisplayName,
                                    ConfigurationKeys.EnvironmentVariableNames.LocationDisplayName);

            locationDisplayName = TryFillValueFromEnvironment(
                                    locationDisplayName,
                                    ConfigurationKeys.EnvironmentVariableNames.LocationDisplayName_Fallback1);

            locationDisplayName = TryFillValueFromEnvironment(
                                    locationDisplayName,
                                    ConfigurationKeys.EnvironmentVariableNames.LocationDisplayName_Fallback2);

            if (String.IsNullOrWhiteSpace(locationDisplayName))
            {
                throw new ArgumentException("The Location Display Name of the Availability Test must be set, but it was not."
                                          +  " To set that value, use one of the following (in order of precedence):"
                                          + $" (a) Explicitly set the property \"{nameof(AvailabilityTestResultAttribute.LocationDisplayName)}\""
                                          + $" on the '{AvailabilityTestResultAttribute.BindingTypeName}'-binding"
                                          +  " (via the attribute or via function.json) (%%-tags are supported);"
                                          + $" (b) Use the App Setting \"{ConfigurationKeys.KeyNames.LocationDisplayName}\" in"
                                          + $" configuration section \"{ConfigurationKeys.SectionNames.AvailabilityTestResults}\";"
                                          + $" (c) Use the environment variable \"{ConfigurationKeys.EnvironmentVariableNames.LocationDisplayName}\";"
                                          + $" (d) Use the environment variable \"{ConfigurationKeys.EnvironmentVariableNames.LocationDisplayName_Fallback1}\";"
                                          + $" (e) Use the environment variable \"{ConfigurationKeys.EnvironmentVariableNames.LocationDisplayName_Fallback2}\".");
            }

            // Location Id:
            string locationId = testConfig?.LocationId;

            locationId = TryFillValueFromConfig(
                                    locationId,
                                    ConfigurationKeys.SectionNames.AvailabilityTestResults,
                                    ConfigurationKeys.KeyNames.LocationId);

            locationId = TryFillValueFromEnvironment(
                                    locationId,
                                    ConfigurationKeys.EnvironmentVariableNames.LocationId);

            if (locationId == null)
            {
                locationId = Format.LocationNameAsId(locationDisplayName);
            }

            if (String.IsNullOrWhiteSpace(locationId))
            {
                throw new ArgumentException($"The Location Id of the Availability Test must be set, but it was not."
                                          + $" To set that value, use one of the following (in order of precedence):"
                                          + $" (a) Explicitly set the property \"{nameof(AvailabilityTestResultAttribute.LocationId)}\""
                                          + $" on the '{AvailabilityTestResultAttribute.BindingTypeName}'-binding"
                                          +  " (via the attribute or via function.json) (%%-tags are supported);"
                                          + $" (b) Use the App Setting \"{ConfigurationKeys.KeyNames.LocationId}\" in"
                                          + $" configuration section \"{ConfigurationKeys.SectionNames.AvailabilityTestResults}\";"
                                          + $" (c) Use the environment variable \"{ConfigurationKeys.EnvironmentVariableNames.LocationId}\";"
                                          + $" (d) As a fallback, a Location Id will be derived from the Location Display Name (only if that is set).");
            }

            // We did our best to get the config. 

            var resolvedConfig = new AvailabilityTestScopeSettingsResolver.AvailabilityTestConfiguration(testDisplayName, locationDisplayName, locationId);
            return resolvedConfig;
        }

        private string TryFillValueFromConfig(string value, string configSectionName, string configKeyName)
        {
            // If we already have value, we are done:
            if (false == String.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            // Try getting from configuration:
            string valueFromConfig = null;
            try
            {
                if (_configuration != null && configKeyName != null)
                {
                    if (configSectionName == null)
                    {
                        // Try WITHOUT using the 'AzureFunctionsJobHost'-root:
                        valueFromConfig = _configuration[configKeyName];

                        // Try WITH using the 'AzureFunctionsJobHost'-root:
                        if (String.IsNullOrWhiteSpace(valueFromConfig))
                        {
                            IConfiguration jobHostConfigSection = _configuration.GetSection(ConfigurationKeys.SectionNames.AzureFunctionsJobHost);
                            valueFromConfig = jobHostConfigSection[configKeyName];
                        }
                    }
                    else
                    {
                        // Try WITHOUT using the 'AzureFunctionsJobHost'-root:
                        IConfiguration configSection = _configuration.GetSection(configSectionName);
                        valueFromConfig = configSection[configKeyName];

                        // Try WITH using the 'AzureFunctionsJobHost'-root:
                        if (String.IsNullOrWhiteSpace(valueFromConfig))
                        {
                            IConfiguration jobHostConfigSection = _configuration.GetSection(ConfigurationKeys.SectionNames.AzureFunctionsJobHost);
                            IConfiguration configSubsection = jobHostConfigSection.GetSection(configSectionName);
                            valueFromConfig = configSubsection[configKeyName];
                        }
                    }
                }
            }
            catch
            { }

            // Apply name resolution:
            if (valueFromConfig != null)
            {
                value = NameResolveWholeStringRecursive(valueFromConfig);
            }

            // Nothing else we can do:
            return value;
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
