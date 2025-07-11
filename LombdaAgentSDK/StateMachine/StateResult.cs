namespace LombdaAgentSDK.StateMachine
{
    public class StateResult
    {
        private object result = new();

        public string ProcessID { get; set; }
        public object _Result { get => result; set => result = value; }
        //public object Result { get; set; }
        public StateResult() { }

        public StateResult(string processID, object result)
        {
            ProcessID = processID;
            _Result = result;
        }

        public StateResult<T> GetResult<T>()
        {
            return new StateResult<T>(ProcessID, (T)_Result);
        }
    }

    public class StateResult<T> : StateResult
    {
        public T Result { get => (T)_Result; set => _Result = value; }
        public StateResult(string process, T result)
        {
            ProcessID = process;
            Result = result!;
        }
    }
}
