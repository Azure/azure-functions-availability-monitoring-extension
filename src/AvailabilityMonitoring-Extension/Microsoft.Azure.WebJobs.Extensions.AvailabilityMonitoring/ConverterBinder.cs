using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    internal class ConverterBinder<TBoundValueType, TInnerBinderValueType> : IValueBinder
    {
        public static Type BoundValueType { get { return typeof(TBoundValueType); } }

        private readonly IValueBinder _innerBinder;
        private readonly Func<TInnerBinderValueType, TBoundValueType> _inputConverter;
        private readonly Func<TBoundValueType, TInnerBinderValueType> _outputConverter;

        public ConverterBinder(IValueBinder innerBinder, Func<TInnerBinderValueType, TBoundValueType> inputConverter, Func<TBoundValueType, TInnerBinderValueType> outputConverter)
        {
            Validate.NotNull(innerBinder, nameof(innerBinder));
            Validate.NotNull(inputConverter, nameof(inputConverter));
            Validate.NotNull(outputConverter, nameof(outputConverter));

            if (false == innerBinder.Type.IsAssignableFrom(typeof(TInnerBinderValueType)))
            {
                // @ToDo: Test this IsAssignableFrom business!
                throw new ArgumentException($"The {nameof(innerBinder)} specified to this"
                                          + $" {nameof(ConverterBinder<TBoundValueType, TInnerBinderValueType>)}<{typeof(TBoundValueType).Name}, {typeof(TInnerBinderValueType).Name}>"
                                          + $" ctor specifies the Type property with the value \"{innerBinder.Type?.FullName ?? "null"}\", which is not assignabe"
                                          + $" to \"{typeof(TInnerBinderValueType).FullName}\".");
            }

            _innerBinder = innerBinder;
            _inputConverter = inputConverter;
            _outputConverter = outputConverter;
        }

        Type IValueProvider.Type
        {
            get
            {
                return typeof(TBoundValueType);
            }
        }

        async Task<object> IValueProvider.GetValueAsync()
        {
            object objectValue = await _innerBinder.GetValueAsync();
            TInnerBinderValueType innerBinderValue = (TInnerBinderValueType) objectValue;
            TBoundValueType outerValue = _inputConverter(innerBinderValue);
            return outerValue;
        }

        string IValueProvider.ToInvokeString()
        {
            return _innerBinder.ToInvokeString();
        }

        Task IValueBinder.SetValueAsync(object value, CancellationToken cancellationToken)
        {
            TBoundValueType outerValue = (TBoundValueType) value;
            TInnerBinderValueType innerBinderValue = _outputConverter(outerValue);
            return _innerBinder.SetValueAsync(innerBinderValue, cancellationToken);
        }
    }
}
