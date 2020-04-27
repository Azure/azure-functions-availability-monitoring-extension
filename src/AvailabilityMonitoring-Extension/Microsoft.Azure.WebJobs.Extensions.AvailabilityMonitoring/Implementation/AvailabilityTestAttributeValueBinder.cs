using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    internal class AvailabilityTestAttributeValueBinder : IValueBinder
    {
        private readonly Type _valueType;

        private readonly Func<Task<object>> _parameterValueBuilder;

        private readonly string _paramaterName;

        private readonly Guid _binderId;

        public AvailabilityTestAttributeValueBinder(Type valueType, string parameterName, Func<Task<object>> parameterValueBuilder)
        {
            _valueType = valueType;
            _parameterValueBuilder = parameterValueBuilder;
            _paramaterName = parameterName;
            _binderId = Guid.NewGuid();
        }
        
        public Type Type { get { return _valueType; } }

        public Task<object> GetValueAsync()
        {
            Console.WriteLine();
            Console.WriteLine($"Value CREATION for parameter \"{_valueType.Name} {_paramaterName}\" requested: AvailabilityTestAttributeValueBinder.GetValueAsync().");
            Console.WriteLine($"Binder-id: {_binderId}");
            Console.WriteLine("Value creation {");
            Task<object> t = _parameterValueBuilder();
            Console.WriteLine("} finished value creation.");
            Console.WriteLine();
            return t;
        }

        public Task SetValueAsync(object value, CancellationToken cancellationToken)
        {
            Console.WriteLine();
            Console.WriteLine($"Value COMPLETION for parameter \"{_valueType.Name} {_paramaterName}\" requested: AvailabilityTestAttributeValueBinder.SetValueAsync().");
            Console.WriteLine($"Binder-id: {_binderId}.");
            Console.WriteLine($"Actually passed value type: {Format.NotNullOrWord(value?.GetType()?.Name)}.");
            Console.WriteLine($"Actually passed value: \'{JsonConvert.SerializeObject(value, Formatting.Indented)}\'.");
            Console.WriteLine();

            return Task.CompletedTask;
        }

        public string ToInvokeString()
        {
            return "";
        }
    }
}
