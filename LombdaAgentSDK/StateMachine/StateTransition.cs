namespace LombdaAgentSDK.StateMachine
{
    public delegate bool TransitionEvent<T>(T input);
    public delegate TOutput ConversionMethod<TInput, TOutput>(TInput input);
    public class StateTransition
    {
        public BaseState NextState { get; set; }
        public object? _ConverterMethodResult { get => _converterMethodResult; set => _converterMethodResult = value; }

        public string type = "base";
        private object? _converterMethodResult;
    }

    public class StateTransition<T> : StateTransition
    {
        public TransitionEvent<T> InvokeMethod { get; set; }
        public StateTransition() { }
        public StateTransition(TransitionEvent<T> methodToInvoke, BaseState nextState)
        {
            type = "out";
            if (nextState.GetInputType().IsAssignableTo(typeof(T)) || typeof(T).IsSubclassOf(nextState.GetInputType()))
            {
                this.NextState = nextState;
                this.InvokeMethod = methodToInvoke;
            }
            else
            {
                throw new InvalidOperationException($"Next State with input type of {nextState.GetInputType()} requires Input type assignable to type of {typeof(T)}");
            }

        }

        public virtual bool Evaluate(T? result)
        {
            return (bool?)InvokeMethod.DynamicInvoke(result) ?? false;
        }
    }

    public class StateTransition<TInput, TOutput> : StateTransition<TInput>
    {
        public ConversionMethod<TInput, TOutput> ConverterMethod { get; set; }

        public TOutput ConverterMethodResult
        {
            get
            {
                if (ConverterMethodResult == null)
                {
                    throw new InvalidOperationException("Converter method result is not set. Ensure the converter method has been invoked.");
                }
                return (TOutput)_ConverterMethodResult;
            }
            set => ConverterMethodResult = value;
        }

        public StateTransition(TransitionEvent<TInput> methodToInvoke, ConversionMethod<TInput, TOutput> converter, BaseState nextState)
        {
            if (nextState.GetInputType().IsAssignableTo(typeof(TOutput)) || typeof(TOutput).IsSubclassOf(nextState.GetInputType()))
            {
                type = "in_out";
                this.ConverterMethod = converter;
                this.NextState = nextState;
                this.InvokeMethod = methodToInvoke;
            }
            else
            {
                throw new InvalidOperationException($"Next State with input type of {nextState.GetInputType()} requires Input type assignable to type of {typeof(TOutput)}");
            }
        }

        public override bool Evaluate(TInput? result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result), "Input cannot be null.");
            }
            
            if((bool?)InvokeMethod.DynamicInvoke(result) ?? false)
            {
                _ConverterMethodResult = ConverterMethod.Invoke(result);
                return true;
            }

            return false;
        }
    }
}
