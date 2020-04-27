using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    internal class AvailabilityTestInfoStringAsyncCollector : IAsyncCollector<string>
    {
        private readonly string _initialValue;

        public string InitialValue { get { return _initialValue; } }

        public AvailabilityTestInfoStringAsyncCollector(string initialValue)
        {
            _initialValue = initialValue;
        }

        public Task AddAsync(string item, CancellationToken cancelControl = default(CancellationToken))
        {
            Console.WriteLine("AvailabilityTestInfoStringAsyncCollector.AddAsyc(). Item:");
            Console.WriteLine(Format.NotNullOrWord(item));
            return Task.CompletedTask;
        }

        public Task FlushAsync(CancellationToken cancelControl = default(CancellationToken))
        {
            Console.WriteLine("AvailabilityTestInfoStringAsyncCollector.FlushAsyc()");
            return Task.CompletedTask;
        }
    }
}
