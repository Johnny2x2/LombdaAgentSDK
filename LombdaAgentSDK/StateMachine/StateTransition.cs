namespace LombdaAgentSDK.StateMachine
{
    public delegate bool TransitionEvent<T>(T input);

    public class StateTransition<T>
    {
        public IState NextProcess { get; set; }

        public TransitionEvent<T> InvokeMethod { get; set; }

        public StateTransition(TransitionEvent<T> MethodToInvoke, IState NextProcess)
        {
            this.NextProcess = NextProcess;
            this.InvokeMethod = MethodToInvoke;
        }

        public virtual bool Evaluate(T? result)
        {
            return (bool?)InvokeMethod.DynamicInvoke(result) ?? false;
        }
    }
}
