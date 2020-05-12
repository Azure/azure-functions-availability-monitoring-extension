using System;
using System.Diagnostics;

namespace Microsoft.Azure.AvailabilityMonitoring
{
    internal static class ActivityExtensions
    {
        public static bool IsAvailabilityTestSpan(this Activity activity, out string testInfoDescriptor, out string testInvocationInstanceDescriptor)
        {
            if (activity == null)
            {
                testInfoDescriptor = null;
                testInvocationInstanceDescriptor = null;
                return false;
            }

            string activityOperationName = activity.OperationName;

            if (activityOperationName == null || false == activityOperationName.StartsWith(Format.AvailabilityTest.SpanOperationNameObjectName, StringComparison.OrdinalIgnoreCase))
            {
                testInfoDescriptor = null;
                testInvocationInstanceDescriptor = null;
                return false;
            }

            testInfoDescriptor = activityOperationName;
            testInvocationInstanceDescriptor = activity.RootId;
            
            return true;
        }
    }
}
