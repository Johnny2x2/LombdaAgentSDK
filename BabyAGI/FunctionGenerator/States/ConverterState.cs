using LombdaAgentSDK.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.FunctionGenerator.States
{
    public delegate U TypeConverter<T,U>(T input);

    public class ConverterState<TInput, TOutput> : BaseState<TInput, TOutput>
    {
        public TypeConverter<TInput, TOutput> ConversionMethod { get; set; }

        public ConverterState(TypeConverter<TInput, TOutput> conversionMethod)
        {
            ConversionMethod = conversionMethod;
        }

        public override async Task<TOutput> Invoke(TInput input)
        {
            return ConversionMethod.Invoke(input);
        }
    }
}
