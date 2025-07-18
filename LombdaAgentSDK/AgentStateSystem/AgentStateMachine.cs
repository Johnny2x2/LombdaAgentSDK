using LlmTornado.Common;
using LlmTornado.Moderation;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.StateMachine;
using static LombdaAgentSDK.Runner;


namespace LombdaAgentSDK.AgentStateSystem
{
    public abstract class AgentStateMachine<TInput, TOutput> : StateMachine<TInput, TOutput>
    {
        public LombdaAgent ControlAgent { get; set; }
        public AgentStateMachine(LombdaAgent lombdaAgent) {

            ControlAgent = lombdaAgent;
            InitilizeStates();
            OnBegin += AddToControl;
            //Add OnFinish and CancellationTriggered events to remove from control
            OnFinish += (output) => RemoveFromControl();
            CancellationTriggered += RemoveFromControl;
            //Add new States Event Handlers for Verbose and Streaming Callbacks from State
            OnStateEntered += (state) =>
            {
                if(state is IAgentState agentState)
                {
                    ControlAgent.VerboseCallback +=agentState.RunnerVerboseCallbacks;
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
            ControlAgent.CurrentStateMachines.Remove(this);
        }

        public abstract void InitilizeStates();
    }
}
