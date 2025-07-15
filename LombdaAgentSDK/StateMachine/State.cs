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
    /// <summary>
    /// Represents the base class for defining a state within a state machine.
    /// </summary>
    /// <remarks>The <see cref="BaseState"/> class provides a framework for managing state transitions, input
    /// and output processing, and state invocation within a state machine. It includes properties and methods for
    /// handling state-specific logic, such as entering and exiting states, checking conditions, and managing
    /// transitions.</remarks>
    public abstract class BaseState
    {
        private SemaphoreSlim threadLimitor;
        private int maxThreads = 20;
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

        /// <summary>
        /// State identifier.
        /// </summary>
        public string ID { get => id; set => id = value; }

        /// <summary>
        /// Gets a list of results extracted from the output results.
        /// </summary>
        public List<object> _Output { get => _OutputResults.Select(output => output._Result).ToList(); }
        /// <summary>
        /// Output results of the state, containing processed results from the state invocation.
        /// </summary>
        public List<StateResult> _OutputResults { get => outputResults; set => outputResults = value; }
        /// <summary>
        /// Gets or sets the list of input objects the state has to process this tick.
        /// </summary>
        public List<object> _Input { get => input; set => input = value; }
        /// <summary>
        /// Input processes that the state has to process this tick.
        /// </summary>
        public List<StateProcess> _InputProcesses { get => inputProcesses; set => inputProcesses = value; }
        /// <summary>
        /// Gets or sets the list of state transitions.
        /// </summary>
        public List<StateTransition<object>> _Transitions { get => transitions; set => transitions = value; }
        /// <summary>
        /// Checks if the state has transitioned.
        /// </summary>
        public bool Transitioned { get => transitioned; set => transitioned = value; }
        /// <summary>
        /// Gets or sets the current state machine instance.
        /// </summary>
        public StateMachine? CurrentStateMachine { get; set; }
        /// <summary>
        /// Internal invoke to abstract BaseState from Type specifics.
        /// </summary>
        /// <returns></returns>
        public abstract Task _Invoke();

        /// <summary>
        /// Adds state Process to the required state.
        /// </summary>
        /// <remarks>This method is abstract and must be implemented by derived classes to define the
        /// specific behavior of entering a new state.</remarks>
        /// <param name="input">The input that influences the state transition. Can be null if no specific input is required for the
        /// transition.</param>
        /// <returns></returns>
        public abstract Task _EnterState(StateProcess? input);

        /// <summary>
        /// Transitions the current state to an exit state asynchronously.
        /// </summary>
        /// <remarks>This method should be implemented to handle any necessary cleanup or finalization
        /// tasks when exiting a state. It is called as part of the state transition process.</remarks>
        /// <returns>A task that represents the asynchronous operation of exiting the state.</returns>
        public abstract Task _ExitState();

        /// <summary>
        /// Retrieves the type of input that this state can process.
        /// </summary>
        /// <returns>A <see cref="Type"/> object representing the input type that this instance is designed to handle.</returns>
        public abstract Type GetInputType();
        /// <summary>
        /// Gets the output type produced by this state.
        /// </summary>
        /// <returns>A <see cref="Type"/> representing the output type.</returns>
        public abstract Type GetOutputType();

        /// <summary>
        /// Gets or sets a value indicating whether parallel transitions are allowed.
        /// </summary>
        public bool AllowsParallelTransitions { get; set; } = false;
        /// <summary>
        /// Check if the state was invoked.
        /// </summary>
        public bool WasInvoked { get => wasInvoked; set => wasInvoked = value; }

        /// <summary>
        /// property to combine input into a single process to avoid running multiple threads for each input.
        /// </summary>
        public bool CombineInput { get => combineInput; set => combineInput = value; }

        /// <summary>
        /// Evaluates and returns a list of state processes that meet specific conditions.
        /// </summary>
        /// <returns>A list of <see cref="StateProcess"/> objects that satisfy the defined conditions.  The list will be empty if
        /// no conditions are met.</returns>
        public abstract List<StateProcess> CheckConditions();
    }

    /// <summary>
    /// Represents a base state in a state machine, providing mechanisms for handling input and output processes, state
    /// transitions, and asynchronous state entry and exit operations.
    /// </summary>
    /// <remarks>This abstract class serves as a foundation for implementing specific states in a state
    /// machine. It manages input processes, output results, and state transitions, and provides asynchronous methods
    /// for entering and exiting states. Derived classes should implement the <see cref="Invoke(TInput)"/> method to
    /// define the processing logic for the state.</remarks>
    /// <typeparam name="TInput">The type of input data processed by the state.</typeparam>
    /// <typeparam name="TOutput">The type of output data produced by the state.</typeparam>
    public abstract class BaseState<TInput, TOutput> : BaseState
    {
        private List<StateProcess<TInput>> inputProcesses = new();
        private List<StateResult<TOutput>> outputResults = new();

        public override Type GetInputType() => typeof(TInput);
        public override Type GetOutputType() => typeof(TOutput);

        private List<StateTransition<TOutput>> transitions = new();
        /// <summary>
        /// Gets the list of output results.
        /// </summary>
        public List<TOutput> Output { get => OutputResults.Select(output=> output.Result).ToList();}
        /// <summary>
        /// Gets the list of input items processed by the input processes.
        /// </summary>
        public List<TInput> Input { get => InputProcesses.Select(process => process.Input).ToList();}
        ///// <summary>
        ///// Gets or sets the collection of input processes.
        ///// </summary>
        //public List<StateProcess<TInput>> InputProcesses { get => _InputProcesses.ConvertAll(item => item.GetProcess<TInput>()); set => _InputProcesses = value.ConvertAll(item => (StateProcess)item)!; }
        ///// <summary>
        ///// Gets or sets the list of output results.
        ///// </summary>
        //public List<StateResult<TOutput>> OutputResults { get => _OutputResults.ConvertAll(item => item.GetResult<TOutput>()); set => _OutputResults = value.ConvertAll(item => (StateResult?)item)!; }
        ///// <summary>
        ///// Gets or sets the collection of input processes.
        ///// </summary>
        public List<StateProcess<TInput>> InputProcesses
        {
            get => inputProcesses;
            set
            {
                inputProcesses = value ?? new List<StateProcess<TInput>>();
                _InputProcesses = inputProcesses.Cast<StateProcess>().ToList();
            }
        }
        ///// <summary>
        ///// Gets or sets the list of output results.
        ///// </summary>
        public List<StateResult<TOutput>> OutputResults
        {
            get => outputResults;
            set
            {
                outputResults = value ?? new List<StateResult<TOutput>>();
                _OutputResults = outputResults.Cast<StateResult>().ToList();
            }
        }
        /// <summary>
        /// Gets or sets the collection of state transitions.
        /// </summary>
        public List<StateTransition<TOutput>> Transitions { get => transitions; set => transitions = value; }

        /// <summary>
        /// Internal _EnterState method to handle adding Input process async before entering state.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Internal _EnterState method to handle adding Input process async before entering state.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async override Task _EnterState(StateProcess? input)
        {
            await _semaphore.WaitAsync();
            WasInvoked = false;
            try
            {
                InputProcesses.Add((StateProcess<TInput>)input!);
                await this.EnterState((TInput)input!._Input!);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Exits after the state has been invoked and processes are cleared.
        /// </summary>
        /// <returns></returns>
        public override async Task _ExitState()
        {
            await _semaphore.WaitAsync();
            try
            {
                _InputProcesses.Clear();
                await ExitState();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Transitions the state machine into the current state asynchronously.
        /// </summary>
        /// <param name="input">Input value</param>
        /// <returns>A task that represents the asynchronous operation of entering the state.</returns>
        public virtual async Task EnterState(TInput? input) { }

        /// <summary>
        /// Performs any necessary cleanup or finalization tasks when exiting the current state.
        /// </summary>
        /// <remarks>This method is intended to be overridden in derived classes to implement
        /// state-specific exit logic. The default implementation does nothing.</remarks>
        /// <returns></returns>
        public virtual async Task ExitState() { }

        //This is to enforce Output = Invoke() and it returns the Output
        //Depending on how many Inputs the State got this could Invoke a lot of Threads.. should probably have a limitor here
        /// <summary>
        /// Asynchronously invokes the state processing logic for each input process and returns the results.
        /// </summary>
        /// <remarks>This method processes each input process either as a combined input or individually,
        /// depending on the <see cref="CombineInput"/> property. It utilizes asynchronous tasks to handle the
        /// processing, which may involve multiple threads. Ensure that the state is properly configured with input
        /// processes before invoking this method, as it will throw an exception if no input processes are
        /// defined.</remarks>
        /// <returns>A task representing the asynchronous operation, containing a list of <see cref="StateResult{TOutput}"/>
        /// objects that represent the results of processing each input.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no input processes are defined for the state.</exception>
        public override async Task<List<StateResult<TOutput>>> _Invoke()
        {
            if (InputProcesses.Count == 0)
                throw new InvalidOperationException($"Input Process is required on State {this.GetType()}");

            //Setup Invoke Task
            List<Task> Tasks = new List<Task>();
            ConcurrentBag<StateResult<TOutput>> oResults = new ConcurrentBag<StateResult<TOutput>>();

            if (CombineInput)
            {
                //Invoke Should handle the Input as a whole (Single Thread can handle processing all the inputs)
                Tasks.Add(Task.Run(async () => oResults.Add(await InternalInvoke(InputProcesses[0]))));
            }
            else
            {
                //Default option to process each input in as its own item
                //(This process is resource bound by the single state instance)
                InputProcesses.ForEach(process => Tasks.Add(Task.Run(async () => oResults.Add(await InternalInvoke(process)))));
            }

            // Wait for collection
            await Task.WhenAll(Tasks);
            Tasks.Clear();
            WasInvoked = true;

            OutputResults = oResults.ToList();

            return OutputResults;
        }

        /// <summary>
        /// StateResult Wrapper for Process and result
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private async Task<StateResult<TOutput>> InternalInvoke(StateProcess<TInput> input)
        {
            return new StateResult<TOutput>(input.ID, await Invoke(input.Input));
        }

        /// <summary>
        /// Processes the specified input and returns the corresponding output asynchronously.
        /// </summary>
        /// <param name="input">The input data to be processed. Cannot be null.</param>
        /// <returns>A task representing the asynchronous operation. The task result contains the processed output.</returns>
        public abstract Task<TOutput> Invoke(TInput input);

        /// <summary>
        /// Determines the first valid state transition based on the provided output.
        /// </summary>
        /// <param name="output">The output used to evaluate potential state transitions.</param>
        /// <returns>The next state if a valid transition is found; otherwise, <see langword="null"/>.</returns>
        private StateTransition? GetFirstValidStateTransition(TOutput output)
        {
            return Transitions?.DefaultIfEmpty(null)?.FirstOrDefault(transition => transition?.Evaluate(output) ?? false) ?? null;
        }

        /// <summary>
        /// Determines the first valid state transition for each result and returns a list of state processes.
        /// </summary>
        /// <remarks>This method iterates over the output results and attempts to find a valid state
        /// transition for each result. If a valid transition is found, it creates a new <see cref="StateProcess"/> with
        /// the new state and result. If no valid transition is found, it checks if the process can be re-attempted and
        /// includes it in the list if possible.</remarks>
        /// <returns>A list of <see cref="StateProcess"/> objects representing the first valid state transition for each result.
        /// If a valid transition is not found and the process can be re-attempted, the original process is included.</returns>
        private List<StateProcess> GetFirstValidStateTransitionForEachResult()
        {
            List<StateProcess> newStateProcesses = new();
            //Results Gathered from invoking
            OutputResults.ForEach(result =>
            {
                //Transitions are selected in order they are added
                StateTransition?  transition = GetFirstValidStateTransition(result.Result);

                //If not transition is found, we can reattempt the process
                if (transition != null)
                {
                    //Check if transition is convertion type or use the output.Result directly
                    var oresult = transition.type == "in_out" ? transition._ConverterMethodResult : result.Result;

                    newStateProcesses.Add(new StateProcess(transition.NextState, oresult));
                }
                else
                {
                    //ReRun the process that failed
                    StateProcess<TInput> failedProcess = InputProcesses.First(process => process.ID == result.ProcessID);
                    //Cap the amount of times a State can reattempt (Fixed at 3 right now)
                    if (failedProcess.CanReAttempt())
                    {
                        newStateProcesses.Add(failedProcess);
                    }
                }
            });

            return newStateProcesses;
        }

        /// <summary>
        /// Retrieves all valid state transitions based on the current output results and transition rules. Used for parallel Transitions
        /// </summary>
        /// <remarks>This method evaluates each output result against defined transitions to determine
        /// valid state processes. If no transitions are valid for a given output, the corresponding process may be
        /// re-attempted if allowed.</remarks>
        /// <returns>A list of <see cref="StateProcess"/> objects representing the valid state transitions.</returns>
        private List<StateProcess> GetAllValidStateTransitions()
        {
            List<StateProcess> newStateProcesses = new();
            //Results Gathered from invoking
            OutputResults.ForEach((output) =>
            {
                List<StateProcess> newStateProcessesFromOutput = new();

                //If the transition evaluates to true for the output, add it to the new state processes
                Transitions.ForEach(transition =>
                {
                    if (transition.Evaluate(output.Result))
                    {
                        //Check if transition is convertion type or use the output.Result directly
                        var result = transition.type == "in_out" ? transition._ConverterMethodResult : output.Result;

                        newStateProcessesFromOutput.Add(new StateProcess(transition.NextState, result));
                    }
                });

                //If process produces no transitions rerun the process
                if (newStateProcessesFromOutput.Count == 0)
                {
                    StateProcess failedProcess = InputProcesses.First(process => process.ID == output.ProcessID);
                    //rerun the process up to the max attempts
                    if (failedProcess.CanReAttempt()) newStateProcessesFromOutput.Add(failedProcess);
                }

                newStateProcesses.AddRange(newStateProcessesFromOutput);
            });

            return newStateProcesses;
        }

        public override List<StateProcess> CheckConditions()
        {
            return AllowsParallelTransitions ? GetAllValidStateTransitions() : GetFirstValidStateTransitionForEachResult();
        }

        /// <summary>
        /// Adds a transition to the current state, specifying the event and the next state to transition to.
        /// </summary>
        /// <param name="methodToInvoke">The event that triggers the transition. This event is associated with the output type <typeparamref
        /// name="TOutput"/>.</param>
        /// <param name="nextState">The state to transition to when the specified event occurs.</param>
        public void AddTransition(TransitionEvent<TOutput> methodToInvoke, BaseState nextState)
        {
            Transitions.Add(new StateTransition<TOutput>(methodToInvoke, nextState));
        }
        /// <summary>
        /// Adds a state transition to the current state.
        /// </summary>
        /// <remarks>This method creates a new state transition using the specified event and conversion
        /// method,  and adds it to the list of transitions for the current state.</remarks>
        /// <typeparam name="T">The type of the input parameter for the conversion method.</typeparam>
        /// <param name="methodToInvoke">The event method to invoke during the transition.</param>
        /// <param name="conversionMethod">The method used to convert the output of the transition event to the required type.</param>
        /// <param name="nextState">The state to transition to after the event is invoked and conversion is performed.</param>
        public void AddTransition<T>(TransitionEvent<TOutput> methodToInvoke, ConversionMethod<TOutput, T> conversionMethod, BaseState nextState)
        {
            var transition = new StateTransition<TOutput, T>(methodToInvoke, conversionMethod, nextState);
            Transitions.Add(transition);
        }
    }

    /// <summary>
    /// Helper State to exit the StateMachine.
    /// </summary>
    public class ExitState : BaseState<object, object>
    {
        public override async Task<object> Invoke(object input)
        {
            CurrentStateMachine.Finish();
            return input; //Forced to use BaseState<object, object> because of the Task not returning in order
        }
    }
}
