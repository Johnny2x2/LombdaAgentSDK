using BabyAGI.BabyAGIStateMachine.DataModels;
using BabyAGI.BabyAGIStateMachine.States;
using LombdaAgentSDK.AgentStateSystem;
using LombdaAgentSDK.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BabyAGI.BabyAGIStateMachine
{
    internal class BabyAGIStateMachine : AgentStateMachine<string, string>
    {
        public BabyAGIStateMachine(LombdaAgent lombdaAgent) : base(lombdaAgent)
        {
            // Initialize the runtime properties for the state machine
            RuntimeProperties.TryAdd("CurrentScratchpad", string.Empty);
            RuntimeProperties.TryAdd("queueTasksForEval", new List<QueueTask>());
            RuntimeProperties.TryAdd("TaskQueue", new List<QueueTask>());
        }

        public override void InitilizeStates()
        {
            BAExecutionState executionState = new BAExecutionState(this);
        }

        public async Task RunStateMachine()
        {
            StateMachine stateMachine = new StateMachine();
        }
    }
}
