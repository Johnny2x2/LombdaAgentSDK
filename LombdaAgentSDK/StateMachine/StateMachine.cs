using OpenAI.Responses;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

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

            await base.Run(StartState, input);

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
        private static SemaphoreSlim semaphore = new(1, 1);
        private SemaphoreSlim threadLimitor;

        private List<StateProcess> activeProcesses = new();
        public List<StateProcess> ActiveProcesses => activeProcesses;

        public CancellationTokenSource StopTrigger = new CancellationTokenSource();
        private int maxThreads = 20;

        public Dictionary<string, object> RuntimeProperties { get; set; } = new Dictionary<string, object>();

        public CancellationToken CancelToken { get => StopTrigger.Token; }

        public event Action? CancellationTriggered;
        public event Action? FinishedTriggered;

        public bool IsFinished { get; set; } = false;

        public object? _FinalResult { get; set; }

        public int MaxThreads { get => maxThreads; set => maxThreads = value; }

        public StateMachine() { 
            threadLimitor = new(MaxThreads, maxThreads); 
        }

        //Used to stop the state machine.
        public void Finish() { IsFinished = true; }

        public void Stop() => StopTrigger.Cancel();

        //Thread safe
        private async Task ExitAllProcesses()
        {
            List<Task> Tasks = new List<Task>();

            activeProcesses.ForEach(process => {
                Tasks.Add(Task.Run(async () => await process.State._ExitState()));
            });

            await Task.WhenAll(Tasks);
            activeProcesses.Clear();
            Tasks.Clear();
        }

        //Thread Safe
        private async Task ProcessTick()
        {
            List<Task> Tasks = new List<Task>();

            activeProcesses.ForEach(process => Tasks.Add(Task.Run(async () =>
            {
                await threadLimitor.WaitAsync();
                try
                {
                    await process.State._Invoke();
                }
                finally
                {
                    threadLimitor.Release();
                }

            })));

            //Wait for collection
            await Task.WhenAll(Tasks);
            Tasks.Clear();
        }

        //Thread unsafe might enter state same state
        private async Task InitilizeProcess(StateProcess process)
        {
            await semaphore.WaitAsync();
            //Gain access to state machine
            try
            {
                process.State.CurrentStateMachine ??= this;
                activeProcesses.Add(process);
            }
            finally 
            { 
                semaphore.Release(); 
            }

            //Internal lock on access to state
            await process.State._EnterState(process); //preset input
        }

        //Thread Unsafe from InitilizeProcess
        private async Task InitilizeAllNewProcesses(List<StateProcess> newStateProcesses)
        {
            List<Task> Tasks = new List<Task>();
            foreach (StateProcess stateProcess in newStateProcesses)
            {
                Tasks.Add(Task.Run(async () => await InitilizeProcess(stateProcess)));
            }
            await Task.WhenAll(Tasks);
            Tasks.Clear();

            //This is to remove running the same state twice with two processes.. it gets input from _EnterState
            activeProcesses = activeProcesses.DistinctBy(state => state.State.ID).ToList();
        }

        //Thread Safe
        private async Task<List<StateProcess>> GetNewProcesses()
        {
            List<StateProcess> newStateProcesses = new();

            activeProcesses.ForEach(process => {
                newStateProcesses.AddRange(process.State.CheckConditions());
            });

            return newStateProcesses;
        }

        //Thread safe
        private async Task<bool> CheckIfFinished()
        {
            if (IsFinished)
            {
                await ExitAllProcesses();
                FinishedTriggered?.Invoke();
                return true;
            }
            return false;
        }

        //Thread safe
        private async Task<bool> CheckIfCancelled()
        {
            if (StopTrigger.IsCancellationRequested)
            {
                CancellationTriggered?.Invoke();
                await ExitAllProcesses();
                return true;
            }
            return false;
        }

        public virtual async Task Run(BaseState runStartState, object? input = null)
        {
            //Add Start State 
            if (ActiveProcesses.Count == 0)
            {
                await InitilizeProcess(new StateProcess(runStartState, input));
            }

            while (!StopTrigger.IsCancellationRequested || IsFinished)
            {
                //Collect each state Result
                await ProcessTick();

                //stop the state machine if needed & exit all states
                if (await CheckIfFinished()) break;

                if (await CheckIfCancelled()) break;

                //Create List of transitions to new states from conditional movement
                List<StateProcess> newStateProcesses = await GetNewProcesses();

                //Exit the current Processes
                await ExitAllProcesses();

                //Add currentStateMachine to each item and only Enter State if it is new
                //Add The inputs for the next run to each states to process
                await InitilizeAllNewProcesses(newStateProcesses);
            }
        }
    }

    
}
