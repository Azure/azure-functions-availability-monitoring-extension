using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;

namespace Microsoft.Azure.WebJobs.Extensions.AvailabilityMonitoring
{
    using PatternMatcherFactory = Func<Func<AvailabilityTestResultAttribute, ValueBindingContext, Task<IAsyncCollector<AvailabilityTelemetry>>>, object>;

    /// <summary>
    /// Temporary workaround until the required overload of the BindToCollector(..) method is added to the SDK.
    /// </summary>
    internal static class FluentBindingRuleExtensions
    {
        private const string PatternMatcherFactoryMethodName = "New";
        private const string FluentBindingRuleBindToCollectorMethodName = "BindToCollector";
        private const string PatternMatcherTypeName = "Microsoft.Azure.WebJobs.Host.Bindings.PatternMatcher";
        private static readonly ParameterModifier[] NoParameterModifiers = new ParameterModifier[0];

#pragma warning disable CS0618
        public static void BindToCollector<TAttribute, TMessage>(
                                        this FluentBindingRule<TAttribute> bindingRule,
                                        Func<TAttribute, ValueBindingContext, Task<IAsyncCollector<TMessage>>> asyncCollectorFactory)
                                            where TAttribute : Attribute
#pragma warning restore CS0618
        {
            // We could speed this up 10x - 100x by creating and caching delegates to the methods we accell vie reflection.
            // However, since this is a temp workaround until the methods are available in the SDK, we will avoid the complexity.

#pragma warning disable CS0618
            Type fluentBindingRuleType = typeof(FluentBindingRule<TAttribute>);
#pragma warning restore CS0618

            // First, reflect to invoke the method
            //     public static PatternMatcher New<TSource, TDest>(Func<TSource, ValueBindingContext, Task<TDest>> func)
            // on the PatternMatcher class:

            Type patternMatcherType = fluentBindingRuleType.Assembly.GetType(PatternMatcherTypeName);
            MethodInfo patternMatcherFactoryBound = null;
            { 
                Type[] genericMethodParamTypes = new Type[] { typeof(TAttribute), typeof(IAsyncCollector<TMessage>) };
                Type requiredParamType = typeof(Func<TAttribute, ValueBindingContext, Task<IAsyncCollector<TMessage>>>);

                foreach (MethodInfo method in patternMatcherType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    if (method.IsGenericMethod && method.GetParameters().Length == 1 && method.GetGenericArguments().Length == 2)
                    {
                        MethodInfo methodBound = method.MakeGenericMethod(genericMethodParamTypes);
                        if (methodBound.GetParameters()[0].ParameterType.Equals(requiredParamType))
                        {
                            patternMatcherFactoryBound = methodBound;
                            break;
                        }
                    }
                }
            }

            // Create a PatternMatcher instance that wraps asyncCollectorFactory:
            object patternMatcherInstance = patternMatcherFactoryBound.Invoke(obj: null, parameters: new object[] { asyncCollectorFactory });

            // Next, reflect to invoke
            //     private void BindToCollector<TMessage>(PatternMatcher pm)
            // in the FluentBindingRule<TAttribute> class:

            MethodInfo bindToCollectorMethodGeneric = fluentBindingRuleType.GetMethod(
                                        FluentBindingRuleBindToCollectorMethodName,
                                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                                        binder: null,
                                        new Type[] { patternMatcherType },
                                        NoParameterModifiers);

            MethodInfo bindToCollectorMethodBound = bindToCollectorMethodGeneric.MakeGenericMethod(new Type[] { typeof(TMessage) });

            // Bind asyncCollectorFactory wrapped into the patternMatcherInstance to the binding rule:
            bindToCollectorMethodBound.Invoke(obj: bindingRule, parameters: new object[] { patternMatcherInstance });
        }
    }
}
