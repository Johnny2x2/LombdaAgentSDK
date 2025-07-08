namespace LombdaAgentSDK.StateMachine
{
    public delegate bool TransitionEvent<T>(T input);

    public class StateTransition<T>
    {
        public IState NextState { get; set; }

        public TransitionEvent<T> InvokeMethod { get; set; }

        public StateTransition(TransitionEvent<T> methodToInvoke, IState nextState)
        {
            this.NextState = nextState;
            this.InvokeMethod = methodToInvoke;
        }

        public virtual bool Evaluate(T? result)
        {
            return (bool?)InvokeMethod.DynamicInvoke(result) ?? false;
        }
    }
}
