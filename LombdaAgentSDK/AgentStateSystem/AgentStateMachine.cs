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
            OnFinish += (output) => RemoveFromControl();
            CancellationTriggered += RemoveFromControl;
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
