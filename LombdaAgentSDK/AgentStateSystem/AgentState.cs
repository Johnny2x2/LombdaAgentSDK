using LombdaAgentSDK.Agents;
using LombdaAgentSDK.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using static LombdaAgentSDK.Runner;

namespace LombdaAgentSDK.AgentStateSystem
{
    public interface IAgentState
    {
        public RunnerVerboseCallbacks? RunnerVerboseCallbacks { get; set; }
        public StreamingCallbacks? StreamingCallbacks { get; set; }
        public Agent StateAgent { get; set; }

        public event Action<string>? RunningVerboseCallback;

        public event Action<string>? RunningStreamingCallback;

        public CancellationTokenSource CancelTokenSource { get; set; }  
    }

    public abstract class AgentState<TInput, TOutput> : BaseState<TInput, TOutput>, IAgentState
    {
        public RunnerVerboseCallbacks? RunnerVerboseCallbacks { get; set; }
        public StreamingCallbacks? StreamingCallbacks { get; set; }
        public Agent StateAgent { get; set; }
        public event Action<string>? RunningVerboseCallback;

        public event Action<string>? RunningStreamingCallback;

        public CancellationTokenSource CancelTokenSource { get; set; } = new CancellationTokenSource();
        /// <summary>
        /// Gets or sets the current state agent responsible for managing state Agent verbose.
        /// </summary>

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

            RunnerVerboseCallbacks += ReceiveVerbose;
            StreamingCallbacks += ReceiveStreaming;

            StateAgent.Client.CancelTokenSource = CancelTokenSource; // Set the cancellation token source for the agent client
        }

        public void ReceiveVerbose(string message)
        {
            RunningVerboseCallback?.Invoke(message);
        }

        public void ReceiveStreaming(string message)
        {
            RunningStreamingCallback?.Invoke(message);
        }

        public async Task<string> BeginRunnerAsync(Agent agent, string input, bool streaming = false)
        {
            return (await Runner.RunAsync(agent, input, verboseCallback: RunnerVerboseCallbacks,streamingCallback:StreamingCallbacks ,streaming: streaming, cancellationToken: CancelTokenSource)).Text ?? "";
        }

        public async Task<T> BeginRunnerAsync<T>(Agent agent, string input, bool streaming = false)
        {
            return (await Runner.RunAsync(agent, input, verboseCallback: RunnerVerboseCallbacks, streamingCallback: StreamingCallbacks, streaming: streaming, cancellationToken: CancelTokenSource)).ParseJson<T>();
        }

        public async Task<string> BeginRunnerAsync(string input, bool streaming = false)
        {
            return (await Runner.RunAsync(StateAgent, input, verboseCallback: RunnerVerboseCallbacks, streamingCallback: StreamingCallbacks, streaming: streaming, cancellationToken: CancelTokenSource)).Text ?? "";
        }

        public async Task<T> BeginRunnerAsync<T>(string input, bool streaming = false)
        {
            return (await Runner.RunAsync(StateAgent, input, verboseCallback: RunnerVerboseCallbacks, streamingCallback: StreamingCallbacks, streaming: streaming, cancellationToken: CancelTokenSource)).ParseJson<T>();
        }
    }
}
