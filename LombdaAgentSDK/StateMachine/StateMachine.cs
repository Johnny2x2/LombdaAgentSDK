namespace LombdaAgentSDK.StateMachine
{
    public class StateMachine
    {
        private IState currentState;
        public IState CurrentState => currentState;

        public CancellationTokenSource ExitTrigger = new CancellationTokenSource();

        public Dictionary<string, object> RuntimeProperties { get; set; } = new Dictionary<string, object>();

        public CancellationToken CancelToken { get => ExitTrigger.Token; }

        public event Action? CancellationTriggered;

        public StateMachine() { }

        //Used to stop the state machine.
        public void End() => ExitTrigger.Cancel();

        public async Task Run(IState state)
        {
            if (currentState == null)
            {
                await ChangeState(state, state._Input);
            }

            IState nextState;

            while (!ExitTrigger.IsCancellationRequested)
            {
                await currentState._Invoke();

                nextState = currentState.CheckConditions();

                await ChangeState(nextState, currentState._Output);
            }

            CancellationTriggered?.Invoke();
        }

        public async Task ChangeState(IState newState, object? result = null)
        {
            if (newState.Equals(currentState)) return;

            newState.CurrentStateMachine = this;

            if (currentState != null) currentState._ExitState();

            currentState = newState;

            currentState._EnterState(result);
        }
    }

    
}
