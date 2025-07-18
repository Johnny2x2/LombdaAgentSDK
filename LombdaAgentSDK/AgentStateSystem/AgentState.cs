using LombdaAgentSDK.Agents;
using LombdaAgentSDK.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LombdaAgentSDK.AgentStateSystem
{
    public abstract class AgentState<TInput, TOutput> : BaseState<TInput, TOutput>
    {
        /// <summary>
        /// Gets or sets the current state agent responsible for managing state Agent verbose.
        /// </summary>
        public Agent StateAgent { get; set; }

        /// <summary>
        /// Initializes the state agent, preparing it for operation.
        /// </summary>
        /// <remarks>This method must be called before any other operations on the state agent are
        /// performed. Failure to initialize may result in undefined behavior.</remarks>
        public abstract Agent InitilizeStateAgent();

        public override async Task<TOutput> Invoke(TInput input)
        {
            throw new NotImplementedException();
        }

        public AgentState(StateMachine.StateMachine stateMachine)
        {
            CurrentStateMachine = stateMachine;
            CurrentStateMachine.States.Add(this); //Keep States alive in the StateMachine
            StateAgent = InitilizeStateAgent();
        }

        public async Task<string> BeginRunnerAsync(Agent agent, string input)
        {
            return (await Runner.RunAsync(agent, input)).Text ?? "";
        }

        public async Task<T> BeginRunnerAsync<T>(Agent agent, string input)
        {
            return (await Runner.RunAsync(agent, input)).ParseJson<T>();
        }

        public async Task<string> BeginRunnerAsync(string input)
        {
            return (await Runner.RunAsync(StateAgent, input)).Text ?? "";
        }

        public async Task<T> BeginRunnerAsync<T>(string input)
        {
            return (await Runner.RunAsync(StateAgent, input)).ParseJson<T>();
        }
    }
}
