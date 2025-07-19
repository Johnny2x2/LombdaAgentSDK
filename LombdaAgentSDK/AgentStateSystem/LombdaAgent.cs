using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LombdaAgentSDK.Runner;

namespace LombdaAgentSDK.AgentStateSystem
{
    public abstract class LombdaAgent
    {
        public List<ModelItem> SharedModelItems = new List<ModelItem>();
        public Agent ControlAgent { get; set; }

        public List<StateMachine.StateMachine> CurrentStateMachines { get; set; } = new();

        public RunResult CurrentResult { get; set; } = new RunResult();
        public string MainThreadId { get; set; } = "";

        public event Action? StartingExecution;
        public event Action? FinishedExecution;
        public event Action<string>? verboseEvent;
        public event Action<string>? streamingEvent;
        public event Action<string>? RootVerboseEvent;
        public event Action<string>? RootStreamingEvent;
        public event Action<StateMachine.StateMachine> StateMachineAdded;
        public event Action<StateMachine.StateMachine> StateMachineRemoved;

        public StreamingCallbacks? StreamingCallback;

        public RunnerVerboseCallbacks? VerboseCallback;

        public StreamingCallbacks? MainStreamingCallback;

        public RunnerVerboseCallbacks? MainVerboseCallback;

        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();

        public LombdaAgent()
        {
            InitializeAgent();
            StreamingCallback += RecieveStreamingCallbacks;
            VerboseCallback += RecieveVerboseCallbacks;
            MainStreamingCallback += RootStreamingCallback;
            MainVerboseCallback += RootVerboseCallback;
            ControlAgent.Client.CancelTokenSource = CancellationTokenSource;
        }

        private void RootStreamingCallback(string message)
        {
            RootStreamingEvent?.Invoke(message);
        }

        private void RootVerboseCallback(string message)
        {
            RootVerboseEvent?.Invoke(message);
        }

        private void RecieveStreamingCallbacks(string message)
        {
            streamingEvent?.Invoke(message);
        }

        private void RecieveVerboseCallbacks(string message)
        {
            verboseEvent?.Invoke(message);
        }

        public void AddStateMachine(StateMachine.StateMachine stateMachine)
        {
            CurrentStateMachines.Add(stateMachine);
            StateMachineAdded.Invoke(stateMachine);
        }
        public void RemoveStateMachine(StateMachine.StateMachine stateMachine)
        {
            CurrentStateMachines.Add(stateMachine);
            StateMachineRemoved.Invoke(stateMachine);
        }

        public void CancelExecution()
        {
            CancellationTokenSource.Cancel();

            foreach (var stateMachine in CurrentStateMachines)
            {
                stateMachine.Stop();
            }
        }

        public abstract void InitializeAgent();

        public async Task<string> AddToConversation(string userInput, bool streaming = true)
        {
            StartingExecution?.Invoke();

            if (CancellationTokenSource.Token.IsCancellationRequested)
            {
                if (!CancellationTokenSource.TryReset())
                {
                    CancellationTokenSource = new CancellationTokenSource();
                }
            }

            if (ControlAgent == null)
            {
                throw new InvalidOperationException("ControlAgent is not set. Please set ControlAgent before adding to conversation.");
            }

            CurrentResult = await Runner.RunAsync(ControlAgent, userInput, messages: CurrentResult.Messages, verboseCallback: MainVerboseCallback, streaming: streaming, streamingCallback: MainStreamingCallback, cancellationToken:CancellationTokenSource, responseID: string.IsNullOrEmpty(MainThreadId) ? "" : MainThreadId);

            if (!CancellationTokenSource.Token.IsCancellationRequested)
            {
                MainThreadId = CurrentResult.Response.Id;
            }

            FinishedExecution?.Invoke();

            return CurrentResult.Text ?? "Error getting Response";
        }

        public async Task<string> StartNewConversation(string userInput, bool streaming = true)
        {
            CurrentResult = new RunResult();
            MainThreadId = "";
            return await AddToConversation(userInput, streaming);
        }
    }
}
