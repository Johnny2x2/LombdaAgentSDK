using BabyAGI;
using BabyAGI.FunctionGenerator.DataModels;
using ChromaDB.Client;
using Examples.Demos.CodingAgent;
using LlmTornado;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Embedding;
using LlmTornado.Embedding.Models;
using LombdaAgentSDK.StateMachine;
using LombdaAgentSDK;
using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using LombdaAgentSDK.AgentStateSystem;
using LombdaAgentSDK.Utility;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Examples.Demos.FunctionGenerator.States
{
    public class CheckExistingFunctionState : AgentState<string, FunctionFoundResultOutput>
    {
        string OriginalTask = "";
        public CheckExistingFunctionState(StateMachine stateMachine) : base(stateMachine) { }

        public override Agent InitilizeStateAgent()
        {
           LLMTornadoModelProvider client = new(ChatModel.OpenAi.Gpt41.V41, [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

           return  new Agent(client,
                "Code Assistant",
                "You are an expert at reviewing functions and determining which to use to solve the Task at hand. If no suitable Function is found let the system know and a new one will be generated.",
                _output_schema: typeof(FunctionFoundResult));
        }

        public async override Task<FunctionFoundResultOutput> Invoke(string args)
        {
            string[]? functions = await QueryFunctionDB(args);

            functions ??= [];

            if (string.IsNullOrEmpty(OriginalTask))
            {
                OriginalTask = CurrentStateMachine.RuntimeProperties.TryGetValue("OriginalTask", out object orginalTask) ? orginalTask.ToString() : string.Empty;
                if(string.IsNullOrEmpty(OriginalTask))
                {
                    throw new InvalidOperationException("Original task is not set in the runtime properties.");
                }
            }

            string prompt = string.Format("""
            The user has provided the following request:

            "{0}"

            Task from the Review Agent:
            {1}

            Below is a list of available functions with their descriptions:

            {2}

            """, OriginalTask, args, string.Join("\n\n", functions));


            FunctionFoundResult result = await BeginRunnerAsync<FunctionFoundResult>(prompt);

            return new FunctionFoundResultOutput(args, result);
        }

        public async Task<string[]?> QueryFunctionDB(string query)
        {
            var configOptions = new ChromaConfigurationOptions(uri: BabyAGIConfig.ChromaDbURI);
            using var httpClient = new HttpClient(ChromaV2ClientHandler.V2handler);
            var chromaClient = new ChromaClient(configOptions, httpClient);

            var functionCollection = await chromaClient.GetOrCreateCollection("functionCollection");
            var functionClient = new ChromaCollectionClient(functionCollection, configOptions, httpClient);

            TornadoApi tornadoApi = new TornadoApi([new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);
            EmbeddingResult? embresult = await tornadoApi.Embeddings.CreateEmbedding(EmbeddingModel.OpenAi.Gen3.Small, query);

            float[]? data = embresult?.Data.FirstOrDefault()?.Embedding;

            var queryData = await functionClient.Query([new(data)], include: ChromaQueryInclude.Metadatas | ChromaQueryInclude.Distances | ChromaQueryInclude.Documents);

            List<string> closeFunctions = new List<string>();

            foreach (var item in queryData)
            {
                foreach (var entry in item)
                {
                    closeFunctions.Add($"Function Name: {entry.Metadata?["FunctionName"]} \n Description: {entry.Document}");
                }
            }

            return closeFunctions.ToArray();
        }
    }
}
