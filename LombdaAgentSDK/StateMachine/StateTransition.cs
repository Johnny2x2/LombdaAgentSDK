namespace LombdaAgentSDK.StateMachine
{
    public delegate bool TransitionEvent<T>(T input);

    public class StateTransition<T>
    {
        public BaseState NextState { get; set; }

        public TransitionEvent<T> InvokeMethod { get; set; }

        public StateTransition(TransitionEvent<T> methodToInvoke, BaseState nextState)
        {
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
}
