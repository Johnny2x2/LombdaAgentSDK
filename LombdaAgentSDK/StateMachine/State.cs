using System.Reflection.Metadata.Ecma335;

namespace LombdaAgentSDK.StateMachine
{
    public interface IState
    {
        public object? _Output { get; set; }
        public object? _Input { get; set; }
        public Task _Invoke();
        public List<IState> CheckConditions();
        public Task _EnterState(object input);
        public Task _ExitState();
        public StateMachine CurrentStateMachine { get; set; }
        public List<StateTransition<object>> _Transitions { get; set; }
    }

    public interface IState<TOutput> : IState
    {
        public TOutput Output { get; set; }
        public List<StateTransition<TOutput>> Transitions { get; set; }
        new public Task<TOutput> _Invoke();
    }

    public interface IState<TInput, TOutput> : IState<TOutput>
    {
        public TInput Input { get; set; }
        public void _EnterState(TInput input);
    }

    //Basically just for the ExitState to not need to return anything
    public abstract class BaseState: IState
    {
        private List<StateTransition<object>> transitions = new();
        private object input = new();
        private object output = new();
        public object? _Output { get => output; set => output = value; }
        public object? _Input { get => input; set => input = value; }
        public List<StateTransition<object>> _Transitions { get => transitions; set => transitions = value; }
        public StateMachine CurrentStateMachine { get; set; }
        public virtual Task _Invoke() => Invoke();
        public abstract Task Invoke();
        public async Task _EnterState(object? input) => this.EnterState(input);
        public virtual void EnterState(object? input) => _Input = input ?? _Input;
        public async Task _ExitState() => this.ExitState();
        public virtual async Task ExitState() { }
        public abstract Type GetInputType();
        public abstract Type GetOutputType();

        public virtual List<IState> CheckConditions()
        {
            List<IState> states = new();
            _Transitions.ForEach(conn =>
            {
                if (conn.Evaluate(_Output))
                {
                    states.Add(conn.NextProcess);
                }
            });

            if (states.Count == 0) 
            {
                states.Add(this);
            }


            return states;
        }
    }

    public abstract class BaseState<TInput, TOutput> : BaseState, IState<TInput, TOutput>
    {
        public override Type GetInputType() => typeof(TInput);
        public override Type GetOutputType() => typeof(TOutput);

        private List<StateTransition<TOutput>> transitions = new();

        public TInput Input { get => (TInput)_Input; set => _Input = value; }
        public TOutput Output { get => (TOutput)_Output; set => _Output = value; }

        public List<StateTransition<TOutput>> Transitions { get => transitions; set => transitions = value; }

        public StateMachine CurrentStateMachine { get; set; }

        public void SetInput(TInput input) => Input = (TInput)input;

        public async Task _EnterState(TInput? input) => this.EnterState(input);

        public virtual async Task EnterState(TInput? input) => Input = input ?? Input;

        //This is to enforce Output = Invoke() and it returns the Output
        public override async Task<TOutput> _Invoke()
        {
            Output = await Invoke();
            return Output;
        }

        public override abstract Task<TOutput> Invoke();

        //Required override to reference the correct type of transitions
        public override List<IState?> CheckConditions()
        {
            List<IState> states = new();
            _Transitions.ForEach(conn =>
            {
                if (conn.Evaluate(_Output))
                {
                    states.Add(conn.NextProcess);
                }
            });

            if (states.Count == 0)
            {
                states.Add(this);
            }


            return states;
        }

        public void AddTransition(TransitionEvent<TOutput> MethodToInvoke, IState NextProcess)
        {
            Transitions.Add(new StateTransition<TOutput>(MethodToInvoke, NextProcess));
        }
    }

    public class ExitState : BaseState<object, object>
    {
        public override async Task<object> Invoke()
        {
            CurrentStateMachine.Finish();
            return Input;
        }
    }
}
