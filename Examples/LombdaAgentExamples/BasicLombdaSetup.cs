using Examples.LombdaAgentExamples.ResearchAgentStateMachine;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.AgentStateSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
namespace Examples.LombdaAgentExamples
{
    internal class BasicLombdaSetup
    {
        [Test]
        public async Task BasicTestStreaming()
        {
            // Create an instance of the BasicLombdaAgent
            BasicLombdaAgent agent = new BasicLombdaAgent();
            agent.RootStreamingEvent += ReceiveStream; // Subscribe to the streaming event
            // Example task to run through the state machine

            string task = "What is the capital of France?";
            Console.WriteLine($"[User]: {task}");
            Console.Write("[Agent]: ");
            // Run the state machine and get the result
            var result = await agent.AddToConversation(task, streaming: true);
            // Output the result
            Console.Write("\n");
            Assert.That(result, Is.Not.Null, "The result should not be null.");
        }
        [Test]
        public async Task BasicTest()
        {
            // Create an instance of the BasicLombdaAgent
            BasicLombdaAgent agent = new BasicLombdaAgent();
            agent.RootStreamingEvent += ReceiveStream; // Subscribe to the streaming event
            // Example task to run through the state machine

            string task = "What is the capital of France?";
            Console.WriteLine($"[User]: {task}");
            Console.Write("[Agent]: ");
            // Run the state machine and get the result
            var result = await agent.AddToConversation(task, streaming:false);
            // Output the result
            Console.Write("\n");
            Assert.That(result, Is.Not.Null, "The result should not be null.");
        }

        [Test]
        public async Task BasicImageTestStreaming()
        {
            // Create an instance of the BasicLombdaAgent
            BasicLombdaAgent agent = new BasicLombdaAgent();
            agent.RootStreamingEvent += ReceiveStream; // Subscribe to the streaming event
            // Example task to run through the state machine

            string task = "What is in image?";
            Console.WriteLine($"[User]: {task}");
            Console.Write("[Agent]: ");
            // Run the state machine and get the result
            //Preprocessor cannot handle image input
            var result = await agent.AddImageToConversation(task, "C:\\Users\\johnl\\Pictures\\dogs cropped.jpg", streaming: true);
            // Output the result
            Console.Write("\n");
            Assert.That(result, Is.Not.Null, "The result should not be null.");
        }

        public async Task ReceiveStream(ModelStreamingEvents stream)
        {
            Console.Write($"{stream.EventType}");
        }
    }

    public class BasicLombdaAgent : LombdaAgent
    {
        /// <summary>
        /// Setup the input preprocessor to run the state machine
        /// </summary>
        public ResearchAgent _stateMachine { get; set; }

        public BasicLombdaAgent() : base()
        {             //Initialize the state machine
            _stateMachine = new ResearchAgent(this);
            InputPreprocessor = RunStateMachine;
        }

        public override void InitializeAgent()
        {
            LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41, [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),], useResponseAPI: true);
            string instructions = $"""You are a person assistant who will receive information preprocessed by a Agentic system to help answer the question. Use SYSTEM message from before the user input as tool output""";
            ControlAgent = new Agent(client, "BabyAGI", instructions);
        }

        public async Task<string> RunStateMachine(string task)
        {
            //Return the final result
            var result = (await _stateMachine.Run(task))[0].FinalReport ?? task;
            return result.ToString() ?? task;
        }
    }
}
