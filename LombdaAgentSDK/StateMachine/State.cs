using LlmTornado.Moderation;
using LombdaAgentSDK.Agents.DataClasses;
using OpenAI.Realtime;
using System.Collections.Concurrent;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LombdaAgentSDK.StateMachine
{
    public interface IState
    {
        public bool Transitioned { get; set; }
        public bool CombineInput { get; set; }
        public bool WasInvoked { get; set; }
        public List<object> _Output { get; set; }
        public List<object> _Input { get; set; }
        public Task _Invoke();
        public List<StateProcess> CheckConditions();
        public Task _EnterState(object input);
        public Task _ExitState();
        public StateMachine CurrentStateMachine { get; set; }
        public List<StateTransition<object>> _Transitions { get; set; }
    }

    public interface IState<TOutput> : IState
    {
        public List<TOutput> Output { get; set; }
        public List<StateTransition<TOutput>> Transitions { get; set; }
        new public Task<List<TOutput>> _Invoke();
    }

    public interface IState<TInput, TOutput> : IState<TOutput>
    {
        public List<TInput> Input { get; set; }
        public Task _EnterState(TInput input);

        new public Task<List<TOutput>> _Invoke();
    }

    public abstract class BaseState
    {
        public string ID { get => id; set => id = value; }
        internal readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        public bool BeingReran = false;
        private List<StateTransition<object>> transitions = new();
        private List<object> input = new();
        private List<object> output = new();
        private List<StateProcess> inputProcesses = new();
        private List<StateResult> outputResults = new();
        private bool wasInvoked = false;
        private bool combineInput = false;
        private bool transitioned = false;
        private string id = Guid.NewGuid().ToString();

        public List<object> _Output { get => output; set => output = value; }

        public List<StateResult> _OutputResults { get => outputResults; set => outputResults = value; }
        public List<object> _Input { get => input; set => input = value; }
        public List<StateProcess> _InputProcesses { get => inputProcesses; set => inputProcesses = value; }
        public List<StateTransition<object>> _Transitions { get => transitions; set => transitions = value; }

        public bool Transitioned { get => transitioned; set => transitioned = value; }
        public StateMachine? CurrentStateMachine { get; set; }
        public abstract Task _Invoke();
        //public abstract Task Invoke(object input);
        public abstract Task _EnterState(StateProcess? input);

        public abstract Task _ExitState();

        public abstract Type GetInputType();
        public abstract Type GetOutputType();

        public bool AllowsParallelTransitions { get; set; } = false;

        public bool WasInvoked { get => wasInvoked; set => wasInvoked = value; }
        public bool CombineInput { get => combineInput; set => combineInput = value; }

        public abstract List<StateProcess> CheckConditions();
    }

    public abstract class BaseState<TInput, TOutput> : BaseState
    {
        public override Type GetInputType() => typeof(TInput);
        public override Type GetOutputType() => typeof(TOutput);

        private List<StateTransition<TOutput>> transitions = new();
        public List<TOutput> Output { get => OutputResults.Select(output=> output.Result).ToList();}

        public List<TInput> Input { get => InputProcesses.Select(process => process.Input).ToList();}
        public List<StateProcess<TInput>> InputProcesses { get => _InputProcesses.ConvertAll(item => (StateProcess<TInput>)item); set => _InputProcesses = value.ConvertAll(item => (StateProcess)item)!; }
        public List<StateResult<TOutput>> OutputResults { get => _OutputResults.ConvertAll(item => (StateResult<TOutput>)item); set => _OutputResults = value.ConvertAll(item => (StateResult?)item)!; }
        public List<StateTransition<TOutput>> Transitions { get => transitions; set => transitions = value; }

        public async Task _EnterState(StateProcess<TInput>? input) 
        {
            await _semaphore.WaitAsync();
            WasInvoked = false;
            try
            {
                InputProcesses.Add(input!);
                await this.EnterState(input!.Input);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async override Task _EnterState(StateProcess? input) => await _EnterState((StateProcess<TInput>)input!);


        public override async Task _ExitState()
        {
            InputProcesses.Clear();
            await ExitState();
        }

        public virtual async Task EnterState(TInput? input) { }

        public virtual async Task ExitState() { }

        //This is to enforce Output = Invoke() and it returns the Output
        public override async Task<List<StateResult<TOutput>>> _Invoke()
        {
            if (InputProcesses.Count == 0)
                throw new InvalidOperationException($"Input Process is required on State {this.GetType()}");

            //Setup Invoke Task
            List<Task> Tasks = new List<Task>();
            ConcurrentBag<StateResult<TOutput>> oResults = new ConcurrentBag<StateResult<TOutput>>();

            if (CombineInput)
            {
                //Invoke Should handle the Input as a whole
                Tasks.Add(Task.Run(async () => oResults.Add(await InternalInvoke(InputProcesses[0]))));
            }
            else
            {
                //Default option to process each input in as its own item (This process is resource bound by the single state instance)
                InputProcesses.ForEach(process => Tasks.Add(Task.Run(async () => oResults.Add(await InternalInvoke(process)))));
                //Wait for collection
            }
            
            await Task.WhenAll(Tasks);
            Tasks.Clear();
            WasInvoked = true;

            OutputResults = oResults.ToList();

            return OutputResults;
        }

        /// <summary>
        /// Wrapper for Process and result
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<StateResult<TOutput>> InternalInvoke(StateProcess<TInput> input)
        {
            return new StateResult<TOutput>(input.ID, await Invoke(input.Input));
        }

        public abstract Task<TOutput> Invoke(TInput input);

        private BaseState? GetFirstValidStateTransition(TOutput output)
        {
            return Transitions?.DefaultIfEmpty(null)?.FirstOrDefault(transition => transition?.Evaluate(output) ?? false)?.NextState ?? null;
        }

        public List<StateProcess> GetFirstValidStateTransitionForEachResult()
        {
            List<StateProcess> newStateProcesses = new();
            
            OutputResults.ForEach(result =>
            {
                BaseState? newState = GetFirstValidStateTransition(result.Result);

                if (newState != null)
                {
                    newStateProcesses.Add(new StateProcess(newState, result.Result));
                }
                else
                {
                    //ReRun the process 
                    StateProcess<TInput> failedProcess = InputProcesses.First(process => process.ID == result.ProcessID);

                    if (failedProcess.CanReAttempt())
                    {
                        newStateProcesses.Add(failedProcess);
                    }
                }
            });

            return newStateProcesses;
        }

        public List<StateProcess> GetAllValidStateTransitions()
        {
            List<StateProcess> newStateProcesses = new();

            OutputResults.ForEach((output) =>
            {
                List<StateProcess> newStateProcessesFromOutput = new();

                //If the transition evaluates to true for the output, add it to the new state processes
                Transitions.ForEach(transition =>
                {
                    if(transition.Evaluate(output.Result)) newStateProcessesFromOutput.Add(new StateProcess(transition.NextState, output.Result));
                });

                //If process produces no transitions 
                if (newStateProcessesFromOutput.Count == 0)
                {
                    StateProcess failedProcess = InputProcesses.First(process => process.ID == output.ProcessID);
                    //rerun the process up to the max attempts
                    if (failedProcess.CanReAttempt()) newStateProcessesFromOutput.Add(failedProcess);
                }

                newStateProcesses.AddRange(newStateProcessesFromOutput);
            } );
           
            return newStateProcesses;
        }

        public override List<StateProcess> CheckConditions()
        {
            return AllowsParallelTransitions ? GetAllValidStateTransitions() : GetFirstValidStateTransitionForEachResult();
        }

        public void AddTransition(TransitionEvent<TOutput> methodToInvoke, BaseState nextState)
        {
            Transitions.Add(new StateTransition<TOutput>(methodToInvoke, nextState));
        }
    }

    //Task for the next state to process
    public class StateProcess
    {
        public int MaxReruns { get; set; } = 3;
        private int rerunAttempts { get; set; } = 0;
        public string ID { get; } = Guid.NewGuid().ToString();
        public BaseState State { get; set; }
        public object Input { get; set; }
        //public object Result { get; set; }
        public StateProcess() { }

        public StateProcess(BaseState state, object input, int maxReruns = 3)
        {
            State = state;
            Input = input;
            MaxReruns = maxReruns;
        }

        public bool CanReAttempt()
        {
            rerunAttempts++;
            return rerunAttempts < MaxReruns;
        }

        public StateResult CreateStateResult(object result)
        {
            return new StateResult(ID, result);
        }
    }

    public class StateProcess<T> : StateProcess
    {
        public new T Input { get; set; }
        public StateProcess(BaseState state, T input, int maxReruns = 3)
        {
            State = state;
            Input = input!;
            MaxReruns = maxReruns;
        }

        public StateResult<T> CreateStateResult(T result)
        {
            return new StateResult<T>(ID, result);
        }
    }

    public class StateResult
    {
        public string ProcessID { get; set; }
        public object Result { get; set; }
        //public object Result { get; set; }
        public StateResult() { }

        public StateResult(string processID, object result)
        {
            ProcessID = processID;
            Result = result;
        }
    }

    public class StateResult<T> : StateResult
    {
        public new T Result { get; set; }
        public StateResult(string process, T result)
        {
            ProcessID = process;
            Result = result!;
        }     
    }

    public class ExitState : BaseState<object, object>
    {
        public override async Task<object> Invoke(object input)
        {
            CurrentStateMachine.Finish();
            return input; //Forced because of the Task not returning in order
        }
    }
}
