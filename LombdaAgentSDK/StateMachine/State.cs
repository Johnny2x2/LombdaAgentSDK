using LombdaAgentSDK.Agents.DataClasses;
using System.Collections.Concurrent;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
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

    //Basically just for the ExitState to not need to return anything
    public abstract class BaseState: IState
    {
        public bool BeingReran = false;
        private List<StateTransition<object>> transitions = new();
        private List<object> input = new();
        private List<object> output = new();
        private bool wasInvoked = false;
        private bool combineInput = false;
        private bool transitioned = false;

        public List<object> _Output { get => output; set => output = value; }
        public List<object> _Input { get => input; set => input = value; }
        public List<StateTransition<object>> _Transitions { get => transitions; set => transitions = value; }

        public bool Transitioned { get => transitioned; set => transitioned = value; }
        public StateMachine CurrentStateMachine { get; set; }
        public abstract Task _Invoke();
        //public abstract Task Invoke(object input);
        public async Task _EnterState(object? input) => this.EnterState(input);
        public virtual void EnterState(object? input) => _Input.Add(input);

        public async Task _ExitState()
        {
            _Input.Clear();
            BeingReran = false;
            Transitioned = true; 
            this.ExitState(); 
        }

        public virtual async Task ExitState() { }
        public abstract Type GetInputType();
        public abstract Type GetOutputType();

        public bool AllowsParallelTransitions { get; set; } = false;

        public bool WasInvoked { get => wasInvoked; set => wasInvoked = value; }
        public bool CombineInput { get => combineInput; set => combineInput = value; }
        public virtual List<StateProcess> CheckConditions()
        {
            List<StateProcess> states = new();
            
            if (!AllowsParallelTransitions)
            {
                foreach(var output in _Output)
                {
                    IState? newState = _Transitions.DefaultIfEmpty(null).FirstOrDefault(transitions => transitions.Evaluate(output))?.NextState ?? null;
                    if (newState != null)
                    {
                        states.Add(new StateProcess(newState,output));
                    }
                }
                
            }
            else
            {
                _Transitions.ForEach(transition =>
                {
                    _Output.ForEach(output =>
                    {
                        if (transition.Evaluate(_Output))
                        {
                            states.Add(new StateProcess(transition.NextState, output));
                        }
                    });
                });
            }

            if (states.Count == 0)
            {
                _Input.ForEach(inpt => states.Add(new StateProcess(this, inpt)));
                
            }

            return states;
        }
    }

    public abstract class BaseState<TInput, TOutput> : BaseState, IState<TInput, TOutput>
    {
        public override Type GetInputType() => typeof(TInput);
        public override Type GetOutputType() => typeof(TOutput);

        private List<StateTransition<TOutput>> transitions = new();

        public List<TInput> Input { get => _Input.ConvertAll(item => (TInput)item); set => _Input = value.ConvertAll(item => (object?)item)!; }
        public List<TOutput> Output { get => _Output.ConvertAll(item => (TOutput)item); set => _Output = value.ConvertAll(item => (object?)item)!; }

        public List<StateTransition<TOutput>> Transitions { get => transitions; set => transitions = value; }

        public async Task _EnterState(TInput input) 
        {
            WasInvoked = false;

            if (!BeingReran)
            {
                if (input != null)
                {
                    Input.Add(input);
                }
            }

            this.EnterState(input);
        }

        public virtual async Task EnterState(TInput? input)
        {
            
        }

        //This is to enforce Output = Invoke() and it returns the Output
        public override async Task<List<TOutput>> _Invoke()
        {
            if (Input.Count == 0)
                throw new InvalidOperationException($"Input is required on State {this.GetType()}");

            //Setup Invoke Task
            List<Task> Tasks = new List<Task>();
            ConcurrentBag<TOutput> oResults = new ConcurrentBag<TOutput>();

            if (CombineInput)
            {
                //Invoke Should handle the Input as a whole
                Tasks.Add(Task.Run(async () => oResults.Add(await Invoke(Input[0]))));
            }
            else
            {
                //Default option to process each input in as its own item (This process is resource bound by the single state instance)
                _Input.ForEach(input => Tasks.Add(Task.Run(async () => oResults.Add(await Invoke((TInput)input)))));
                //Wait for collection
            }

            await Task.WhenAll(Tasks);
            Tasks.Clear();
            WasInvoked = true;

            Output = oResults.ToList();
            return Output;
        }

        public abstract Task<TOutput> Invoke(TInput input);

        //Required override to reference the correct type of transitions
        public override List<StateProcess> CheckConditions()
        {
            List<StateProcess> newStateProcesses = new();

            if (!AllowsParallelTransitions)
            {
                foreach (var output in Output)
                {
                    IState? newState = Transitions?.DefaultIfEmpty(null)?.FirstOrDefault(transition => transition?.Evaluate(output) ?? false)?.NextState ?? null;
                    if (newState != null)
                    {
                        newStateProcesses.Add(new StateProcess(newState, output));
                    }
                }
            }
            else
            {
                Transitions.ForEach(transition =>
                {
                    Output.ForEach(output =>
                    {
                        if (transition.Evaluate(output))
                        {
                            newStateProcesses.Add(new StateProcess(transition.NextState, output));
                        }
                    });
                });
            }

            if (newStateProcesses.Count == 0)
            {
                _Input.ForEach(inpt => newStateProcesses.Add(new StateProcess(this, inpt)));

            }

            return newStateProcesses;
        }

        public void AddTransition(TransitionEvent<TOutput> methodToInvoke, BaseState nextState)
        {
            Transitions.Add(new StateTransition<TOutput>(methodToInvoke, nextState));
        }
    }

    public class StateProcess
    {
        public IState State { get; set; }
        public object Input { get; set; }
        //public object Result { get; set; }
        public StateProcess(IState state, object input)
        {
            State = state;
            Input = input;
        }
    }

    public class ExitState : BaseState<object, object>
    {
        public override async Task<object> Invoke(object? input)
        {
            CurrentStateMachine.Finish();
            return input;
        }
    }
}
