using LlmTornado.Common;
using LlmTornado.Moderation;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.StateMachine;
using static LombdaAgentSDK.Runner;


namespace LombdaAgentSDK.AgentStateSystem
{
    public interface IAgentStateMachine
    {
        public LombdaAgent ControlAgent { get; set; }
        public List<ModelItem> SharedModelItems { get; set; }
    }

    public abstract class AgentStateMachine<TInput, TOutput> : StateMachine<TInput, TOutput>, IAgentStateMachine
    {
        public LombdaAgent ControlAgent { get; set; }
        public List<ModelItem> SharedModelItems { get; set; } = new List<ModelItem>();

        public AgentStateMachine(LombdaAgent lombdaAgent) {

            ControlAgent = lombdaAgent;
            InitilizeStates();
            OnBegin += AddToControl;
            //Add OnFinish and CancellationTriggered events to remove from control
            OnFinish += (output) => RemoveFromControl();
            CancellationTriggered += RemoveFromControl;
            CancellationTriggered += CancelTriggered;

            //Add new States Event Handlers for Verbose and Streaming Callbacks from State
            OnStateEntered += (state) =>
            {
                if(state.State is IAgentState agentState)
                {
                    ControlAgent.VerboseCallback += agentState.RunnerVerboseCallbacks;
                    ControlAgent.StreamingCallback += agentState.StreamingCallbacks;
                }
            };

            //Remove Verbose and Streaming Callbacks from State when exited
            OnStateExited += (state) =>
            {
                if (state is IAgentState agentState)
                {
                    ControlAgent.VerboseCallback -= agentState.RunnerVerboseCallbacks;
                    ControlAgent.StreamingCallback -= agentState.StreamingCallbacks;
                }
            };
        }

        private void AddToControl()
        {
            ControlAgent.AddStateMachine(this);
        }

        private void RemoveFromControl()
        {
            ControlAgent.RemoveStateMachine(this);
        }

        private void CancelTriggered()
        {
            foreach (var state in States)
            {
                if (state is IAgentState agentState)
                {
                    agentState.CancelTokenSource.Cancel();
                }
            }
        }

        public abstract void InitilizeStates();
    }
}
