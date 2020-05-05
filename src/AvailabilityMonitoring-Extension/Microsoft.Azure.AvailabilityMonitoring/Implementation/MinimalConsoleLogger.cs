using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.AvailabilityMonitoring{
    internal class MinimalConsoleLogger : ILogger
    {
        private class MinimalConsoleLoggerScope : IDisposable
        {
            private MinimalConsoleLogger _logger = null;
            private readonly int _scopeId;

            public MinimalConsoleLoggerScope(MinimalConsoleLogger logger, int scopeId)
            {
                _logger = logger;
                _scopeId = scopeId;
            }

            public void Dispose()
            {
                MinimalConsoleLogger logger = Interlocked.Exchange(ref _logger, null);
                if (logger != null)
                {
                    logger.EndScope(_scopeId);
                }
            }
        }

        private const string TabString = "    ";
        private static readonly Random Rnd = new Random();

        private const int Column1EndOffs = 12;
        private const int Column2EndOffs = 36;

        private int _indentDepth = 0;

        public IDisposable BeginScope<TState>(TState state)
        {
            int scopeId = Rnd.Next();
            var scope = new MinimalConsoleLoggerScope(this, scopeId);

            Console.WriteLine();
            Console.WriteLine(FormatLine(
                                column1: "[BeginScope]",
                                column2: $"ScopeId = {scopeId.ToString("X")} {{",
                                column3: null));

            if (state is IEnumerable<KeyValuePair<string, object>> stateTable)
            {
                int lineNum = 0;
                foreach (string line in Format.AsTextLines(stateTable))
                {
                    Console.WriteLine(FormatLine(
                                column1: (lineNum++ == 0) ? "[ScopeState]" : null,
                                column2: null,
                                column3: state?.ToString() ?? "null"));
                }
            }
            else
            {
                Console.WriteLine(FormatLine(
                                column1: "[ScopeState]",
                                column2: null,
                                column3: state?.ToString() ?? "null"));
            }
            
            Console.WriteLine();

            Interlocked.Increment(ref _indentDepth);
            return scope;
        }

        public void EndScope(int scopeId)
        {
            Interlocked.Decrement(ref _indentDepth);

            Console.WriteLine();
            Console.WriteLine(FormatLine(
                                column1: "[EndScope]",
                                column2: $"}} ScopeId = {scopeId.ToString("X")}",
                                column3: null));
            Console.WriteLine();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Validate.NotNull(formatter, nameof(formatter));

            string message = formatter(state, exception);

            string column1 = $"[{logLevel.ToString()}]";

            string column2 = $"Event={{Id={eventId.Id}, Name=\"{eventId.Name}\"";

            string column3 = null;
            if (false == String.IsNullOrEmpty(message) && exception != null)
            {
                column3 = $"Message = \"{message}\",{TabString}Exception = \"{exception}\"";
            }
            else if (false == String.IsNullOrEmpty(message))
            {
                column3 = message;
            }
            else if (exception != null)
            {
                column3 = exception.ToString();
            }

            string line = FormatLine(column1, column2, column3);
            Console.WriteLine(line);
        }

        private string FormatLine(string column1, string column2, string column3)
        {
            if (String.IsNullOrEmpty(column1) && String.IsNullOrEmpty(column2) && String.IsNullOrEmpty(column3))
            {
                return String.Empty;
            }

            var str = new StringBuilder();

            str.Append(column1 ?? String.Empty);
            if (String.IsNullOrEmpty(column2) && String.IsNullOrEmpty(column3))
            {
                return str.ToString();
            }

            while (str.Length < Column1EndOffs)
            {
                str.Append(" ");
            }

            str.Append(column2 ?? String.Empty);
            if (String.IsNullOrEmpty(column3))
            {
                return str.ToString();
            }

            while (str.Length < Column2EndOffs)
            {
                str.Append(" ");
            }

            for (int i = 0; i < _indentDepth; i++)
            {
                str.Append(TabString);
            }

            str.Append(column3);
            return str.ToString();
        }

    }
}
