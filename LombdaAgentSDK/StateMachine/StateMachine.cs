using OpenAI.Responses;

namespace LombdaAgentSDK.StateMachine
{
    [Obsolete]
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

            StartState.CurrentStateMachine = this;
            StateProcess<TInput> process = new StateProcess<TInput>(StartState, input);
            ActiveProcesses.Add(process);
            await StartState._EnterState(process); //preset input

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

    }

    public class StateMachine<TInput, TOutput> : StateMachine
    {
        public List<TOutput>? Results { get => ResultState._Output.ConvertAll(item => (TOutput)item)!; }

        BaseState StartState { get; set; }
        BaseState ResultState { get; set; }

        public StateMachine() { }

        public async Task<List<TOutput?>> Run(TInput input)
        {
            if (StartState == null)
            {
                throw new InvalidOperationException("Need to Set a Start State for the Resulting StateMachine");
            }

            if (ResultState == null)
            {
                throw new InvalidOperationException("Need to Set a Result State for the Resulting StateMachine");
            }


            StartState.CurrentStateMachine = this;
            StateProcess<TInput> process = new StateProcess<TInput>(StartState, input);
            ActiveProcesses.Add(process);
            await StartState._EnterState(process); //preset input


            await base.Run(StartState);

            return Results;
        }

        public void SetEntryState(BaseState startState)
        {
            if (!startState.GetInputType().IsAssignableTo(typeof(TInput)))
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

    }

    public class StateMachine
    {
        private List<StateProcess> activeProcesses = new();
        public List<StateProcess> ActiveProcesses => activeProcesses;

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

        public virtual async Task Run(BaseState runStartState, object? input = null)
        {
            //Add Start State
            if (ActiveProcesses.Count == 0)
            {
                runStartState.CurrentStateMachine = this;
                StateProcess startProcess = new StateProcess(runStartState, input);
                activeProcesses.Add(startProcess);
                await runStartState._EnterState(startProcess); //preset input
            }

            while (!StopTrigger.IsCancellationRequested || IsFinished)
            {
                //States to run
                List<Task> Tasks = new List<Task>();

                //Collect each state Result
                activeProcesses.ForEach(process => Tasks.Add(Task.Run(async () => await process.State._Invoke())));

                //Wait for collection
                await Task.WhenAll(Tasks);

                Tasks.Clear();

                //stop the state machine if needed & exit all states
                if (IsFinished)
                {
                    foreach (var process in activeProcesses)
                    {
                        Tasks.Add(Task.Run(async () => await process.State._ExitState()));
                    }
                    await Task.WhenAll(Tasks);
                    Tasks.Clear();
                    FinishedTriggered?.Invoke();
                    break;
                }

                if (StopTrigger.IsCancellationRequested)
                {
                    CancellationTriggered?.Invoke();

                    foreach (var process in activeProcesses)
                    {
                        Tasks.Add(Task.Run(async () => await process.State._ExitState()));
                    }
                    await Task.WhenAll(Tasks);
                    Tasks.Clear();
                    break;
                }

                //Create List of transitions to new states from conditional movement
                List<StateProcess> newStateProcesses = new();

                activeProcesses.ForEach(process => {
                    newStateProcesses.AddRange(process.State.CheckConditions());                    
                    });

                //Check to see if we can exit the active states (Make sure they transition to next phase)
                activeProcesses.ForEach(process => {
                    if (!process.State.CombineInput) 
                    {
                        //If normal state where user doesn't have fixed transition
                        Tasks.Add(Task.Run(async () => await process.State._ExitState())); 
                    }
                    else
                    {
                        //If state did transition
                        if (process.State.Transitioned)
                        {
                            //Trigger Exit and clear the InputProcesses collected
                            Tasks.Add(Task.Run(async () => await process.State._ExitState()));
                        }
                    }
                });


                await Task.WhenAll(Tasks);
                Tasks.Clear();

                //Clear the active states for new states
                activeProcesses.Clear();

                //Add currentStateMachine to each item and only Enter State if it is new
                //Add The inputs for the next run to each states to process
                foreach (StateProcess stateProcess in newStateProcesses)
                {
                    stateProcess.State.CurrentStateMachine = this;
                    //For existing states that have not exited the InputProcess will still exist to run
                    //Was Invoked but didn't transition
                    if (!stateProcess.State.WasInvoked)
                    {
                        Tasks.Add(Task.Run(async () => await stateProcess.State._EnterState(stateProcess)));
                    }

                    activeProcesses.Add(stateProcess); //Add in the new states
                }

                //This is to remove running the state twice with two inputs.. it gets input from _EnterState
                activeProcesses = activeProcesses.DistinctBy(state => state.GetType()).ToList();

                await Task.WhenAll(Tasks);
                Tasks.Clear();
            }
        }
    }

    
}
