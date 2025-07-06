namespace LombdaAgentSDK.StateMachine
{
    public interface IState
    {
        public object? _Output { get; set; }
        public object? _Input { get; set; }
        public Task _Invoke();
        public IState CheckConditions();
        public void _EnterState(object input);
        public void _ExitState();
        public StateMachine CurrentStateMachine { get; set; }
    }

    public interface IState<TInput, TOutput> : IState
    {
        public TOutput Output { get; set; }
        public TInput Input { get; set; }
        public List<StateTransition<TOutput>> Transitions { get; set; }
        new public Task<TOutput> _Invoke();
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
        public void _EnterState(object? input) => this.EnterState(input);
        public virtual void EnterState(object? input) => _Input = input ?? _Input;
        public void _ExitState() => this.ExitState();
        public virtual void ExitState() { }
        public virtual IState CheckConditions()
        {
            var connection = _Transitions.DefaultIfEmpty(null).First(conn => conn.Evaluate(_Output));

            return connection != null ? connection.NextProcess : this;
        }
    }

    public abstract class BaseState<TInput, TOutput> : BaseState, IState<TInput, TOutput>
    {
        private List<StateTransition<TOutput>> transitions = new();

        public TInput Input { get => (TInput)_Input; set => _Input = value; }
        public TOutput Output { get => (TOutput)_Output; set => _Output = value; }

        public List<StateTransition<TOutput>> Transitions { get => transitions; set => transitions = value; }

        public StateMachine CurrentStateMachine { get; set; }

        public void SetInput(TInput input) => Input = (TInput)input;

        public void _EnterState(TInput? input) => this.EnterState(input);

        public virtual void EnterState(TInput? input) => Input = input ?? Input;

        //This is to enforce Output = Invoke() and it returns the Output
        public override async Task<TOutput> _Invoke()
        {
            Output = await Invoke();
            return Output;
        }

        public override abstract Task<TOutput> Invoke();

        //Required override to reference the correct type of transitions
        public override IState CheckConditions()
        {
            var connection = Transitions?.DefaultIfEmpty(null).First(conn => conn.Evaluate(Output));

            return connection != null ? connection.NextProcess : this;
        }

    }

    public class ExitState : BaseState
    {
        public override async Task Invoke() => CurrentStateMachine.End();
    }
}
