using OpenAI.Responses;

namespace LombdaAgentSDK.StateMachine
{
    public class ResultingStateMachine<TInput, TOutput> : StateMachine
    {
        public List<TOutput>? Results { get => ResultState._Output.ConvertAll(item => (TOutput)item)!;}

        BaseState StartState { get; set; }
        BaseState ResultState { get; set; }

        public ResultingStateMachine() { }

        public async Task<List<TOutput?>> Run(TInput input)
        {
            if(StartState == null)
            {
                throw new InvalidOperationException("Need to Set a Start State for the Resulting StateMachine");
            }

            if (ResultState == null)
            {
                throw new InvalidOperationException("Need to Set a Result State for the Resulting StateMachine");
            }

            StartState._Input.Add(input);

            await base.Run(StartState);

            return Results;
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

                //stop the state machine if needed & exit all states
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
                List<ResultForState> newStateResults = new();

                activeStates.ForEach(state => {
                    newStateResults.AddRange(state.CheckConditions());                    
                    });

                //Check to see if we can exit the active states (Make sure they transition to next phase)
                activeStates.ForEach(state => {
                    if (state.Transitioned) Tasks.Add(Task.Run(async () => await state._ExitState()));
                });


                await Task.WhenAll(Tasks);
                Tasks.Clear();

                //Clear the active states for new states
                activeStates.Clear();

                //Add currentStateMachine to each item and only Enter State if it is new
                //Enter State here is tricky because two states can transition to the same state
                foreach (ResultForState transitionState in newStateResults)
                {
                    transitionState.State.CurrentStateMachine = this;
                    if (!transitionState.State.WasInvoked)
                    {
                        Tasks.Add(Task.Run(async () => await transitionState.State._EnterState(transitionState.Result)));
                    }
                    activeStates.Add(transitionState.State); //Add in the new states
                }

                await Task.WhenAll(Tasks);
                Tasks.Clear();

            }
        }
    }

    
}
