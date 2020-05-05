using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.AvailabilityMonitoring{
    internal static class LogExtensions
    {
        private class NoOpDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }

        private static readonly IDisposable NoOpDisposableSingelton = new NoOpDisposable();

        public static IDisposable BeginScopeSafe<TState>(this ILogger log, TState state) where TState : class
        {
            if (log == null || state == null)
            {
                return NoOpDisposableSingelton;
            }

            return log.BeginScope(state);
        }
    }
}
