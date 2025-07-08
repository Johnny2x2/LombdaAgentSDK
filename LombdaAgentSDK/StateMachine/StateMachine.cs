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
        private List<IState> activeStates = new();
        public List<IState> ActiveStates => activeStates;

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
            //Add Start State
            if (ActiveStates.Count <= 0)
            {
                state.CurrentStateMachine = this;
                activeStates.Add(state);
                await state._EnterState(state._Input); //preset input
            }

            while (!StopTrigger.IsCancellationRequested || IsFinished)
            {
                //States to run
                List<Task> Tasks = new List<Task>();

                //Collect each state Result
                activeStates.ForEach(state => Tasks.Add(Task.Run(async () => await state?._Invoke())));

                //Wait for collection
                await Task.WhenAll(Tasks);

                Tasks.Clear();

                //stop the state machine if needed
                if (IsFinished)
                {
                    foreach (IState activeState in activeStates)
                    {
                        Tasks.Add(Task.Run(async () => await activeState._ExitState()));
                    }
                    await Task.WhenAll(Tasks);
                    Tasks.Clear();
                    FinishedTriggered?.Invoke();
                    break;
                }

                if (StopTrigger.IsCancellationRequested)
                {
                    CancellationTriggered?.Invoke();

                    foreach (IState activeState in activeStates)
                    {
                        Tasks.Add(Task.Run(async () => await activeState._ExitState()));
                    }
                    await Task.WhenAll(Tasks);
                    Tasks.Clear();
                    break;
                }

                //Create List of transitions to new states from conditional movement
                Dictionary<IState, List<IState>> newStateTransitions = new();

                activeStates.ForEach(state => {
                    newStateTransitions.Add(state, state.CheckConditions());
                    });

                //Check to see if we can exit the active states (Make sure they transition to next phase)
                foreach (var executedState in newStateTransitions)
                {
                    if(executedState.Value.All(selectedTransitionStates => !selectedTransitionStates.Equals(executedState)))
                    {
                        Tasks.Add(Task.Run(async () => await executedState.Key._ExitState()));
                    }
                }

                await Task.WhenAll(Tasks);
                Tasks.Clear();

                //Clear the active states for new states
                activeStates.Clear();

                //Add currentStateMachine to each item and only Enter State if it is new
                foreach (var executedState in newStateTransitions)
                {
                    foreach (IState transitionState in executedState.Value)
                    {
                        transitionState.CurrentStateMachine = this;
                        if (!transitionState.Equals(executedState.Key))
                        {
                            Tasks.Add(Task.Run(async () => await transitionState._EnterState(executedState.Key._Output!)));
                        }
                    }
                    activeStates.AddRange(executedState.Value); //Add in the new states
                }

                await Task.WhenAll(Tasks);
                Tasks.Clear();

            }
        }
    }

    
}
