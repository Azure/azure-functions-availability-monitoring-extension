using System;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.Azure.AvailabilityMonitoring;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    internal class AvailabilityTestFunctionRegistry
    {
        private class AvailabilityTestRegistration
        {
            public string FunctionName { get; private set; }
            public IAvailabilityTestConfiguration Config { get; private set; }
            public bool IsAvailabilityTest { get; private set; }

            public AvailabilityTestRegistration(string functionName, IAvailabilityTestConfiguration config, bool isAvailabilityTest)
            {
                Validate.NotNullOrWhitespace(functionName, nameof(functionName));
                Validate.NotNull(config, nameof(config));
                
                this.FunctionName = functionName;
                this.Config = config;
                this.IsAvailabilityTest = isAvailabilityTest;
            }
        }


        private readonly ConcurrentDictionary<string, AvailabilityTestRegistration> _registeredAvailabilityTests;


        public AvailabilityTestFunctionRegistry()
        {
            _registeredAvailabilityTests = new ConcurrentDictionary<string, AvailabilityTestRegistration>(StringComparer.OrdinalIgnoreCase);
        }


        public void Register(string functionName, IAvailabilityTestConfiguration testConfig, ILogger log)
        {
            GetOrRegister(functionName, testConfig, isAvailabilityTest: true, log, "based on a code attribute annotation");
        }


// Type 'FunctionInvocationContext' (and other Filter-related types) is marked as preview/obsolete,
// but the guidance from the Azure Functions team is to use it, so we disable the warning.
#pragma warning disable CS0618
        public bool IsAvailabilityTest(FunctionInvocationContext functionInvocationContext, out string functionName, out IAvailabilityTestConfiguration testConfig)
#pragma warning restore CS0618 
        {
            Validate.NotNull(functionInvocationContext, nameof(functionInvocationContext));

            functionName = functionInvocationContext.FunctionName;
            Validate.NotNullOrWhitespace(functionName, "functionInvocationContext.FunctionName");

            // In most cases we have already registered the Function:
            // either by callign this method from the filter during an earlier execution (out-of-proc languages)
            // or by calling Register(..) from the binding (.Net (in-proc) functions).

            if (_registeredAvailabilityTests.TryGetValue(functionName, out AvailabilityTestRegistration registration))
            {
                testConfig = registration.Config;
                return registration.IsAvailabilityTest;
            }

            ILogger log = functionInvocationContext.Logger;
            log = AvailabilityTest.Log.CreateFallbackLogIfRequired(log);

            // Getting here means that we are executing out-of-proc language function for the first time.
            // In such cases, bindings happen late and dynamically, AFTER filters. Thus, NO binding has yet occurred.
            // We will read the function metadata to see if the return value of the function is tagged with the right attribute. 

            try
            {
                // Attempt to parse the function metadata file. This will throw if something goes wrong.
                // We will catch immediately, but this is rare if it happens at all) and helps attaching debuggers.
                GetTestConfigFromMetadata(functionName, functionInvocationContext, log, out bool isAvailabilityTest, out testConfig);

                // We got here becasue the function was not registered, so take the insertion path right away:
                GetOrRegisterSlow(functionName, testConfig, isAvailabilityTest, log, "based on the function metadata file");
                return isAvailabilityTest;
            }
            catch(Exception ex)
            {
                log.LogError(ex,
                            $"Error while processing function metadata file to determine whether this function is a Coded Availability Test:"
                           + " FunctionName=\"{FunctionName}\", {{ErrorType=\"{ErrorType}\", {{ErrorMessage=\"{ErrorMessage}\"}}",
                             functionName,
                             ex.GetType().Name,
                             ex.Message);

                // We could not conclusively determine the aswer from metadata.
                // We assume "NOT an Availability Test", but we do not cache this, so we will keep checking in case this was some transient IO error.
                // We are not worried about the resulting perf impace, bacause this should not happen anyway.

                testConfig = null;
                return false;
            }
        }


        private IAvailabilityTestConfiguration GetOrRegister(string functionName,
                                                             IAvailabilityTestConfiguration testConfig, 
                                                             bool isAvailabilityTest, 
                                                             ILogger log, 
                                                             string causeDescriptionMsg)
        {
            Validate.NotNullOrWhitespace(functionName, nameof(functionName));
            Validate.NotNull(testConfig, nameof(testConfig));
            causeDescriptionMsg = causeDescriptionMsg ?? "unknown reason";

            // The test will be already registered in all cases, except the first invocation.
            // Optimize for that and pay a small perf premium during the very first invocation.

            if (_registeredAvailabilityTests.TryGetValue(functionName, out AvailabilityTestRegistration registration))
            {
                if (registration.IsAvailabilityTest != isAvailabilityTest)
                {
                    throw new InvalidOperationException($"Registering Funtion \"{functionName}\"as {(isAvailabilityTest ? "" : "NOT")} "
                                                      + $"a Coded Availability Test ({causeDescriptionMsg}),"
                                                      +  " but a Function with the same name is already registered as with the opposite"
                                                      +  " IsAvailabilityTest-setting. Are you mixing .Net-based (in-proc) and"
                                                      +  " non-.Net (out-of-proc) Functions in the same App and share the same Function name?"
                                                      +  " That scenario that is not supported.");
                }

                return registration.Config;
            }

            // We did not have a registration. Let's try to insert one:
            return GetOrRegisterSlow(functionName, testConfig, isAvailabilityTest, log, causeDescriptionMsg);
        }


        private IAvailabilityTestConfiguration GetOrRegisterSlow(string functionName, 
                                                                 IAvailabilityTestConfiguration testConfig, 
                                                                 bool isAvailabilityTest, 
                                                                 ILogger log, 
                                                                 string causeDescriptionMsg)
        {
            AvailabilityTestRegistration newRegistration = null;
            AvailabilityTestRegistration usedRegistration = _registeredAvailabilityTests.GetOrAdd(
                        functionName,
                        (fn) =>
                        {
                            newRegistration = new AvailabilityTestRegistration(functionName, testConfig, isAvailabilityTest);
                            return newRegistration;
                        });

            if (usedRegistration == newRegistration)
            {
                log = AvailabilityTest.Log.CreateFallbackLogIfRequired(log);

                if (isAvailabilityTest)
                {
                    log?.LogInformation($"A new Coded Availability Test was discovered ({causeDescriptionMsg}):"
                                       + " {{ FunctionName=\"{FunctionName}\" }}",
                                        functionName);
                }
                else
                {
                    log?.LogInformation($"A Function was registered as NOT a Coded Availability Test ({causeDescriptionMsg}):"
                                       + " {{ FunctionName=\"{FunctionName}\" }}",
                                        functionName);
                }
            }
            else
            {
                if (usedRegistration.IsAvailabilityTest != isAvailabilityTest)
                {
                    throw new InvalidOperationException($"Registering Funtion \"{functionName}\"as {(isAvailabilityTest ? "" : "NOT")} "
                                                          + $"a Coded Availability Test ({causeDescriptionMsg}),"
                                                          + " but a Function with the same name is already registered as with the opposite"
                                                          + " IsAvailabilityTest-setting. Are you mixing .Net-based (in-proc) and"
                                                          + " non-.Net (out-of-proc) Functions in the same App and share the same Function name?"
                                                          + " That scenario that is not supported.");
                }
            }

            return usedRegistration.Config;
        }


// Type 'FunctionInvocationContext' (and other Filter-related types) is marked as preview/obsolete,
// but the guidance from the Azure Functions team is to use it, so we disable the warning.
#pragma warning disable CS0618
        private static void GetTestConfigFromMetadata(string functionName,
                                               FunctionInvocationContext functionInvocationContext, 
                                               ILogger log, 
                                               out bool isAvailabilityTest, 
                                               out IAvailabilityTestConfiguration testConfig)
#pragma warning restore CS0618 
        {
            // We will do very verbose error checking and logging via exception here to aid supportability
            // in case out assumptions about Function Runtime behaviur get violated.

            const string BeginAnalysisLogMessage = "Analysis of function metadata file to determine whether this function"
                                                    + " is a Coded Availability Test beginning:"
                                                    + " {{FunctionName=\"{FunctionName}\"}}";

            const string FinishAnalysisLogMessage = "Analysis of function metadata file to determine whether this function"
                                                    + " is a Coded Availability Test finished:"
                                                    + " {{FunctionName=\"{FunctionName}\", IsAvailabilityTest=\"{IsAvailabilityTest}\"}}";

            log?.LogDebug(BeginAnalysisLogMessage, functionName);

            string metadataFileContent = ReadFunctionMetadataFile(functionInvocationContext);

            FunctionMetadata functionMetadata = JsonConvert.DeserializeObject<FunctionMetadata>(metadataFileContent);

            if (functionMetadata == null)
            {
                throw new InvalidOperationException($"Could not parse the function metadata for function \"{functionName}\".");
            }
                
            if (functionMetadata.Bindings == null)
            {
                throw new InvalidOperationException($"The function metadata for function \"{functionName}\" was parsed,"
                                                    + " but it did not contain a list of bindings.");
            }

            if (functionMetadata.Bindings.Count == 0)
            {
                throw new InvalidOperationException($"The function metadata for function \"{functionName}\" was parsed;"
                                                    + " it contained a list of bindings, but the list had no entries.");
            }

            foreach (BindingMetadata bindingMetadata in functionMetadata.Bindings)
            {
                if (bindingMetadata == null || bindingMetadata.Type == null)
                {
                    continue;
                }

                if (bindingMetadata.Type.Equals(AvailabilityTestResultAttribute.BindingTypeName, StringComparison.OrdinalIgnoreCase)
                        || bindingMetadata.Type.Equals(nameof(AvailabilityTestResultAttribute), StringComparison.OrdinalIgnoreCase))
                {
                    isAvailabilityTest = true;
                    testConfig = bindingMetadata;

                    log?.LogDebug(FinishAnalysisLogMessage, functionName, isAvailabilityTest);
                    return;
                }
            }

            isAvailabilityTest = false;
            testConfig = null;

            log?.LogDebug(FinishAnalysisLogMessage, functionName, isAvailabilityTest);
            return;
        }


// Type 'FunctionInvocationContext' (and other Filter-related types) is marked as preview/obsolete,
// but the guidance from the Azure Functions team is to use it, so we disable the warning.
#pragma warning disable CS0618
        private static string ReadFunctionMetadataFile(FunctionInvocationContext functionInvocationContext)
#pragma warning restore CS0618 
        {
            // We will do very verbose error checking and logging via exceptions here to aid supportability
            // in case out assumptions about Function Runtime behaviur get violated.

            // For out-of-proc languages, the _context parameter should contain info about the runtime environment.
            // It should be of type ExecutionContext.
            // ExecutionContext should have info about the location of the function metadata file.

            Validate.NotNull(functionInvocationContext.Arguments, "functionInvocationContext.Arguments");

            const string NeedContextArgumentErrorPrefix = "For non-.Net (out-of-proc) functions, the Arguments table of the specified"
                                                        + " FunctionInvocationContext is expected to have a an entry with the key \"_context\""
                                                        + " and a value of type \"ExecutionContext\".";

            if (! functionInvocationContext.Arguments.TryGetValue("_context", out object execContextObj))
            {
                throw new InvalidOperationException(NeedContextArgumentErrorPrefix + " However, such entry does not exist.");
            }

            if (execContextObj == null)
            {
                throw new InvalidOperationException(NeedContextArgumentErrorPrefix + " Such entry exists, but the value is null.");
            }

            string metadataFilePath;
            if (execContextObj is ExecutionContext execContext)
            {
                metadataFilePath = GetFullFunctionMetadataPath(execContext);
            }
            else
            { 
                throw new InvalidOperationException(NeedContextArgumentErrorPrefix
                                                 + $" Such entry exists, but is has the wrong type (\"{execContextObj.GetType().Name}\").");
            }
            
            string metadataFileContent = File.ReadAllText(metadataFilePath);
            return metadataFileContent;
        }

        private static string GetFullFunctionMetadataPath(ExecutionContext execContext)
        {
            const string functionJson = "function.json";

            string functionDir = execContext.FunctionDirectory ?? String.Empty;
            string metadataFilePathInFuncDir = Path.Combine(functionDir, functionJson);

            if (File.Exists(metadataFilePathInFuncDir))
            {
                return metadataFilePathInFuncDir;
            }

            // We did not find function.json where it should be (in FunctionDirectory).
            // Let us attempt to look in FunctionAppDirectory as a fallback.
            // @ToDo: Is this reqired / safe?

            string functionAppDir = execContext.FunctionAppDirectory ?? String.Empty;
            string metadataFilePathInAppDir = Path.Combine(functionAppDir, functionJson);

            if (File.Exists(metadataFilePathInAppDir))
            {
                return metadataFilePathInAppDir;
            }

            throw new InvalidOperationException($"Looked for the Function Metadata File (\"{functionJson}\") first in"
                                              + $" \"{metadataFilePathInFuncDir}\" and then in \"{metadataFilePathInAppDir}\","
                                              +  " but that file does not exist.");
        }
    }
}
