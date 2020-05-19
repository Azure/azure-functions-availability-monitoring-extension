using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.AvailabilityMonitoring
{
    public class AvailabilityTestScope : IDisposable
    {
        public enum Stage : Int32
        {
            New = 10,
            Started = 20,
            Completed = 30,
            SentResults = 40
        }

        private const string DefaultResultMessage_NoError_Pass =         "Passed: Availability Test completed normally and reported Success.";
        private const string DefaultResultMessage_NoError_Fail =         "Failed: Availability Test completed normally and reported Failure.";
        private const string DefaultResultMessage_Error_Exception =      "Error: Availability Test resulted in an exception.";
        private const string DefaultResultMessage_Error_Timeout =        "Error: Availability Test timed out.";
        private const string DefaultResultMessage_NoResult_Disposed =    "No Result: Availability Test was disposed, but no result was set. A Failure is assumed.";
        private const string DefaultResultMessage_NoResult_NotDisposed = "No Result: Availability Test was not disposed, and no result was set. A Failure is assumed.";

        private const string PropertyName_WebtestLocationId = "WebtestLocationId";

        private const string PropertyName_AssociatedException_Type =      "AssociatedException.Type";
        private const string PropertyName_AssociatedException_Message =   "AssociatedException.Message";
        private const string PropertyName_AssociatedException_IsTimeout = "AssociatedException.IsTimeout";

        private const string PropertyName_AssociatedAvailabilityResult_Name =        "AssociatedAvailabilityResult.Name";
        private const string PropertyName_AssociatedAvailabilityResult_RunLocation = "AssociatedAvailabilityResult.RunLocation";
        private const string PropertyName_AssociatedAvailabilityResult_Id =          "AssociatedAvailabilityResult.Id";
        private const string PropertyName_AssociatedAvailabilityResult_IsTimeout =   "AssociatedAvailabilityResult.IsTimeout";

        private readonly string _instrumentationKey;
        private readonly TelemetryClient _telemetryClient;
        private readonly bool _flushOnDispose;
        private readonly ILogger _log;
        private readonly object _logScope;

        private int _currentStage;

        private Activity _activitySpan = null;
        private string _activitySpanId = null;
        private string _activitySpanOperationName = null;
        private string _distributedOperationId = null;
        private DateTimeOffset _startTime = default;
        private DateTimeOffset _endTime = default;
        private AvailabilityTelemetry _finalAvailabilityResult = null;

        public AvailabilityTestScope.Stage CurrentStage { get { return (AvailabilityTestScope.Stage) _currentStage; } }

        public string TestDisplayName { get; }

        public string LocationDisplayName { get; }

        public string LocationId { get; }

        public string ActivitySpanOperationName 
        {
            get 
            {
                string activitySpanOperationName = _activitySpanOperationName;
                if (activitySpanOperationName == null)
                {
                    throw new InvalidOperationException($"{nameof(ActivitySpanOperationName)} is not available before this {nameof(AvailabilityTestScope)} has been started.");
                }

                return activitySpanOperationName;
            } 
        }

        public string DistributedOperationId
        {
            get
            {
                string distributedOperationId = _distributedOperationId;
                if (distributedOperationId == null)
                {
                    throw new InvalidOperationException($"{nameof(DistributedOperationId)} is not available before this {nameof(AvailabilityTestScope)} has been started.");
                }

                return distributedOperationId;
            }
        }

        public AvailabilityTestScope(string testDisplayName, string locationDisplayName, string locationId, TelemetryConfiguration telemetryConfig, bool flushOnDispose, ILogger log, object logScope)
        {
            Validate.NotNullOrWhitespace(testDisplayName, nameof(testDisplayName));
            Validate.NotNullOrWhitespace(locationDisplayName, nameof(locationDisplayName));
            Validate.NotNullOrWhitespace(locationId, nameof(locationId));
            Validate.NotNull(telemetryConfig, nameof(telemetryConfig));

            this.TestDisplayName = testDisplayName;
            this.LocationDisplayName = locationDisplayName;
            this.LocationId = locationId;

            _instrumentationKey = telemetryConfig.InstrumentationKey;
            _telemetryClient = new TelemetryClient(telemetryConfig);

            _flushOnDispose = flushOnDispose;

            _log = log;
            _logScope = logScope;

            _currentStage = (int) Stage.New;
        }

        public void Start()
        {
            using (_log.BeginScopeSafe(_logScope))
            {
                _log?.LogInformation($"{nameof(AvailabilityTestScope)}.{nameof(Start)} beginning:"
                                    + " {{TestDisplayName=\"{TestDisplayName}\", LocationDisplayName=\"{LocationDisplayName}\", LocationId=\"{LocationId}\"}}",
                                        TestDisplayName, LocationDisplayName, LocationId);

                TransitionStage(from: Stage.New, to: Stage.Started);

                // Start activity:
                _activitySpanOperationName = Format.AvailabilityTest.SpanOperationName(TestDisplayName, LocationDisplayName);
                _activitySpan = new Activity(_activitySpanOperationName).Start();
                _activitySpanId = Format.AvailabilityTest.SpanId(_activitySpan);

                _distributedOperationId = _activitySpan.RootId;

                // Start the timer:
                _startTime = DateTimeOffset.Now;

                _log?.LogInformation($"{nameof(AvailabilityTestScope)}.{nameof(Start)} finished:"
                                    + " {{TestDisplayName=\"{TestDisplayName}\", LocationDisplayName=\"{LocationDisplayName}\", LocationId=\"{LocationId}\","
                                    + " SpanId=\"{SpanId}\", StartTime=\"{StartTime}\", OperationName=\"{OperationName}\"}}",
                                        TestDisplayName, LocationDisplayName, LocationId,
                                        _activitySpanId, _startTime.ToString("o"), _activitySpanOperationName);
            }
        }

        public void Complete(bool success)
        {
            using (_log.BeginScopeSafe(_logScope))
            {
                _log?.LogInformation($"{nameof(AvailabilityTestScope)}.{nameof(Complete)} invoked with Success={{Success}}:"
                                   + " {{TestDisplayName=\"{TestDisplayName}\", LocationDisplayName=\"{LocationDisplayName}\", LocationId=\"{LocationId}\","
                                   + " SpanId=\"{SpanId}\"}}",
                                     success,
                                     TestDisplayName, LocationDisplayName, LocationId,
                                     Format.SpellIfNull(_activitySpanId));

                EnsureStage(Stage.Started);

                AvailabilityTelemetry availabilityResult = CreateDefaultAvailabilityResult();
                availabilityResult.Success = success;

                availabilityResult.Message = success
                                                ? DefaultResultMessage_NoError_Pass
                                                : DefaultResultMessage_NoError_Fail;

                Complete(availabilityResult);
            }
        }

        private void CompleteOnDisposeOrFinalize(bool disposing)
        {
            _log?.LogInformation($"{nameof(AvailabilityTestScope)}.{nameof(CompleteOnDisposeOrFinalize)} invoked with Disposing={{Disposing}}:"
                                + " {{TestDisplayName=\"{TestDisplayName}\", LocationDisplayName=\"{LocationDisplayName}\", LocationId=\"{LocationId}\","
                                + " SpanId=\"{SpanId}\"}}."
                                + " This indicates that the test result was not set by calling Complete(..); a Failure will be assumed.",
                                    disposing,
                                    TestDisplayName, LocationDisplayName, LocationId,
                                    Format.SpellIfNull(_activitySpanId));

            EnsureStage(Stage.Started);

            AvailabilityTelemetry availabilityResult = CreateDefaultAvailabilityResult();
            availabilityResult.Success = false;

            availabilityResult.Message = disposing
                                            ? DefaultResultMessage_NoResult_Disposed
                                            : DefaultResultMessage_NoResult_NotDisposed;

            Complete(availabilityResult);
        }

        public void Complete(Exception error)
        {
            Complete(error, isTimeout: false);
        }

        public void Complete(Exception error, bool isTimeout)
        {
            using (_log.BeginScopeSafe(_logScope))
            {
                _log?.LogInformation($"{nameof(AvailabilityTestScope)}.{nameof(Complete)} invoked with"
                                   +  " (ExceptionType={ExceptionType}, ExceptionMessage=\"{ExceptionMessage}\", IsTimeout={IsTimeout}):"
                                   +  " {{TestDisplayName=\"{TestDisplayName}\", LocationDisplayName=\"{LocationDisplayName}\", LocationId=\"{LocationId}\","
                                   +  " SpanId=\"{SpanId}\"}}",
                                     Format.SpellIfNull(error?.GetType()?.Name), Format.LimitLength(error.Message, 100, trim: true), isTimeout,
                                     TestDisplayName, LocationDisplayName, LocationId,
                                     Format.SpellIfNull(_activitySpanId));

                EnsureStage(Stage.Started);

                AvailabilityTelemetry availabilityResult = CreateDefaultAvailabilityResult();
                availabilityResult.Success = false;

                availabilityResult.Message = isTimeout
                                                ? DefaultResultMessage_Error_Timeout
                                                : DefaultResultMessage_Error_Exception;

                availabilityResult.Properties[PropertyName_AssociatedException_Type] = Format.SpellIfNull(error?.GetType()?.Name);
                availabilityResult.Properties[PropertyName_AssociatedException_Message] = Format.SpellIfNull(error?.Message);
                availabilityResult.Properties[PropertyName_AssociatedException_IsTimeout] = isTimeout.ToString();

                if (error != null)
                {
                    error.Data[PropertyName_AssociatedAvailabilityResult_Name] = availabilityResult.Name;
                    error.Data[PropertyName_AssociatedAvailabilityResult_RunLocation] = availabilityResult.RunLocation;
                    error.Data[PropertyName_AssociatedAvailabilityResult_Id] = availabilityResult.Id;
                    error.Data[PropertyName_AssociatedAvailabilityResult_IsTimeout] = isTimeout.ToString();
                }

                Complete(availabilityResult);
            }
        }

        public void Complete(AvailabilityTelemetry availabilityResult)
        {
            using (_log.BeginScopeSafe(_logScope))
            {
                _log?.LogInformation($"{nameof(AvailabilityTestScope)}.{nameof(Complete)} beginning:"
                                   + " {{TestDisplayName=\"{TestDisplayName}\", LocationDisplayName=\"{LocationDisplayName}\", LocationId=\"{LocationId}\","
                                   + " SpanId=\"{SpanId}\"}}",
                                     TestDisplayName, LocationDisplayName, LocationId,
                                     Format.SpellIfNull(_activitySpanId));

                Validate.NotNull(availabilityResult, nameof(availabilityResult));

                TransitionStage(from: Stage.Started, to: Stage.Completed);

                // Stop the timer:
                _endTime = DateTimeOffset.Now;

                // Stop activity:
                _activitySpan.Stop();

                // Examine several properties of the Availability Result.
                // If the user set them, use the user's value. Otherwise, initialize appropriately:

                if (String.IsNullOrWhiteSpace(availabilityResult.Message))
                {
                    availabilityResult.Message = availabilityResult.Success
                                                    ? DefaultResultMessage_NoError_Pass
                                                    : DefaultResultMessage_NoError_Fail;
                }

                if (availabilityResult.Timestamp == default(DateTimeOffset))
                {
                    availabilityResult.Timestamp = _startTime;
                }
                else if (availabilityResult.Timestamp.ToUniversalTime() != _startTime.ToUniversalTime())
                {
                    _log?.LogDebug($"{nameof(AvailabilityTestScope)}.{nameof(Complete)} (SpanId=\"{{SpanId}}\") detected that the Timestamp of the"
                                 + $" specified Availability Result is different from the corresponding value of this {nameof(AvailabilityTestScope)}."
                                 + $" The value specified in the Availability Result takes precedence for tracking."
                                 +  " AvailabilityTestScope_StartTime=\"{AvailabilityTestScope_StartTime}\". AvailabilityResult_Timestamp=\"{AvailabilityResult_Timestamp}\"",
                                    _activitySpanId, _startTime.ToUniversalTime().ToString("o"), availabilityResult.Timestamp.ToUniversalTime().ToString("o"));
                }

                TimeSpan duration = _endTime - availabilityResult.Timestamp;

                if (availabilityResult.Duration == TimeSpan.Zero)
                {
                    availabilityResult.Duration = duration;
                }
                else if (availabilityResult.Duration != duration)
                {
                    _log?.LogDebug($"{nameof(AvailabilityTestScope)}.{nameof(Complete)} (SpanId=\"{{SpanId}}\") detected that the Duration of the"
                                 + $" specified Availability Result is different from the corresponding value of this {nameof(AvailabilityTestScope)}."
                                 + $" The value specified in the Availability Result takes precedence for tracking."
                                 +  " AvailabilityTestScope_Duration=\"{AvailabilityTestScope_Duration}\". AvailabilityResult_Duration=\"{AvailabilityResult_Duration}\"",
                                    _activitySpanId, duration, availabilityResult.Duration);
                }

                if (String.IsNullOrWhiteSpace(availabilityResult.Name))
                {
                    availabilityResult.Name = TestDisplayName;
                }
                else if (! availabilityResult.Name.Equals(TestDisplayName, StringComparison.Ordinal))
                {
                    _log?.LogDebug($"{nameof(AvailabilityTestScope)}.{nameof(Complete)} (SpanId=\"{{SpanId}}\") detected that the Name of the"
                                 + $" specified Availability Result is different from the corresponding value of this {nameof(AvailabilityTestScope)}."
                                 + $" The value specified in the Availability Result takes precedence for tracking."
                                 +  " AvailabilityTestScopeTestDisplayName=\"{AvailabilityTestScope_TestDisplayName}\". AvailabilityResult_Name=\"{AvailabilityResult_Name}\"",
                                    _activitySpanId, TestDisplayName, availabilityResult.Name);
                }

                if (String.IsNullOrWhiteSpace(availabilityResult.RunLocation))
                {
                    availabilityResult.RunLocation = LocationDisplayName;
                }
                else if (! availabilityResult.RunLocation.Equals(LocationDisplayName, StringComparison.Ordinal))
                {
                    _log?.LogDebug($"{nameof(AvailabilityTestScope)}.{nameof(Complete)} (SpanId=\"{{SpanId}}\") detected that the RunLocation of the"
                                 + $" specified Availability Result is different from the corresponding value of this {nameof(AvailabilityTestScope)}."
                                 + $" The value specified in the Availability Result takes precedence for tracking."
                                 +  " AvailabilityTestScope_LocationDisplayName=\"{AvailabilityTestScope_LocationDisplayName}\". AvailabilityResult_RunLocation=\"{AvailabilityResult_RunLocation}\"",
                                    _activitySpanId, LocationDisplayName, availabilityResult.RunLocation);
                }

                if (! availabilityResult.Properties.TryGetValue(PropertyName_WebtestLocationId, out string availabilityResultLocationId))
                {
                    availabilityResultLocationId = null;
                }

                if (String.IsNullOrWhiteSpace(availabilityResultLocationId))
                {
                    availabilityResult.Properties[PropertyName_WebtestLocationId] = LocationId;
                }
                else if (! availabilityResultLocationId.Equals(LocationId, StringComparison.Ordinal))
                {
                    _log?.LogDebug($"{nameof(AvailabilityTestScope)}.{nameof(Complete)} (SpanId=\"{{SpanId}}\") detected that the WebtestLocationId of the"
                                 + $" specified Availability Result is different from the corresponding value of this {nameof(AvailabilityTestScope)}."
                                 + $" The value specified in the Availability Result takes precedence for tracking."
                                 +  " AvailabilityTestScope_LocationId=\"{AvailabilityTestScope_LocationId}\". AvailabilityResult_WebtestLocationId=\"{AvailabilityResult_WebtestLocationId}\"",
                                    _activitySpanId, LocationId, availabilityResultLocationId);
                }

                // The user may or may not have set the ID of the availability result telemetry.
                // Either way, we must set it to the right value, otherwise distributed tracing will break:
                availabilityResult.Id = _activitySpanId;

                // Similarly, whatever iKey the user set, we insist on the value from this scope's telemetry configuration to make
                // sure everything ends up in the right place.
                // Users may request a feature to allow sending availabuility results to an iKey that is different from other telemetry.
                // If so, we should consider exposing a corresponsing parameter on the ctor of this class and - corresponsingly - on 
                // the AvailabilityTestResultAttribute. In that case we must also do the appropriate thing with the traces sent by this
                // class. Sending them and the telemetry result to different destinations may be a failure pit for the user.
                availabilityResult.Context.InstrumentationKey = _instrumentationKey;

                // Store the result, but do not send it until SendResult() is called:
                _finalAvailabilityResult = availabilityResult;

                _log?.LogInformation($"{nameof(AvailabilityTestScope)}.{nameof(Complete)} finished"
                                   + " {{TestDisplayName=\"{TestDisplayName}\", LocationDisplayName=\"{LocationDisplayName}\", LocationId=\"{LocationId}\","
                                   + " SpanId=\"{SpanId}\", StartTime=\"{StartTime}\", EndTime=\"{EndTime}\", Duration=\"{Duration}\", Success=\"{Success}\"}}",
                                     TestDisplayName, LocationDisplayName, LocationId,
                                     _activitySpanId, _startTime.ToString("o"), _endTime.ToString("o"), duration, availabilityResult.Success);
            }
        }

        public AvailabilityTestInfo CreateAvailabilityTestInfo()
        {
            AvailabilityTelemetry defaultAvailabilityResult = CreateDefaultAvailabilityResult();
            var testInfo = new AvailabilityTestInfo(TestDisplayName, LocationDisplayName, LocationId, _startTime, defaultAvailabilityResult);
            return testInfo;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~AvailabilityTestScope()
        {
            try
            {
                try
                {
                    Dispose(disposing: false);
                }
                catch (Exception ex)
                {
                    using (_log.BeginScopeSafe(_logScope))
                    {
                        _log?.LogError(ex,
                                      $"{nameof(AvailabilityTestScope)} finalizer threw an exception:"
                                     + " {{SpanId=\"{SpanId}\", ExceptionType=\"{ExceptionType}\", ExceptionMessage=\"{ExceptionMessage}\"}}",
                                       Format.SpellIfNull(_activitySpanId), ex.GetType().Name, ex.Message);
                    }
                }
            }
            catch
            {
                // We are on the Finalizer thread, so the user has no chance of catching an exception.
                // We make our best attempt at logging it and then swallow it to avoid tearing down the application.
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            using (_log.BeginScopeSafe(_logScope))
            {
                Stage stage = CurrentStage;

                _log?.LogInformation($"{nameof(AvailabilityTestScope)}.{nameof(Dispose)} beginning:"
                                   + " {{TestDisplayName=\"{TestDisplayName}\", LocationDisplayName=\"{LocationDisplayName}\", LocationId=\"{LocationId}\","
                                   + " SpanId=\"{SpanId}\", CurrentStage=\"{CurrentStage}\", Disposing=\"{Disposing}\"}}",
                                     TestDisplayName, LocationDisplayName, LocationId,
                                     Format.SpellIfNull(_activitySpanId), stage, disposing);

                switch (stage)
                {
                    case Stage.New:
                        break;

                    case Stage.Started:
                        CompleteOnDisposeOrFinalize(disposing);
                        SendResult();
                        FlushIfRequested();
                        break;

                    case Stage.Completed:
                        SendResult();
                        FlushIfRequested();
                        break;

                    case Stage.SentResults:
                        FlushIfRequested();
                        break;
                }

                _log?.LogInformation($"{nameof(AvailabilityTestScope)}.{nameof(Dispose)} finished:"
                                   + " {{TestDisplayName=\"{TestDisplayName}\", LocationDisplayName=\"{LocationDisplayName}\", LocationId=\"{LocationId}\","
                                   + " SpanId=\"{SpanId}\", CurrentStage=\"{CurrentStage}\", Disposing=\"{Disposing}\"}}",
                                     TestDisplayName, LocationDisplayName, LocationId,
                                     Format.SpellIfNull(_activitySpanId), CurrentStage, disposing);
            }
        }

        private void SendResult()
        {
            _log?.LogInformation($"{nameof(AvailabilityTestScope)}.{nameof(SendResult)} beginning:"
                               + " {{TestDisplayName=\"{TestDisplayName}\", LocationDisplayName=\"{LocationDisplayName}\", LocationId=\"{LocationId}\","
                               + " SpanId=\"{SpanId}\"}}",
                                 TestDisplayName, LocationDisplayName, LocationId,
                                 Format.SpellIfNull(_activitySpanId));

            TransitionStage(from: Stage.Completed, to: Stage.SentResults);

            AvailabilityTelemetry availabilityResult = _finalAvailabilityResult;
            if (availabilityResult == null)
            {
                throw new InvalidOperationException($"This {nameof(AvailabilityTestScope)} was in the {Stage.Completed}-stage,"
                                                  + $" but Final Availability Result was not initialized. This indicated that"
                                                  + $" this {nameof(AvailabilityTestScope)} may be used from multiple threads."
                                                  + $" That is currently not supported.");
            }

            _telemetryClient.TrackAvailability(availabilityResult);

            _log?.LogInformation($"{nameof(AvailabilityTestScope)}.{nameof(SendResult)} finished:"
                               + " {{TestDisplayName=\"{TestDisplayName}\", LocationDisplayName=\"{LocationDisplayName}\", LocationId=\"{LocationId}\","
                               + " SpanId=\"{SpanId}\"}}",
                                 TestDisplayName, LocationDisplayName, LocationId,
                                 Format.SpellIfNull(_activitySpanId));
        }

        private void FlushIfRequested()
        {
            if (_flushOnDispose)
            {
                _log?.LogInformation($"{nameof(AvailabilityTestScope)} is flushing its {nameof(TelemetryClient)}:"
                               + " {{TestDisplayName=\"{TestDisplayName}\", LocationDisplayName=\"{LocationDisplayName}\", LocationId=\"{LocationId}\","
                               + " SpanId=\"{SpanId}\", CurrentStage=\"{CurrentStage}\", FlushOnDispose=\"{FlushOnDispose}\"}}",
                                 TestDisplayName, LocationDisplayName, LocationId,
                                 Format.SpellIfNull(_activitySpanId), CurrentStage, _flushOnDispose);

                _telemetryClient.Flush();
            }
            else
            {
                _log?.LogInformation($"{nameof(AvailabilityTestScope)} is NOT flushing its {nameof(TelemetryClient)}:"
                                               + " {{TestDisplayName=\"{TestDisplayName}\", LocationDisplayName=\"{LocationDisplayName}\", LocationId=\"{LocationId}\","
                                               + " SpanId=\"{SpanId}\", CurrentStage=\"{CurrentStage}\", FlushOnDispose=\"{FlushOnDispose}\"}}",
                                                 TestDisplayName, LocationDisplayName, LocationId,
                                                 Format.SpellIfNull(_activitySpanId), CurrentStage, _flushOnDispose);
            }
        }

        private AvailabilityTelemetry CreateDefaultAvailabilityResult()
        {
            // We cannot create a default result if we already Completed. We should use the actual (final) result then.
            EnsureStage(Stage.New, Stage.Started);

            //const string mockApplicationInsightsAppId = "00000000-0000-0000-0000-000000000000";
            //const string mockApplicationInsightsArmResourceName = "Application-Insights-Component";

            // Note: this method is not thread-safe in respect to Stage transitions
            // (e.g. we may have just transitioned into Strated, but not yet set the start time).

            var availabilityResult = new AvailabilityTelemetry();

            if (CurrentStage == Stage.New)
            {
                availabilityResult.Timestamp = default(DateTimeOffset);
            }
            else
            {
                availabilityResult.Timestamp = _startTime.ToUniversalTime();
                availabilityResult.Id = _activitySpanId;
            }

            availabilityResult.Duration = TimeSpan.Zero;
            availabilityResult.Success = false;

            availabilityResult.Name = TestDisplayName;
            availabilityResult.RunLocation = LocationDisplayName;
            availabilityResult.Properties["WebtestLocationId"] = this.LocationId;

            //availabilityResult.Properties["SyntheticMonitorId"] = $"default_{this.TestArmResourceName}_{this.LocationId}";
            //availabilityResult.Properties["WebtestArmResourceName"] = this.TestArmResourceName;

            //availabilityResult.Properties["SourceId"] = $"sid://{mockApplicationInsightsAppId}.visualstudio.com"
            //                                                      + $"/applications/{mockApplicationInsightsArmResourceName}"
            //                                                      + $"/features/{this.TestArmResourceName}"
            //                                                      + $"/locations/{this.LocationId}";

            if (! String.IsNullOrWhiteSpace(_instrumentationKey))
            {
                availabilityResult.Context.InstrumentationKey = _instrumentationKey;
            }

            return availabilityResult;
        }

        private void TransitionStage(AvailabilityTestScope.Stage from, AvailabilityTestScope.Stage to)
        {
            int fromStage = (int) from, toStage = (int) to;
            int prevStage = Interlocked.CompareExchange(ref _currentStage, toStage, fromStage);

            if (prevStage != fromStage)
            {
                throw new InvalidOperationException($"Error transitioning {nameof(AvailabilityTestScope)}.{nameof(CurrentStage)}"
                                                  + $" to \'{to}\' (={toStage}): Previous {nameof(CurrentStage)} was expected to"
                                                  + $" be \'{from}\' (={fromStage}), but it was actually \'{((Stage) prevStage)}\' (={prevStage}).");
            }
        }

        private void EnsureStage(AvailabilityTestScope.Stage required)
        {
            int requiredStage = (int) required;
            int currStage = _currentStage;

            if (currStage != requiredStage)
            {
                throw new InvalidOperationException($"For this operation {nameof(AvailabilityTestScope)}.{nameof(CurrentStage)}"
                                                  + $" is required to be \'{required}\' (={requiredStage}),"
                                                  + $" but it is actually \'{((Stage) currStage)}\' (={currStage}).");
            }
        }

        private void EnsureStage(AvailabilityTestScope.Stage requiredA, AvailabilityTestScope.Stage requiredB)
        {
            int requiredStageA = (int) requiredA, requiredStageB = (int) requiredB;
            int currStage = _currentStage;

            if (currStage != requiredStageA && currStage != requiredStageB)
            {
                throw new InvalidOperationException($"For this operation {nameof(AvailabilityTestScope)}.{nameof(CurrentStage)}"
                                                  + $" is required to be \'{requiredA}\' (={requiredStageA}) or \'{requiredB}\' (={requiredStageB}),"
                                                  + $" but it is actually \'{((Stage) currStage)}\' (={currStage}).");
            }
        }
    }
}
