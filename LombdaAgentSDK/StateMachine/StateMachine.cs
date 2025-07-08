namespace LombdaAgentSDK.StateMachine
{
    public class ResultingStateMachine<TInput, TOutput> : StateMachine
    {
        public TOutput? Result { get => (TOutput)ResultState._Output!;}

        BaseState StartState { get; set; }
        BaseState ResultState { get; set; }

        public ResultingStateMachine() { }

        public async Task<TOutput?> Run(TInput input)
        {
            if(StartState == null)
            {
                throw new InvalidOperationException("Need to Set a Start State for the Resulting StateMachine");
            }

            if (ResultState == null)
            {
                throw new InvalidOperationException("Need to Set a Result State for the Resulting StateMachine");
            }

            StartState._Input = input;

            await base.Run(StartState);

            return Result;
        }

        public void SetEntryState(BaseState startState)
        {
            if(!startState.GetInputType().IsAssignableTo(typeof(TInput)))
            {
                throw new InvalidCastException($"Entry State {startState.ToString()} with Input type of {startState.GetInputType()} Requires Input Type of {typeof(TInput)}");
            }

            StartState = startState;
        }

        public void SetOutputState(BaseState resultState)
        {
            if (!resultState.GetOutputType().IsAssignableTo(typeof(TOutput)))
            {
                throw new InvalidCastException($"Output State {resultState.ToString()} with Output type of {resultState.GetOutputType()} Requires Cast of Output Type to {typeof(TOutput)}");
            }

            ResultState = resultState;
        }

        public bool CanCastToGeneric<T>(object value)
        {
            return value is T;
        }
    }

    public class StateMachine
    {
        private IState currentState;
        public IState CurrentState => currentState;

        public CancellationTokenSource StopTrigger = new CancellationTokenSource();
        
        public Dictionary<string, object> RuntimeProperties { get; set; } = new Dictionary<string, object>();

        public CancellationToken CancelToken { get => StopTrigger.Token; }

        public event Action? CancellationTriggered;
        public event Action? FinishedTriggered;

        public bool IsFinished { get; set; } = false;

        public object? _FinalResult { get; set; }

        public StateMachine() { }

        //Used to stop the state machine.
        public void Finish() { IsFinished = true; }

        public void Stop() => StopTrigger.Cancel();

        public virtual async Task Run(IState state)
        {
            if (currentState == null)
            {
                await ChangeState(state, state._Input);
            }

            IState nextState;

            while (!StopTrigger.IsCancellationRequested || IsFinished)
            {
                await currentState?._Invoke()!;

                _FinalResult = CurrentState._Output;

                if (IsFinished)
                {
                    currentState._ExitState();
                    FinishedTriggered?.Invoke();
                    break;
                }

                if (StopTrigger.IsCancellationRequested)
                {
                    CancellationTriggered?.Invoke();
                    currentState._ExitState();
                    break;
                }

                nextState = currentState.CheckConditions();
                await ChangeState(nextState, currentState._Output);
            }
        }

        public async Task ChangeState(IState newState, object? result = null)
        {
            if (newState.Equals(currentState)) return;

            newState.CurrentStateMachine = this;

            if (currentState != null)
            {
                currentState._ExitState();
            }

            currentState = newState;

            currentState._EnterState(result);
        }
    }

    
}
