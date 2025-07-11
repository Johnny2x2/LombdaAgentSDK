namespace LombdaAgentSDK.StateMachine
{
    public class StateProcess
    {
        private object input = new();

        public int MaxReruns { get; set; } = 3;
        private int rerunAttempts { get; set; } = 0;
        public string ID { get; set; } = Guid.NewGuid().ToString();
        public BaseState State { get; set; }
        public object _Input { get => input; set => input = value; }
        //public object Result { get; set; }
        public StateProcess() { }

        public StateProcess(BaseState state, object input, int maxReruns = 3)
        {
            State = state;
            _Input = input;
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

        public StateProcess<T> GetProcess<T>()
        {
            return new StateProcess<T>(State, (T)input, ID);
        }
    }

    public class StateProcess<T> : StateProcess
    {
        public T Input { get => (T)_Input; set => _Input = (object)value!; }

        public StateProcess(BaseState state, T input, int maxReruns = 3) : base(state, (object?)input!, maxReruns)
        {
            Input = input!;
        }

        public StateProcess(BaseState state, T input, string id, int maxReruns = 3) : base(state, (object?)input!, maxReruns)
        {
            Input = input!;
            ID = id;
        }

        public StateResult<T> CreateStateResult(T result)
        {
            return new StateResult<T>(ID, result);
        }
    }
}
