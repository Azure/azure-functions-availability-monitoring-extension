using System;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Protocols;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    internal class AvailabilityTestAttributeBinding : IBinding
    {
        private readonly Func<ValueBindingContext, Task<object>> _parameterValueBuilder;
        private readonly Type _valueType;
        private readonly string _paramaterName;

        public AvailabilityTestAttributeBinding(Type valueType, string parameterName, Func<ValueBindingContext, Task<object>> parameterValueBuilder)
        {
            _parameterValueBuilder = parameterValueBuilder;
            _valueType = valueType;
            _paramaterName = parameterName;
        }

        public bool FromAttribute { get { return true; } }

        public Task<IValueProvider> BindAsync(object value, ValueBindingContext valueBindingContext)
        {
            throw new NotImplementedException();
        }

        public Task<IValueProvider> BindAsync(BindingContext bindingContext)
        {
            Console.WriteLine($"AvailabilityTestAttributeBinding.BindAsync(..):"
                            + $" valueType={_valueType}; paramaterName={_paramaterName}, FunctionInstanceId={bindingContext.FunctionInstanceId}.");

            Func<Task<object>> builder = () => _parameterValueBuilder(bindingContext.ValueContext);
            IValueProvider binder = new AvailabilityTestAttributeValueBinder(_valueType, _paramaterName, builder);
            return Task.FromResult(binder);
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new ParameterDescriptor
            {
                Name = _paramaterName,
                Type = _valueType.FullName
            };
        }
    }
}