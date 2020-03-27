﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.ApplicationInsights.DataContracts;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    internal static class Convert
    {
        private static readonly string NullWord = "null";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string NotNullOrWord(string s)
        {
            return s ?? NullWord;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetPropertyOrNullWord(AvailabilityTelemetry availabilityResult, string propertyName)
        {
            return NotNullOrWord(GetPropertyOrNull(availabilityResult, propertyName));
        }

        public static string GetPropertyOrNull(AvailabilityTelemetry availabilityResult, string propertyName)
        {
            IDictionary<string, string> properties = availabilityResult?.Properties;
            if (properties == null || propertyName == null)
            {
                return null;
            }

            if (false == properties.TryGetValue(propertyName, out string propertyValue))
            {
                return null;
            }

            return propertyValue;
        }

        public static JObject AvailabilityTestInvocationToJObject(AvailabilityTestInvocation availabilityTestInvocation)
        {
            Validate.NotNull(availabilityTestInvocation, nameof(availabilityTestInvocation));
            JObject jObject = JObject.FromObject(availabilityTestInvocation);
            return jObject;
        }

        public static AvailabilityTestInvocation JObjectToAvailabilityTestInvocation(JObject availabilityTestInvocation)
        {
            Validate.NotNull(availabilityTestInvocation, nameof(availabilityTestInvocation));

            try
            {
                AvailabilityTestInvocation stronglyTypedTestInvocation = availabilityTestInvocation.ToObject<AvailabilityTestInvocation>();
                return stronglyTypedTestInvocation;
            }
            catch(Exception)
            {
                return null;
            }
        }

        public static AvailabilityTelemetry JObjectToAvailabilityTelemetry(JObject availabilityResult)
        {
            Validate.NotNull(availabilityResult, nameof(availabilityResult));

            try
            {
                AvailabilityTelemetry stronglyTypedAvailabilityTelemetry = availabilityResult.ToObject<AvailabilityTelemetry>();
                return stronglyTypedAvailabilityTelemetry;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static AvailabilityTelemetry AvailabilityTestInvocationToAvailabilityTelemetry(AvailabilityTestInvocation availabilityTestInvocation)
        {
            Validate.NotNull(availabilityTestInvocation, nameof(availabilityTestInvocation));
            return availabilityTestInvocation.AvailabilityResult;
        }

        public static AvailabilityTestInvocation AvailabilityTelemetryToAvailabilityTestInvocation(AvailabilityTelemetry availabilityResult)
        {
            Validate.NotNull(availabilityResult, nameof(availabilityResult));
            return new AvailabilityTestInvocation(availabilityResult);
        }
    }
}